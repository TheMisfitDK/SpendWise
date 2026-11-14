using ExpenseTracker.Data;
using ExpenseTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseTracker.Pages.Expenses;

public class DeleteModel(AppDbContext db) : PageModel
{
    public Expense? Expense { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Expense = await db.Expenses.FindAsync(id);
        return Expense is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var expense = await db.Expenses.FindAsync(id);
        if (expense is null)
        {
            return NotFound();
        }

        db.Expenses.Remove(expense);
        await db.SaveChangesAsync();

        TempData["Toast"] = $"Deleted “{expense.Description}”.";
        return RedirectToPage("./Index");
    }
}
