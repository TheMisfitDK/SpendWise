using System.Globalization;
using ExpenseTracker.Models;
using ExpenseTracker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseTracker.Pages.Expenses;

public class IndexModel(ExpenseService expenses) : PageModel
{
    [FromQuery(Name = "q")] public string? Q { get; set; }
    [FromQuery(Name = "category")] public string? Category { get; set; }
    [FromQuery(Name = "month")] public string? Month { get; set; }
    [FromQuery(Name = "page")] public int PageNumber { get; set; } = 1;

    public ExpensePage Result { get; private set; } = default!;
    public IReadOnlyList<string> Categories => CategoryCatalog.Names;

    public async Task OnGetAsync()
    {
        var from = Months.TryParse(Month);
        var toExclusive = from?.AddMonths(1);

        Result = await expenses.SearchAsync(Q, Category, from, toExclusive, PageNumber);
        PageNumber = Result.PageNumber;
    }

    /// <summary>Keeps the filters when the pager moves to another page.</summary>
    public Dictionary<string, string> RouteFor(int page)
    {
        var route = new Dictionary<string, string>
        {
            ["page"] = page.ToString(CultureInfo.InvariantCulture),
        };

        if (!string.IsNullOrWhiteSpace(Q)) route["q"] = Q;
        if (!string.IsNullOrWhiteSpace(Category)) route["category"] = Category;
        if (!string.IsNullOrWhiteSpace(Month)) route["month"] = Month;

        return route;
    }
}
