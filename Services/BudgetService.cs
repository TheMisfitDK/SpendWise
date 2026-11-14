using System.Globalization;
using ExpenseTracker.Data;
using ExpenseTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Services;

/// <summary>One line in the budgets table.</summary>
public sealed record BudgetRow(DateOnly Month, decimal Spent, decimal Budget, bool IsCustom, bool IsCurrent)
{
    public bool Over => Spent > Budget;

    public double Used => Budget <= 0 ? 0 : (double)(Spent / Budget);

    public string Percent => (Used * 100).ToString("0", CultureInfo.InvariantCulture) + "%";

    public string Label => Month.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

    public string MonthValue => Months.ToInputValue(Month);
}

/// <summary>
/// Budgets are stored per month. Anything without a row uses the default from
/// configuration, so the app still works before the user sets anything.
/// </summary>
public class BudgetService(AppDbContext db, IConfiguration config)
{
    public decimal DefaultBudget => config.GetValue<decimal?>("App:MonthlyBudget") ?? 0m;

    public async Task<decimal> GetBudgetAsync(DateOnly month, CancellationToken ct = default)
    {
        var key = Months.FirstOfMonth(month);

        var row = await db.Budgets
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Month == key, ct);

        return row?.Amount ?? DefaultBudget;
    }

    public async Task<bool> HasOverrideAsync(DateOnly month, CancellationToken ct = default)
    {
        var key = Months.FirstOfMonth(month);
        return await db.Budgets.AsNoTracking().AnyAsync(b => b.Month == key, ct);
    }

    public async Task<IReadOnlyDictionary<DateOnly, decimal>> GetOverridesAsync(
        DateOnly from, DateOnly toInclusive, CancellationToken ct = default)
    {
        var start = Months.FirstOfMonth(from);
        var end = Months.FirstOfMonth(toInclusive);

        var rows = await db.Budgets
            .AsNoTracking()
            .Where(b => b.Month >= start && b.Month <= end)
            .ToListAsync(ct);

        return rows.ToDictionary(b => b.Month, b => b.Amount);
    }

    public async Task SetBudgetAsync(DateOnly month, decimal amount, CancellationToken ct = default)
    {
        var key = Months.FirstOfMonth(month);

        var row = await db.Budgets.FirstOrDefaultAsync(b => b.Month == key, ct);
        if (row is null)
        {
            db.Budgets.Add(new Budget { Month = key, Amount = amount });
        }
        else
        {
            row.Amount = amount;
        }

        await db.SaveChangesAsync(ct);
    }

    /// <summary>Drops the override so the month goes back to the default budget.</summary>
    public async Task ClearBudgetAsync(DateOnly month, CancellationToken ct = default)
    {
        var key = Months.FirstOfMonth(month);

        var row = await db.Budgets.FirstOrDefaultAsync(b => b.Month == key, ct);
        if (row is null)
        {
            return;
        }

        db.Budgets.Remove(row);
        await db.SaveChangesAsync(ct);
    }
}
