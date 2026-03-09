namespace DebtDash.Web.Domain.Models;

public class LoanProfile
{
    public Guid Id { get; set; }
    public decimal InitialPrincipal { get; set; }
    public decimal AnnualRate { get; set; }
    public int TermMonths { get; set; }
    public DateOnly StartDate { get; set; }
    public decimal FixedMonthlyCosts { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<PaymentLogEntry> Payments { get; set; } = [];
    public List<ProjectionSnapshot> Projections { get; set; } = [];
}
