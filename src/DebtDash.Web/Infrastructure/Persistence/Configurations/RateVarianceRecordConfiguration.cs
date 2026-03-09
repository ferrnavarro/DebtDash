using DebtDash.Web.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DebtDash.Web.Infrastructure.Persistence.Configurations;

public class RateVarianceRecordConfiguration : IEntityTypeConfiguration<RateVarianceRecord>
{
    public void Configure(EntityTypeBuilder<RateVarianceRecord> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.CalculatedRate).HasColumnType("decimal(10,6)");
        builder.Property(e => e.StatedOrOverrideRate).HasColumnType("decimal(10,6)");
        builder.Property(e => e.VarianceAbsolute).HasColumnType("decimal(10,6)");
        builder.Property(e => e.VarianceBasisPoints).HasColumnType("decimal(10,4)");
        builder.Property(e => e.ThresholdBasisPoints).HasColumnType("decimal(10,4)");
    }
}
