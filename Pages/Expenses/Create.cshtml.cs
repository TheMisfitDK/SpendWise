using ExpenseTracker.Data;
using ExpenseTracker.Models;
using ExpenseTracker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseTracker.Pages.Expenses;

public class CreateModel(AppDbContext db, CurrencyService currency) : ExpenseFormPageModel
{
    public void OnGet()
    {
        Expense.SpentOn = DateOnly.FromDateTime(DateTime.Today);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        db.Expenses.Add(Expense);
        await db.SaveChangesAsync();

        TempData["Toast"] = $"Added {currency.Format(Expense.Amount)} to {Expense.Category}.";
        return RedirectToPage("./Index");
    }
}
