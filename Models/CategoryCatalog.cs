namespace ExpenseTracker.Models;

/// <summary>
/// A spending category plus the bits of presentation the UI needs.
/// </summary>
public sealed record ExpenseCategory(string Name, string Emoji, string Color);

/// <summary>
/// Fixed list of categories. Kept in code rather than the database because a
/// student project doesn't need a whole table for ten strings.
/// </summary>
public static class CategoryCatalog
{
    public const string DefaultName = "Other";

    public static readonly IReadOnlyList<ExpenseCategory> All =
    [
        new("Food", "🍜", "#f97316"),
        new("Groceries", "🛒", "#22c55e"),
        new("Transport", "🚌", "#3b82f6"),
        new("Rent", "🏠", "#8b5cf6"),
        new("Utilities", "💡", "#eab308"),
        new("Health", "🩺", "#ef4444"),
        new("Fun", "🎮", "#ec4899"),
        new("Education", "📚", "#14b8a6"),
        new("Shopping", "🛍️", "#a855f7"),
        new(DefaultName, "📦", "#64748b"),
    ];

    public static IReadOnlyList<string> Names { get; } = All.Select(c => c.Name).ToArray();

    public static ExpenseCategory Get(string? name) =>
        All.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))
        ?? All[^1];
}
