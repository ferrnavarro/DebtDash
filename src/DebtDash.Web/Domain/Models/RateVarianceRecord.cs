namespace DebtDash.Web.Domain.Models;

public class RateVarianceRecord
{
    public Guid Id { get; set; }
    public Guid PaymentLogEntryId { get; set; }
    public decimal CalculatedRate { get; set; }
    public decimal? StatedOrOverrideRate { get; set; }
    public decimal VarianceAbsolute { get; set; }
    public decimal VarianceBasisPoints { get; set; }
    public bool IsFlagged { get; set; }
    public decimal ThresholdBasisPoints { get; set; }
    public DateTime CreatedAt { get; set; }

    public PaymentLogEntry PaymentLogEntry { get; set; } = null!;
}
