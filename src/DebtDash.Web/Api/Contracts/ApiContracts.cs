namespace DebtDash.Web.Api.Contracts;

// ──────────────────────────────────────────────────────────────────────────────
// Comparison Dashboard Contracts (Feature 001-advanced-loan-dashboards)
// ──────────────────────────────────────────────────────────────────────────────

public enum DashboardWindowKey
{
    FullHistory,
    Trailing6Months,
    Trailing12Months,
    YearToDate,
}

public enum ComparisonStatus
{
    Ahead,
    OnTrack,
    Behind,
    InsufficientData,
}

public enum DashboardState
{
    Ready,
    Empty,
    LimitedData,
}

public enum MilestoneType
{
    DivergenceStart,
    HighestBalanceGap,
    HighestInterestSavings,
    EarlyPayoff,
    Overlap,
}

public record DashboardWindowResponse(
    DashboardWindowKey Key,
    string Label,
    DateOnly RangeStart,
    DateOnly RangeEnd);

public record ComparisonSummaryResponse(
    DashboardWindowKey WindowKey,
    ComparisonStatus CurrentStatus,
    decimal? MonthsSaved,
    decimal? ProjectedPayoffDateDelta,
    decimal? RemainingBalanceDelta,
    decimal? CumulativeInterestAvoided,
    DateOnly? FirstMeaningfulDivergenceDate,
    DateTime LastRecalculatedAt,
    string ExplanatoryStateMessage);

public record ComparisonTimelinePointResponse(
    DateOnly Date,
    decimal ActualRemainingBalance,
    decimal BaselineRemainingBalance,
    decimal ActualCumulativeInterest,
    decimal BaselineCumulativeInterest,
    decimal ActualCumulativePrincipal,
    decimal BaselineCumulativePrincipal,
    decimal BalanceDelta,
    decimal InterestDelta,
    decimal PayoffProgressDeltaMonths,
    bool ContainsExtraPrincipalEffect);

public record ComparisonMilestoneResponse(
    MilestoneType Type,
    DateOnly Date,
    string Title,
    string Description,
    decimal? Value);

public record DashboardComparisonResponse(
    ComparisonSummaryResponse Summary,
    List<ComparisonTimelinePointResponse> BalanceSeries,
    List<ComparisonTimelinePointResponse> CostSeries,
    List<ComparisonMilestoneResponse> Milestones,
    List<DashboardWindowResponse> AvailableWindows,
    DashboardWindowResponse ActiveWindow,
    DashboardState State);

// ──────────────────────────────────────────────────────────────────────────────
// Existing Contracts (preserved for backward compatibility)
// ──────────────────────────────────────────────────────────────────────────────

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

// ──────────────────────────────────────────────────────────────────────────────
// CSV Payment Import Contracts (Feature 001-csv-payment-import)
// ──────────────────────────────────────────────────────────────────────────────

public record CsvPaymentRow(
    int RowIndex,
    Guid LoanId,
    DateOnly PaymentDate,
    decimal TotalPaid,
    decimal PrincipalPaid,
    decimal InterestPaid,
    decimal FeesPaid);

public record CsvRowError(int RowIndex, List<string> Errors);

public record ImportPreviewResponse(
    int TotalRows,
    int ValidCount,
    int InvalidCount,
    List<CsvPaymentRow> ValidRows,
    List<CsvRowError> InvalidRows);

public record ImportConfirmRequest(List<CsvPaymentRow> Rows);

public record SkippedRowDetail(int RowIndex, string Reason);

public record ImportConfirmResponse(
    int ImportedCount,
    int SkippedCount,
    List<SkippedRowDetail> SkippedRows);
