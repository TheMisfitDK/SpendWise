using ExpenseTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Data;

/// <summary>
/// Fills an empty database with six months of believable spending so the
/// dashboard isn't empty the first time you run it.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.Expenses.AnyAsync(ct))
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var firstMonth = new DateOnly(today.Year, today.Month, 1).AddMonths(-5);
        var random = new Random(20261113);
        var expenses = new List<Expense>();

        // Things that show up once a month, in roughly the same place.
        var monthly =
            new (string Description, string Category, decimal Min, decimal Max, int Day)[]
            {
                ("Rent", "Rent", 9500m, 11000m, 1),
                ("Electricity bill", "Utilities", 700m, 1600m, 4),
                ("Internet", "Utilities", 799m, 799m, 6),
                ("Phone plan", "Utilities", 299m, 299m, 6),
                ("Gym membership", "Health", 1200m, 1200m, 7),
                ("Streaming subscription", "Fun", 199m, 199m, 12),
            };

        // Things that happen zero or more times a month at random.
        var occasional =
            new (string Description, string Category, decimal Min, decimal Max, int MaxPerMonth)[]
            {
                ("Coffee and a pastry", "Food", 60m, 180m, 6),
                ("Canteen lunch", "Food", 80m, 220m, 8),
                ("Dinner with friends", "Food", 400m, 1200m, 2),
                ("Groceries run", "Groceries", 600m, 1800m, 4),
                ("Bus pass top-up", "Transport", 200m, 600m, 2),
                ("Taxi home", "Transport", 120m, 400m, 2),
                ("Pharmacy", "Health", 150m, 600m, 1),
                ("Textbook", "Education", 350m, 1200m, 1),
                ("Course materials", "Education", 100m, 500m, 1),
                ("Cinema ticket", "Fun", 200m, 400m, 1),
                ("New shirt", "Shopping", 500m, 1500m, 1),
                ("Phone case", "Shopping", 200m, 500m, 1),
            };

        var notes = new string?[] { null, null, null, "Paid by card", "Split with roommates", "Cash", null };

        for (var monthIndex = 0; monthIndex < 6; monthIndex++)
        {
            var monthStart = firstMonth.AddMonths(monthIndex);
            var isCurrentMonth = monthStart.Year == today.Year && monthStart.Month == today.Month;
            var lastDay = isCurrentMonth
                ? today.Day
                : DateTime.DaysInMonth(monthStart.Year, monthStart.Month);

            foreach (var (description, category, min, max, day) in monthly)
            {
                if (day > lastDay) continue;
                expenses.Add(Create(description, category, min, max, Day(monthStart, day), random, notes));
            }

            // Spending drifts a little up and down month to month.
            var volume = random.Next(70, 121) / 100.0;

            foreach (var (description, category, min, max, maxPerMonth) in occasional)
            {
                var count = random.Next(0, maxPerMonth + 1);
                for (var i = 0; i < count; i++)
                {
                    if (lastDay < 1) continue;
                    var day = random.Next(1, lastDay + 1);
                    expenses.Add(Create(description, category, min * (decimal)volume, max * (decimal)volume,
                        Day(monthStart, day), random, notes));
                }
            }
        }

        expenses.Sort((a, b) => a.SpentOn.CompareTo(b.SpentOn));

        db.Expenses.AddRange(expenses);
        await db.SaveChangesAsync(ct);
    }

    private static DateOnly Day(DateOnly monthStart, int day) =>
        new(monthStart.Year, monthStart.Month, day);

    private static Expense Create(
        string description, string category, decimal min, decimal max,
        DateOnly date, Random random, string?[] notes)
    {
        var amount = min + (decimal)random.NextDouble() * (max - min);

        return new Expense
        {
            Description = description,
            Category = category,
            Amount = Math.Round(amount, 2),
            SpentOn = date,
            Notes = notes[random.Next(notes.Length)],
            CreatedAt = DateTime.UtcNow,
        };
    }
}
