using Microsoft.EntityFrameworkCore;
using expensesTracker26.Domain;

namespace expensesTracker26.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<IncomeSource> IncomeSources => Set<IncomeSource>();
    public DbSet<IncomeSourceForTheMonth> IncomeSourcesForTheMonth => Set<IncomeSourceForTheMonth>();
    //public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<BillsHolder> BillsHolders => Set<BillsHolder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BillsHolder>()
            .HasOne(b => b.IncomeSource)
            .WithMany(i => i.BillsHolders)
            .HasForeignKey(b => b.IncomeSourceId);

        modelBuilder.Entity<BillsHolder>()
            .HasIndex(b => new { b.IncomeSourceId, b.MonthId, b.YearId });
        //.IsUnique();
    }
}
