using System.Globalization;

namespace ExpenseTracker.Services;

/// <summary>Small helpers so month parsing lives in one place.</summary>
public static class Months
{
    public static DateOnly FirstOfMonth(DateOnly date) => new(date.Year, date.Month, 1);

    public static DateOnly Current => FirstOfMonth(DateOnly.FromDateTime(DateTime.Today));

    public static string ToInputValue(DateOnly month) =>
        month.ToString("yyyy-MM", CultureInfo.InvariantCulture);

    /// <summary>Parses the "yyyy-MM" string an input type=month posts.</summary>
    public static DateOnly? TryParse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateOnly.TryParseExact(
            value + "-01", "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None,
            out var parsed)
            ? parsed
            : null;
    }
}
