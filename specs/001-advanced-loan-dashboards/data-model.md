# Data Model: Advanced Loan Comparison Dashboards

## LoanProfile

- Purpose: Existing baseline loan configuration used to derive both actual and
  no-extra-principal comparison paths.
- Fields used by this feature:
  - id (UUID)
  - initialPrincipal (decimal, greater than 0)
  - annualRate (decimal, greater than or equal to 0)
  - termMonths (int, greater than 0)
  - startDate (date)
  - fixedMonthlyCosts (decimal, greater than or equal to 0)
  - currencyCode (string)
- Relationships:
  - One LoanProfile has many PaymentLogEntry records.
  - One LoanProfile produces one derived comparison view per requested dashboard
    window.

## PaymentLogEntry

- Purpose: Existing recorded payment events that define the actual path and influence
  divergence from the no-extra-principal baseline.
- Fields used by this feature:
  - id (UUID)
  - loanProfileId (UUID, FK)
  - paymentDate (date)
  - totalPaid (decimal)
  - principalPaid (decimal)
  - interestPaid (decimal)
  - feesPaid (decimal)
  - remainingBalanceAfterPayment (decimal, derived)
  - daysSincePreviousPayment (int, derived)
- Validation rules relevant to comparison:
  - Payment chronology must remain deterministic after insert, update, or delete.
  - Component totals must remain internally consistent before comparison refresh.
  - Retroactive payment changes trigger full downstream recalculation.

## DashboardWindow

- Purpose: Represents the requested comparison scope for the dashboard.
- Fields:
  - key (enum: full-history, trailing-6-months, trailing-12-months, year-to-date)
  - label (string)
  - rangeStart (date, derived)
  - rangeEnd (date, derived)
- Validation rules:
  - Key must be one of the supported presets.
  - Range must align to the borrower history available for the selected loan.

## ComparisonTimelinePoint

- Purpose: A dated derived point used by charts and summaries to compare actual and
  baseline behavior on the same timeline.
- Fields:
  - date (date)
  - actualRemainingBalance (decimal)
  - baselineRemainingBalance (decimal)
  - actualCumulativeInterest (decimal)
  - baselineCumulativeInterest (decimal)
  - actualCumulativePrincipal (decimal)
  - baselineCumulativePrincipal (decimal)
  - balanceDelta (decimal)
  - interestDelta (decimal)
  - payoffProgressDeltaMonths (decimal)
  - containsExtraPrincipalEffect (bool)
- Validation rules:
  - Each point must map to one shared chronological comparison position.
  - Delta values are derived only from actual and baseline values in the same point.
  - The sequence must be ordered ascending by date.

## ComparisonSummary

- Purpose: Top-level dashboard summary describing current status against the
  no-extra-principal baseline.
- Fields:
  - windowKey (enum)
  - currentStatus (enum: ahead, on-track, behind, insufficient-data)
  - monthsSaved (decimal)
  - projectedPayoffDateDelta (decimal months)
  - remainingBalanceDelta (decimal)
  - cumulativeInterestAvoided (decimal)
  - firstMeaningfulDivergenceDate (date, nullable)
  - lastRecalculatedAt (datetime)
  - explanatoryStateMessage (string)
- Validation rules:
  - Status must be derived from comparison deltas and limited-data rules.
  - Summary values must match the final visible timeline point in the selected window.
  - If data is insufficient, unsupported summary values remain null or explicitly not
    available rather than estimated without evidence.

## ComparisonMilestone

- Purpose: Explains meaningful moments in the actual-versus-baseline story without
  requiring the user to parse the full chart.
- Fields:
  - type (enum: divergence-start, highest-balance-gap, highest-interest-savings,
    early-payoff, overlap)
  - date (date)
  - title (string)
  - description (string)
  - value (decimal, nullable)
- Validation rules:
  - Milestones must be derivable from timeline data in the selected window.
  - Returned milestones must be chronologically valid and user-meaningful.

## DashboardComparisonResponse

- Purpose: Aggregate read model returned by the dashboard endpoint for the advanced
  comparison experience.
- Fields:
  - summary (ComparisonSummary)
  - balanceSeries (array of ComparisonTimelinePoint or chart-specific projections)
  - costSeries (array of ComparisonTimelinePoint or chart-specific projections)
  - milestones (array of ComparisonMilestone)
  - availableWindows (array of DashboardWindow)
  - activeWindow (DashboardWindow)
  - state (enum: ready, empty, limited-data)
- Validation rules:
  - All chart series and summary fields must be generated from the same loan and
    payment snapshot.
  - State must reflect whether meaningful comparison data exists for the selected
    window.

## Recalculation Rules

- Any loan profile update recalculates baseline comparison behavior.
- Any payment create, update, or delete recalculates actual timeline, baseline-aligned
  deltas, milestones, and summary outputs.
- Comparison outputs remain derived views and are not the authoritative source for
  loan or payment truth.