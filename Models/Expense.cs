using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Models;

/// <summary>
/// A single amount of money that left the account.
/// </summary>
public class Expense
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Give the expense a short description.")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "Use between 2 and 80 characters.")]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 1_000_000, ErrorMessage = "Enter an amount between 0.01 and 1,000,000.")]
    [DataType(DataType.Currency)]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(30)]
    public string Category { get; set; } = CategoryCatalog.DefaultName;

    [DataType(DataType.Date)]
    [Display(Name = "Date spent")]
    public DateOnly SpentOn { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [StringLength(200, ErrorMessage = "Notes must be 200 characters or fewer.")]
    [Display(Name = "Notes (optional)")]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
