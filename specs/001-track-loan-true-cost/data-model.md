# Data Model: True Cost Loan Tracker

## LoanProfile

- Purpose: Stores baseline loan configuration used for all calculations and comparisons.
- Fields:
  - `id` (UUID)
  - `initialPrincipal` (decimal, > 0)
  - `annualRate` (decimal, >= 0)
  - `termMonths` (int, > 0)
  - `startDate` (date)
  - `fixedMonthlyCosts` (decimal, >= 0)
  - `currencyCode` (string, ISO 4217-like)
  - `createdAt` (datetime)
  - `updatedAt` (datetime)
- Validation rules:
  - Principal and term are required positive values.
  - Annual rate cannot be negative.
  - Start date cannot be null.
- Relationships:
  - One `LoanProfile` has many `PaymentLogEntry`.
  - One `LoanProfile` has many `ProjectionSnapshot`.

## PaymentLogEntry

- Purpose: Represents one manually logged payment event and derived period values.
- Fields:
  - `id` (UUID)
  - `loanProfileId` (UUID, FK)
  - `paymentDate` (date)
  - `totalPaid` (decimal, > 0)
  - `principalPaid` (decimal, >= 0)
  - `interestPaid` (decimal, >= 0)
  - `feesPaid` (decimal, >= 0)
  - `daysSincePreviousPayment` (int, >= 0, derived)
  - `remainingBalanceAfterPayment` (decimal, >= 0, derived)
  - `calculatedRealRate` (decimal, derived)
  - `manualRateOverrideEnabled` (bool)
  - `manualRateOverride` (decimal, nullable)
  - `createdAt` (datetime)
  - `updatedAt` (datetime)
- Validation rules:
  - `principalPaid + interestPaid + feesPaid` must equal `totalPaid` (within currency rounding tolerance).
  - `paymentDate` must be on or after loan start date.
  - Override value is required when override is enabled.
- Relationships:
  - Many `PaymentLogEntry` belong to one `LoanProfile`.
  - One `PaymentLogEntry` may have one `RateVarianceRecord`.
- State transitions:
  - `draft` -> `validated` on successful save.
  - Any update triggers full dependent recalculation for subsequent entries.

## RateVarianceRecord

- Purpose: Captures discrepancy between calculated and logged/overridden rate context for audit visibility.
- Fields:
  - `id` (UUID)
  - `paymentLogEntryId` (UUID, FK)
  - `calculatedRate` (decimal)
  - `statedOrOverrideRate` (decimal, nullable)
  - `varianceAbsolute` (decimal)
  - `varianceBasisPoints` (decimal)
  - `isFlagged` (bool)
  - `thresholdBasisPoints` (decimal)
  - `createdAt` (datetime)
- Validation rules:
  - `varianceAbsolute` is computed and cannot be user-supplied.
  - Flagging applies when variance exceeds configured threshold.
- Relationships:
  - One `RateVarianceRecord` belongs to one `PaymentLogEntry`.

## ProjectionSnapshot

- Purpose: Stores recomputed payoff forecast and velocity indicators at a point in time.
- Fields:
  - `id` (UUID)
  - `loanProfileId` (UUID, FK)
  - `asOfPaymentLogEntryId` (UUID, nullable FK)
  - `predictedEndDate` (date)
  - `remainingMonthsEstimate` (decimal)
  - `principalVelocity` (decimal)
  - `baselineRemainingMonths` (decimal)
  - `deltaMonthsVsBaseline` (decimal)
  - `createdAt` (datetime)
- Validation rules:
  - Remaining months cannot be negative.
  - Predicted end date must be on or after latest payment date.
- Relationships:
  - Many `ProjectionSnapshot` belong to one `LoanProfile`.

## DashboardMetricSet (Derived)

- Purpose: Aggregated read model for KPI cards and charts.
- Fields:
  - `totalInterestPaid` (decimal)
  - `totalCapitalPaid` (decimal)
  - `averageRealRateWeighted` (decimal)
  - `timeRemainingMonths` (decimal)
  - `originalTermMonths` (int)
  - `principalInterestTrendSeries` (array of time-series points)
  - `debtCountdownSeries` (array of time-series points)
- Validation rules:
  - Derived from persisted records; never directly persisted as authoritative source.

## Referential and Recalculation Rules

- Payment entries are stored chronologically for projection consistency.
- Insert/update/delete on `PaymentLogEntry` triggers recalculation of downstream entries,
  variance records, projection snapshots, and dashboard read model values.
- Monetary calculations use decimal precision with consistent rounding rules at service boundaries.
