using System.Globalization;
using ExpenseTracker.Data;
using ExpenseTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Services;

/// <summary>One category's slice of a month, already formatted for the view.</summary>
public sealed record CategorySlice(ExpenseCategory Category, decimal Total, int Count, double Share)
{
    /// <summary>Width of the bar relative to the biggest category, 0–100.</summary>
    public double Relative { get; init; }

    public string BarPercent => (Relative * 100).ToString("0.#", CultureInfo.InvariantCulture) + "%";
    public string SharePercent => (Share * 100).ToString("0.#", CultureInfo.InvariantCulture) + "%";
}

/// <summary>One bar in the six-month trend chart.</summary>
public sealed record TrendPoint(string Label, decimal Total, bool IsCurrent)
{
    public double HeightPercent { get; init; }
    public string CssHeight => (Total <= 0 ? 2 : Math.Max(HeightPercent, 2)).ToString("0.#", CultureInfo.InvariantCulture) + "%";
    public bool HasSpending => Total > 0;
}

/// <summary>Average spend for one day of the week across the six-month window.</summary>
public sealed record WeekdayPoint(string Label, decimal Average, double Relative)
{
    public string CssHeight =>
        (Relative * 100).ToString("0.#", CultureInfo.InvariantCulture) + "%";
}

public sealed class MonthSummary
{
    public DateOnly MonthStart { get; init; }
    public decimal Total { get; init; }
    public int Count { get; init; }
    public decimal DailyAverage { get; init; }
    public Expense? Biggest { get; init; }
    public decimal Budget { get; init; }
    public IReadOnlyList<CategorySlice> Categories { get; init; } = [];
    public IReadOnlyList<TrendPoint> Trend { get; init; } = [];
    public IReadOnlyList<WeekdayPoint> Weekdays { get; init; } = [];
    public IReadOnlyList<Expense> Recent { get; init; } = [];
    public SpendChart Chart { get; init; } = new(string.Empty, string.Empty, string.Empty, 0m);

    /// <summary>CSS conic-gradient built from the category shares.</summary>
    public string DonutCss { get; init; } = "conic-gradient(#eef0f7 0% 100%)";

    public decimal Remaining => Budget - Total;
    public double BudgetUsed => Budget <= 0 ? 0 : (double)(Total / Budget);
    public string BudgetPercent => (BudgetUsed * 100).ToString("0", CultureInfo.InvariantCulture) + "%";
    public bool OverBudget => Total > Budget;
    public string MonthName => MonthStart.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
}

public sealed record ExpensePage(
    IReadOnlyList<Expense> Items,
    decimal Total,
    int TotalCount,
    int PageNumber,
    int PageSize)
{
    public int TotalPages => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;
    public int FirstItemNumber => TotalCount == 0 ? 0 : (PageNumber - 1) * PageSize + 1;
    public int LastItemNumber => Math.Min(PageNumber * PageSize, TotalCount);
}

/// <summary>
/// All the read-side queries live here so the page models stay thin and the
/// dashboard maths can be reused or unit tested later.
/// </summary>
public class ExpenseService(AppDbContext db, BudgetService budgets)
{
    public async Task<MonthSummary> GetMonthSummaryAsync(
        DateOnly month, int trendMonths = 6, CancellationToken ct = default)
    {
        var monthStart = new DateOnly(month.Year, month.Month, 1);
        var nextMonth = monthStart.AddMonths(1);
        var trendStart = monthStart.AddMonths(-(trendMonths - 1));

        // One query loads the whole window, then the maths happens in memory.
        // At this scale (a few thousand rows) that's simpler and avoids SQLite's
        // quirks with decimal aggregates.
        var window = await db.Expenses
            .AsNoTracking()
            .Where(e => e.SpentOn >= trendStart && e.SpentOn < nextMonth)
            .ToListAsync(ct);

        var inMonth = window
            .Where(e => e.SpentOn >= monthStart && e.SpentOn < nextMonth)
            .OrderByDescending(e => e.SpentOn)
            .ThenByDescending(e => e.Id)
            .ToList();

        var total = inMonth.Sum(e => e.Amount);

        var grouped = inMonth
            .GroupBy(e => e.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(e => e.Amount), Count = g.Count() })
            .OrderByDescending(g => g.Total)
            .ToList();

        var biggestCategory = grouped.Count > 0 ? grouped[0].Total : 0m;

        var categories = grouped
            .Select(g => new CategorySlice(
                CategoryCatalog.Get(g.Category),
                g.Total,
                g.Count,
                total == 0 ? 0 : (double)(g.Total / total))
            {
                Relative = biggestCategory == 0 ? 0 : (double)(g.Total / biggestCategory),
            })
            .ToList();

        var trend = new List<TrendPoint>();
        for (var i = 0; i < trendMonths; i++)
        {
            var m = trendStart.AddMonths(i);
            var sum = window
                .Where(e => e.SpentOn.Year == m.Year && e.SpentOn.Month == m.Month)
                .Sum(e => e.Amount);

            trend.Add(new TrendPoint(m.ToString("MMM", CultureInfo.InvariantCulture), sum, i == trendMonths - 1));
        }

