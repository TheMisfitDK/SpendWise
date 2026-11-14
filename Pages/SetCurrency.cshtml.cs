using ExpenseTracker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseTracker.Pages;

/// <summary>
/// Tiny endpoint behind the currency picker in the header. Stores the choice in a
/// cookie, then sends the browser straight back to the page it came from.
/// </summary>
public class SetCurrencyModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Index");

    public IActionResult OnPost(string? code, string? returnUrl)
    {
        var option = CurrencyService.Find(code);
        if (option is null)
        {
            return BadRequest();
        }

        Response.Cookies.Append(CurrencyService.CookieName, option.Code, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            HttpOnly = true,
        });

        return Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl!)
            : RedirectToPage("/Index");
    }
}
