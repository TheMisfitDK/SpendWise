using System.Globalization;
using System.Text;

namespace ExpenseTracker.Services;

/// <summary>SVG geometry for the cumulative chart, ready to drop into a path attribute.</summary>
public sealed record SpendChart(string LinePath, string AreaPath, string BudgetPath, decimal Max)
{
    public bool HasData => !string.IsNullOrEmpty(LinePath);
}

/// <summary>
/// Turns a month of daily totals into SVG path strings. Doing the maths here
/// keeps formatting culture-safe (no commas for decimals in the markup).
/// </summary>
public static class ChartGeometry
{
    private const double Width = 600;
    private const double Height = 180;
    private const double Pad = 10;

    public static SpendChart Build(IReadOnlyList<decimal> dailyTotals, decimal budget, int daysToDraw = 0)
    {
        var days = dailyTotals.Count;
        if (days < 2)
        {
            return new SpendChart(string.Empty, string.Empty, string.Empty, 0m);
        }

        // For the current month we only draw up to today; a flat line across days
        // that have not happened yet would look like spending that never occurred.
        if (daysToDraw <= 0 || daysToDraw > days)
        {
            daysToDraw = days;
        }

        if (daysToDraw < 2)
        {
            daysToDraw = 2;
        }

        var cumulative = new decimal[days];
        var running = 0m;
        for (var i = 0; i < days; i++)
        {
            running += dailyTotals[i];
            cumulative[i] = running;
        }

        // Nothing spent yet: show the empty state rather than a dead flat line.
        if (cumulative[daysToDraw - 1] <= 0m)
        {
            return new SpendChart(string.Empty, string.Empty, string.Empty, 0m);
        }

        var max = Math.Max(budget, cumulative[daysToDraw - 1]);
        if (max <= 0)
        {
            max = 1m;
        }

        // Leave 8% headroom so the line never touches the top edge.
        var top = max * 1.08m;

        double X(int index) => index * Width / (days - 1);
        double Y(decimal value) => Height - Pad - (double)(value / top) * (Height - 2 * Pad);

        var line = new StringBuilder();
        for (var i = 0; i < daysToDraw; i++)
        {
            line.Append(i == 0 ? "M " : " L ");
            line.Append(Format(X(i))).Append(' ').Append(Format(Y(cumulative[i])));
        }

        var linePath = line.ToString();
        var areaPath = string.Concat(
            linePath,
            " L ", Format(X(daysToDraw - 1)), " ", Format(Height - Pad),
            " L 0 ", Format(Height - Pad), " Z");

        var budgetPath = string.Concat(
            "M 0 ", Format(Y(0m)),
            " L ", Format(Width), " ", Format(Y(budget)));

        return new SpendChart(linePath, areaPath, budgetPath, max);
    }

    private static string Format(double value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);
}