        var maxTrend = trend.Count == 0 ? 0m : trend.Max(t => t.Total);
        var withHeights = trend
            .Select(t => t with
            {
                HeightPercent = maxTrend == 0 ? 0 : (double)(t.Total / maxTrend) * 100,
            })
            .ToList();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var isCurrentMonth = monthStart.Year == today.Year && monthStart.Month == today.Month;
        var budget = await budgets.GetBudgetAsync(monthStart, ct);

        // Daily totals for the cumulative chart; index 0 is the first of the month.
        var daysInMonth = DateTime.DaysInMonth(monthStart.Year, monthStart.Month);
        var daily = new decimal[daysInMonth];
        foreach (var e in inMonth)
        {
            daily[e.SpentOn.Day - 1] += e.Amount;
        }

        // Day-of-week pattern over the whole window. Future days are excluded so a
        // half-finished month does not drag every average down.
        var windowEnd = nextMonth.AddDays(-1);
        if (today < windowEnd)
        {
            windowEnd = today;
        }

        var dayCounts = new int[7];
        var dayTotals = new decimal[7];

        for (var d = trendStart; d <= windowEnd; d = d.AddDays(1))
        {
            dayCounts[(int)d.DayOfWeek]++;
        }

        foreach (var e in window)
        {
            if (e.SpentOn <= windowEnd)
            {
                dayTotals[(int)e.SpentOn.DayOfWeek] += e.Amount;
            }
        }

        var maxWeekday = 0m;
        var weekdayAverages = new decimal[7];
        for (var i = 0; i < 7; i++)
        {
            weekdayAverages[i] = dayCounts[i] == 0 ? 0m : dayTotals[i] / dayCounts[i];
            maxWeekday = Math.Max(maxWeekday, weekdayAverages[i]);
        }

        // Monday first, because a spending week reads better that way.
        var weekdays = new[] { 1, 2, 3, 4, 5, 6, 0 }
            .Select(i => new WeekdayPoint(
                DateTimeFormatInfo.InvariantInfo.GetAbbreviatedDayName((DayOfWeek)i),
                weekdayAverages[i],
                maxWeekday == 0 ? 0 : (double)(weekdayAverages[i] / maxWeekday)))
            .ToList();

        var daysElapsed = isCurrentMonth
            ? today.Day
            : daysInMonth;

        return new MonthSummary
        {
            MonthStart = monthStart,
            Total = total,
            Count = inMonth.Count,
            DailyAverage = daysElapsed == 0 ? 0 : total / daysElapsed,
            Biggest = inMonth.Count == 0 ? null : inMonth.MaxBy(e => e.Amount),
            Budget = budget,
            Categories = categories,
            Trend = withHeights,
            Weekdays = weekdays,
            Recent = inMonth.Take(6).ToList(),
            DonutCss = BuildDonut(categories),
            Chart = ChartGeometry.Build(daily, budget, daysElapsed),
        };
    }

    /// <summary>Turns the category shares into one conic-gradient string.</summary>
    private static string BuildDonut(IReadOnlyList<CategorySlice> categories)
    {
        if (categories.Count == 0)
        {
            return "conic-gradient(#eef0f7 0% 100%)";
        }

        var stops = new List<string>(categories.Count);
        var start = 0d;

        foreach (var c in categories)
        {
            var end = start + c.Share * 100;
            stops.Add(FormattableString.Invariant(
                $"{c.Category.Color} {start:0.##}% {end:0.##}%"));
            start = end;
        }

        return $"conic-gradient(from -90deg, {string.Join(", ", stops)})";
    }

    public async Task<ExpensePage> SearchAsync(
        string? query,
        string? category,
        DateOnly? from,
        DateOnly? toExclusive,
        int page = 1,
        int pageSize = 15,
        CancellationToken ct = default)
    {
        var filtered = db.Expenses.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            filtered = filtered.Where(e =>
                e.Description.Contains(term) ||
                (e.Notes != null && e.Notes.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            filtered = filtered.Where(e => e.Category == category);
        }

        if (from is not null)
        {
            filtered = filtered.Where(e => e.SpentOn >= from);
        }

        if (toExclusive is not null)
        {
            filtered = filtered.Where(e => e.SpentOn < toExclusive);
        }

        var totalCount = await filtered.CountAsync(ct);

        // Summed in memory on purpose: the column is stored as integer cents,
        // and SQLite aggregation over a converted decimal is not worth the risk.
        var amounts = await filtered.Select(e => e.Amount).ToListAsync(ct);
        var total = amounts.Sum();

        if (page < 1) page = 1;
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        if (page > totalPages) page = totalPages;

        var items = await filtered
            .OrderByDescending(e => e.SpentOn)
            .ThenByDescending(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new ExpensePage(items, total, totalCount, page, pageSize);
    }

    /// <summary>Spend per calendar month, keyed by the first day of the month.</summary>
    public async Task<IReadOnlyDictionary<DateOnly, decimal>> GetMonthlyTotalsAsync(
        DateOnly from, DateOnly toExclusive, CancellationToken ct = default)
    {
        var rows = await db.Expenses
            .AsNoTracking()
            .Where(e => e.SpentOn >= from && e.SpentOn < toExclusive)
            .Select(e => new { e.SpentOn, e.Amount })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => new DateOnly(r.SpentOn.Year, r.SpentOn.Month, 1))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));
    }
}
