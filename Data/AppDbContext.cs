using ExpenseTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Budget> Budgets => Set<Budget>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var expense = modelBuilder.Entity<Expense>();

        expense.ToTable("Expenses");
        expense.HasKey(e => e.Id);

        // SQLite has no real decimal type. Storing money as whole cents in an
        // INTEGER column keeps sums and sorting exact instead of floating point.
        expense.Property(e => e.Amount)
            .HasConversion(
                value => (long)Math.Round(value * 100m, MidpointRounding.AwayFromZero),
                cents => cents / 100m)
            .HasColumnType("INTEGER");

        expense.Property(e => e.Description).HasMaxLength(80).IsRequired();
        expense.Property(e => e.Category).HasMaxLength(30).IsRequired();
        expense.Property(e => e.Notes).HasMaxLength(200);

        // Every dashboard and list query filters or sorts on one of these.
        expense.HasIndex(e => e.SpentOn);
        expense.HasIndex(e => e.Category);

        var budget = modelBuilder.Entity<Budget>();
        budget.ToTable("Budgets");
        budget.HasKey(b => b.Id);
        budget.Property(b => b.Amount)
            .HasConversion(
                value => (long)Math.Round(value * 100m, MidpointRounding.AwayFromZero),
                cents => cents / 100m)
            .HasColumnType("INTEGER");

        // One budget per month.
        budget.HasIndex(b => b.Month).IsUnique();
    }
}
