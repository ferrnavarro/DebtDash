using DebtDash.Web.Domain.Models;

namespace DebtDash.Web.Domain.Services;

public interface IRateVarianceService
{
    RateVarianceRecord? EvaluateVariance(
        decimal calculatedRate,
        decimal? statedOrOverrideRate,
        Guid paymentLogEntryId);
}

public class RateVarianceService : IRateVarianceService
{
    private const decimal DefaultThresholdBasisPoints = 5.0m; // 0.05 percentage points = 5 basis points

    public RateVarianceRecord? EvaluateVariance(
        decimal calculatedRate,
        decimal? statedOrOverrideRate,
        Guid paymentLogEntryId)
    {
        if (statedOrOverrideRate is null)
            return null;

        var varianceAbsolute = Math.Abs(calculatedRate - statedOrOverrideRate.Value);
        var varianceBasisPoints = varianceAbsolute * 100m; // Convert percentage points to basis points
        var isFlagged = varianceBasisPoints > DefaultThresholdBasisPoints;

        return new RateVarianceRecord
        {
            Id = Guid.NewGuid(),
            PaymentLogEntryId = paymentLogEntryId,
            CalculatedRate = calculatedRate,
            StatedOrOverrideRate = statedOrOverrideRate,
            VarianceAbsolute = varianceAbsolute,
            VarianceBasisPoints = varianceBasisPoints,
            IsFlagged = isFlagged,
            ThresholdBasisPoints = DefaultThresholdBasisPoints,
            CreatedAt = DateTime.UtcNow
        };
    }
}
