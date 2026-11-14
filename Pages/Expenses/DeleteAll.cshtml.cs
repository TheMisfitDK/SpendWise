using ExpenseTracker.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Pages.Expenses;

/// <summary>
/// Bulk delete. Deliberately a separate page with a summary and an explicit
/// confirmation, so a single stray click cannot wipe the database.
/// </summary>
public class DeleteAllModel(AppDbContext db) : PageModel
{
    public int Count { get; private set; }
    public decimal Total { get; private set; }

    public async Task OnGetAsync()
    {
        var amounts = await db.Expenses
            .AsNoTracking()
            .Select(e => e.Amount)
            .ToListAsync();

        Count = amounts.Count;
        Total = amounts.Sum();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var deleted = await db.Expenses.ExecuteDeleteAsync();

        TempData["Toast"] = deleted == 0
            ? "There was nothing to delete."
            : $"Deleted all {deleted} expenses. The dashboard is empty again.";

        return RedirectToPage("./Index");
    }
}
