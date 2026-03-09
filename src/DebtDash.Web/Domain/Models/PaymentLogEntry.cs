namespace DebtDash.Web.Domain.Models;

public class PaymentLogEntry
{
    public Guid Id { get; set; }
    public Guid LoanProfileId { get; set; }
    public DateOnly PaymentDate { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal PrincipalPaid { get; set; }
    public decimal InterestPaid { get; set; }
    public decimal FeesPaid { get; set; }
    public int DaysSincePreviousPayment { get; set; }
    public decimal RemainingBalanceAfterPayment { get; set; }
    public decimal CalculatedRealRate { get; set; }
    public bool ManualRateOverrideEnabled { get; set; }
    public decimal? ManualRateOverride { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public LoanProfile LoanProfile { get; set; } = null!;
    public RateVarianceRecord? RateVariance { get; set; }
}
