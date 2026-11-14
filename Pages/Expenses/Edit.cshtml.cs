using ExpenseTracker.Data;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Pages.Expenses;

public class EditModel(AppDbContext db) : ExpenseFormPageModel
{
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var expense = await db.Expenses.FindAsync(id);
        if (expense is null)
        {
            return NotFound();
        }

        Expense = expense;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var existing = await db.Expenses.FindAsync(Expense.Id);
        if (existing is null)
        {
            return NotFound();
        }

        // Copy field by field instead of saving the posted entity directly, so a
        // crafted request cannot overwrite CreatedAt or invent an Id.
        existing.Description = Expense.Description;
        existing.Amount = Expense.Amount;
        existing.Category = Expense.Category;
        existing.SpentOn = Expense.SpentOn;
        existing.Notes = Expense.Notes;

        await db.SaveChangesAsync();

        TempData["Toast"] = $"Updated “{existing.Description}”.";
        return RedirectToPage("./Index");
    }
}
