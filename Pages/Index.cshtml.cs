using ExpenseTracker.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseTracker.Pages;

public class IndexModel(ExpenseService expenses) : PageModel
{
    public MonthSummary Summary { get; private set; } = default!;
    public DateOnly SelectedMonth { get; private set; }
    public DateOnly CurrentMonth { get; private set; } = Months.Current;

    public DateOnly PreviousMonth => SelectedMonth.AddMonths(-1);
    public DateOnly NextMonth => SelectedMonth.AddMonths(1);
    public bool CanGoForward => SelectedMonth < CurrentMonth;
    public bool IsCurrentMonth => SelectedMonth == CurrentMonth;

    public string SelectedMonthValue => Months.ToInputValue(SelectedMonth);
    public string CurrentMonthValue => Months.ToInputValue(CurrentMonth);

    public async Task OnGetAsync(string? month)
    {
        CurrentMonth = Months.Current;

        var requested = Months.TryParse(month) ?? CurrentMonth;
        SelectedMonth = requested > CurrentMonth ? CurrentMonth : requested;

        Summary = await expenses.GetMonthSummaryAsync(SelectedMonth);
    }
}
