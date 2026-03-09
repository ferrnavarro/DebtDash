using DebtDash.Web.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DebtDash.Web.Infrastructure.Persistence.Configurations;

public class ProjectionSnapshotConfiguration : IEntityTypeConfiguration<ProjectionSnapshot>
{
    public void Configure(EntityTypeBuilder<ProjectionSnapshot> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.RemainingMonthsEstimate).HasColumnType("decimal(10,2)");
        builder.Property(e => e.PrincipalVelocity).HasColumnType("decimal(18,4)");
        builder.Property(e => e.BaselineRemainingMonths).HasColumnType("decimal(10,2)");
        builder.Property(e => e.DeltaMonthsVsBaseline).HasColumnType("decimal(10,2)");

        builder.HasIndex(e => e.LoanProfileId);
    }
}
