using DebtDash.Web.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DebtDash.Web.Infrastructure.Persistence.Configurations;

public class LoanProfileConfiguration : IEntityTypeConfiguration<LoanProfile>
{
    public void Configure(EntityTypeBuilder<LoanProfile> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.InitialPrincipal).HasColumnType("decimal(18,2)");
        builder.Property(e => e.AnnualRate).HasColumnType("decimal(10,6)");
        builder.Property(e => e.FixedMonthlyCosts).HasColumnType("decimal(18,2)");
        builder.Property(e => e.CurrencyCode).HasMaxLength(3);

        builder.HasMany(e => e.Payments)
            .WithOne(p => p.LoanProfile)
            .HasForeignKey(p => p.LoanProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Projections)
            .WithOne(p => p.LoanProfile)
            .HasForeignKey(p => p.LoanProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
