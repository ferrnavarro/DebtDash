using DebtDash.Web.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DebtDash.Web.Infrastructure.Persistence;

public class DebtDashDbContext(DbContextOptions<DebtDashDbContext> options) : DbContext(options)
{
    public DbSet<LoanProfile> LoanProfiles => Set<LoanProfile>();
    public DbSet<PaymentLogEntry> PaymentLogEntries => Set<PaymentLogEntry>();
    public DbSet<RateVarianceRecord> RateVarianceRecords => Set<RateVarianceRecord>();
    public DbSet<ProjectionSnapshot> ProjectionSnapshots => Set<ProjectionSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DebtDashDbContext).Assembly);
    }
}
