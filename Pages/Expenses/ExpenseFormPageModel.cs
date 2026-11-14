using ExpenseTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseTracker.Pages.Expenses;

/// <summary>
/// Shared form state for Create and Edit so both pages render the same partial.
/// </summary>
public abstract class ExpenseFormPageModel : PageModel
{
    [BindProperty]
    public Expense Expense { get; set; } = new();

    public IReadOnlyList<string> Categories => CategoryCatalog.Names;
}
