using System.Globalization;
using ExpenseTracker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseTracker.Pages.Budgets;

public class IndexModel(BudgetService budgets, ExpenseService expenses, CurrencyService currency) : PageModel
{
    public const decimal MaxBudget = 100_000_000m;

    [BindProperty]
    public decimal Amount { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Month { get; set; }

    public DateOnly SelectedMonth { get; private set; }
    public DateOnly CurrentMonth { get; private set; } = Months.Current;
    public bool HasCustomBudget { get; private set; }
    public decimal DefaultBudget => budgets.DefaultBudget;
    public IReadOnlyList<BudgetRow> Rows { get; private set; } = [];

    public string SelectedMonthValue => Months.ToInputValue(SelectedMonth);
    public string SelectedMonthLabel => SelectedMonth.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

    public async Task OnGetAsync()
    {
        await LoadAsync();
        Amount = await budgets.GetBudgetAsync(SelectedMonth);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadAsync();

        if (Amount < 1 || Amount > MaxBudget)
        {
            ModelState.AddModelError(nameof(Amount), "Enter a budget between 1 and 100,000,000.");
            return Page();
        }

        await budgets.SetBudgetAsync(SelectedMonth, Amount);

        TempData["Toast"] = $"Budget for {SelectedMonthLabel} set to {currency.Format(Amount)}.";
        return RedirectToPage("./Index", new { month = SelectedMonthValue });
    }

    public async Task<IActionResult> OnPostResetAsync()
    {
        await LoadAsync();
        await budgets.ClearBudgetAsync(SelectedMonth);

        TempData["Toast"] = $"{SelectedMonthLabel} now uses the default budget.";
        return RedirectToPage("./Index", new { month = SelectedMonthValue });
    }

    private async Task LoadAsync()
    {
        CurrentMonth = Months.Current;

        var requested = Months.TryParse(Month) ?? CurrentMonth;
        SelectedMonth = requested > CurrentMonth ? CurrentMonth : requested;

        HasCustomBudget = await budgets.HasOverrideAsync(SelectedMonth);

        // The table always covers the six months ending with the current one, so
        // editing an older month never shrinks the history you can see.
        var start = CurrentMonth.AddMonths(-5);
        var totals = await expenses.GetMonthlyTotalsAsync(start, CurrentMonth.AddMonths(1));
        var overrides = await budgets.GetOverridesAsync(start, CurrentMonth);

        Rows = Enumerable.Range(0, 6)
            .Select(i => start.AddMonths(i))
            .Select(m => new BudgetRow(
                m,
                totals.TryGetValue(m, out var spent) ? spent : 0m,
                overrides.TryGetValue(m, out var custom) ? custom : DefaultBudget,
                overrides.ContainsKey(m),
                m == CurrentMonth))
            .ToList();
    }
}
