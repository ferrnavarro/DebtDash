using DebtDash.Web.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DebtDash.Web.Infrastructure.Persistence.Configurations;

public class PaymentLogEntryConfiguration : IEntityTypeConfiguration<PaymentLogEntry>
{
    public void Configure(EntityTypeBuilder<PaymentLogEntry> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TotalPaid).HasColumnType("decimal(18,2)");
        builder.Property(e => e.PrincipalPaid).HasColumnType("decimal(18,2)");
        builder.Property(e => e.InterestPaid).HasColumnType("decimal(18,2)");
        builder.Property(e => e.FeesPaid).HasColumnType("decimal(18,2)");
        builder.Property(e => e.RemainingBalanceAfterPayment).HasColumnType("decimal(18,2)");
        builder.Property(e => e.CalculatedRealRate).HasColumnType("decimal(10,6)");
        builder.Property(e => e.ManualRateOverride).HasColumnType("decimal(10,6)");

        builder.HasIndex(e => new { e.LoanProfileId, e.PaymentDate });

        builder.HasOne(e => e.RateVariance)
            .WithOne(r => r.PaymentLogEntry)
            .HasForeignKey<RateVarianceRecord>(r => r.PaymentLogEntryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
