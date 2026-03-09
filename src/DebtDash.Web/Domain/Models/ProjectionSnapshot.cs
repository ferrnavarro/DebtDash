namespace DebtDash.Web.Domain.Models;

public class ProjectionSnapshot
{
    public Guid Id { get; set; }
    public Guid LoanProfileId { get; set; }
    public Guid? AsOfPaymentLogEntryId { get; set; }
    public DateOnly PredictedEndDate { get; set; }
    public decimal RemainingMonthsEstimate { get; set; }
    public decimal PrincipalVelocity { get; set; }
    public decimal BaselineRemainingMonths { get; set; }
    public decimal DeltaMonthsVsBaseline { get; set; }
    public DateTime CreatedAt { get; set; }

    public LoanProfile LoanProfile { get; set; } = null!;
}
