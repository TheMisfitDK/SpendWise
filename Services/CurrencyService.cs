using System.Globalization;

namespace ExpenseTracker.Services;

/// <summary>A currency the app knows how to display.</summary>
public sealed record CurrencyOption(string Code, string Symbol, string Name, string NumberCulture);

/// <summary>
/// Resolves the currency for the current request from a cookie, falling back to
/// the configured default. Injected into views so every page formats the same way.
/// </summary>
public class CurrencyService
{
    public const string CookieName = "spendwise.currency";

    /// <summary>Rupee first: that is the default for this project.</summary>
    public static readonly IReadOnlyList<CurrencyOption> Supported =
    [
        new("INR", "₹", "Indian Rupee", "en-IN"),
        new("USD", "$", "US Dollar", "en-US"),
        new("EUR", "€", "Euro", "en-IE"),
        new("GBP", "£", "Pound Sterling", "en-GB"),
    ];

    private readonly IHttpContextAccessor _http;
    private readonly CurrencyOption _fallback;

    public CurrencyService(IHttpContextAccessor http, IConfiguration config)
    {
        _http = http;
        _fallback = Find(config["App:DefaultCurrency"]) ?? Supported[0];
    }

    public CurrencyOption Current
    {
        get
        {
            var fromCookie = Find(_http.HttpContext?.Request.Cookies[CookieName]);
            return fromCookie ?? _fallback;
        }
    }

    public string Symbol => Current.Symbol;

    public static CurrencyOption? Find(string? code) =>
        string.IsNullOrWhiteSpace(code)
            ? null
            : Supported.FirstOrDefault(c => c.Code.Equals(code, StringComparison.OrdinalIgnoreCase));

    // "en-IN" groups digits as 1,23,456.78 instead of 123,456.78, which is what
    // you actually expect when the symbol is ₹.
    private CultureInfo Culture => CultureInfo.GetCultureInfo(Current.NumberCulture);

    public string Format(decimal value) =>
        Sign(value) + Symbol + Math.Abs(value).ToString("N2", Culture);

    public string FormatWhole(decimal value) =>
        Sign(value) + Symbol + Math.Abs(value).ToString("N0", Culture);

    private static string Sign(decimal value) => value < 0 ? "-" : string.Empty;
}
