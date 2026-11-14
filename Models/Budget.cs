using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Models;

/// <summary>
/// A spending limit for one month. Months without a row fall back to the
/// configured default budget.
/// </summary>
public class Budget
{
    public int Id { get; set; }

    /// <summary>Always the first day of the month; the unique index enforces one row per month.</summary>
    public DateOnly Month { get; set; }

    [Range(1, 100_000_000, ErrorMessage = "Enter a budget between 1 and 100,000,000.")]
    public decimal Amount { get; set; }
}
