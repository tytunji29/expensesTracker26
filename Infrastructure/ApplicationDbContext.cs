using Microsoft.EntityFrameworkCore;
using expensesTracker26.Domain;

namespace expensesTracker26.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<IncomeSource> IncomeSources => Set<IncomeSource>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<BillsHolder> BillsHolders => Set<BillsHolder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure many-to-many relationship via BillsHolder
        modelBuilder.Entity<BillsHolder>()
            .HasOne(b => b.Expense)
            .WithMany(e => e.BillsHolders)
            .HasForeignKey(b => b.ExpenseId);

        modelBuilder.Entity<BillsHolder>()
            .HasOne(b => b.IncomeSource)
            .WithMany(i => i.BillsHolders)
            .HasForeignKey(b => b.IncomeSourceId);

        modelBuilder.Entity<BillsHolder>()
            .HasIndex(b => new { b.ExpenseId, b.IncomeSourceId })
            .IsUnique();
    }
}
