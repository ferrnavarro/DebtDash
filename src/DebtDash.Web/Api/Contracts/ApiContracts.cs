namespace DebtDash.Web.Api.Contracts;

public record LoanProfileResponse(
    Guid Id,
    decimal InitialPrincipal,
    decimal AnnualRate,
    int TermMonths,
    DateOnly StartDate,
    decimal FixedMonthlyCosts,
    string CurrencyCode);

public record LoanProfileUpsertRequest(
    decimal InitialPrincipal,
    decimal AnnualRate,
    int TermMonths,
    DateOnly StartDate,
    decimal FixedMonthlyCosts,
    string CurrencyCode);

public record PaymentUpsertRequest(
    DateOnly PaymentDate,
    decimal TotalPaid,
    decimal PrincipalPaid,
    decimal InterestPaid,
    decimal FeesPaid,
    bool ManualRateOverrideEnabled = false,
    decimal? ManualRateOverride = null);

public record PaymentLogEntryResponse(
    Guid Id,
    DateOnly PaymentDate,
    decimal TotalPaid,
    decimal PrincipalPaid,
    decimal InterestPaid,
    decimal FeesPaid,
    int DaysSincePreviousPayment,
    decimal RemainingBalanceAfterPayment,
    decimal CalculatedRealRate,
    bool ManualRateOverrideEnabled,
    decimal? ManualRateOverride,
    RateVarianceResponse? RateVariance);

public record RateVarianceResponse(
    decimal CalculatedRate,
    decimal? StatedOrOverrideRate,
    decimal VarianceAbsolute,
    decimal VarianceBasisPoints,
    bool IsFlagged);

public record PaymentListResponse(
    List<PaymentLogEntryResponse> Items,
    int Page,
    int PageSize,
    int TotalItems);

public record ProjectionSnapshotResponse(
    DateOnly PredictedEndDate,
    decimal RemainingMonthsEstimate,
    decimal PrincipalVelocity,
    decimal BaselineRemainingMonths,
    decimal DeltaMonthsVsBaseline);

public record DashboardResponse(
    decimal TotalInterestPaid,
    decimal TotalCapitalPaid,
    decimal AverageRealRateWeighted,
    decimal TimeRemainingMonths,
    int OriginalTermMonths,
    List<PrincipalInterestTrendPoint> PrincipalInterestTrendSeries,
    List<DebtCountdownPoint> DebtCountdownSeries);

public record PrincipalInterestTrendPoint(
    DateOnly Date,
    decimal PrincipalPaid,
    decimal InterestPaid);

public record DebtCountdownPoint(
    DateOnly Date,
    decimal RemainingBalance);
