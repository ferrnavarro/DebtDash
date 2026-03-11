# Data Model: Smart Monthly Payment Calculator with Live Rate Integration

All entities in this feature are **in-memory computed types** (C# records / TypeScript interfaces).
No new database tables or EF Core migrations are required.

---

## PaymentScheduleRequest

- **Purpose**: Input from the user to initiate a schedule calculation.
- **Fields**:
  - `payoffDate` (date) — the target month/year by which the loan must be fully paid off.
  - `feeAmount` (decimal, >= 0) — flat monthly fee applied to every period; defaults to the
    most recent ledger entry's `feesPaid` value if not explicitly provided.
- **Validation rules**:
  - `payoffDate` must produce at least 1 remaining monthly period from the current date.
  - `feeAmount` must be >= 0 when explicitly provided.
  - Outstanding loan balance must be > 0 at calculation time.

---

## PaymentScheduleResponse

- **Purpose**: Full computed payment schedule returned to the client.
- **Fields**:
  - `loanId` (UUID) — the loan this schedule is based on.
  - `outstandingBalance` (decimal) — the loan balance at calculation time.
  - `periods` (int) — total number of monthly payment periods.
  - `monthlyPaymentAmount` (decimal) — base PMT amount (principal + interest per period, excluding fee).
  - `feeAmountPerPeriod` (decimal) — the flat fee applied to each period.
  - `totalMonthlyAmount` (decimal) — `monthlyPaymentAmount + feeAmountPerPeriod`.
  - `rateQuote` (RateQuoteContext) — rate used and its source.
  - `entries` (array of SchedulePeriodEntry) — one entry per month.
  - `summary` (ScheduleSummary) — totals across all periods.
  - `calculatedAt` (datetime) — when this schedule was computed.

---

## SchedulePeriodEntry

- **Purpose**: One row in the payment schedule, representing a single monthly period.
- **Fields**:
  - `periodNumber` (int, 1-based) — sequence number of this payment in the schedule.
  - `dueDate` (date) — the first day of the month in which this payment is due, computed as
    current month + periodNumber months.
  - `principalComponent` (decimal) — portion of the payment that reduces the balance.
  - `interestComponent` (decimal) — `remainingBalance × (annualRate / 12 / 100)`, rounded to 2dp.
  - `feeComponent` (decimal) — the flat per-period fee.
  - `totalPayment` (decimal) — `principalComponent + interestComponent + feeComponent`.
  - `remainingBalance` (decimal) — balance after this payment (zero on the final period, adjusted
    for rounding so the final balance is ≥ 0 and ≤ 0.01).
- **Validation rules**:
  - `principalComponent` + `interestComponent` must equal `monthlyPaymentAmount` (within ±0.01
    for the last period where rounding adjustment is applied).
  - `remainingBalance` must be ≥ 0 for all periods and exactly 0 (±0.01) for the final period.
- **State transitions**: periods are ordered 1..n; period n always has `remainingBalance` ≈ 0.

---

## ScheduleSummary

- **Purpose**: Aggregated totals across the entire schedule for full-cost transparency.
- **Fields**:
  - `totalPrincipal` (decimal) — sum of all `principalComponent` values.
  - `totalInterest` (decimal) — sum of all `interestComponent` values.
  - `totalFees` (decimal) — sum of all `feeComponent` values.
  - `totalAmountPaid` (decimal) — sum of all `totalPayment` values; equals
    `totalPrincipal + totalInterest + totalFees`.
  - `periodCount` (int) — number of rows in the schedule (for display confirmation).

---

## RateQuoteContext

- **Purpose**: Documents the interest rate used in the schedule for transparency and auditability.
- **Fields**:
  - `annualRate` (decimal) — the annual interest rate applied (in percent, e.g., 5.5 for 5.5%).
  - `source` (string enum: `"ledger"` | `"baseline"`) — `"ledger"` when the rate is the
    `CalculatedRealRate` from the most recent payment ledger entry; `"baseline"` when the
    ledger is empty and the rate falls back to `LoanProfile.AnnualRate`.
  - `resolvedAt` (datetime) — when the rate was resolved (for audit trail display).
  - `isFallback` (bool) — `true` when no payment ledger entries exist and the stored baseline
    rate was used instead.
  - `fallbackReason` (string, nullable) — human-readable reason for fallback (e.g.,
    `"No payment ledger entries found; using configured loan rate"`); `null` when
    `isFallback` is `false`.
  - `rateChangedFromBaseline` (bool) — `true` when the ledger-derived rate differs from
    `LoanProfile.AnnualRate` (displayed as an informational notice, not a blocker).
  - `rateChangeWarning` (bool) — `true` when the absolute difference between the ledger
    rate and `LoanProfile.AnnualRate` exceeds 50 basis points (0.5 percentage points);
    prompts user to verify before proceeding.

---

## FeeDefaultResponse

- **Purpose**: Carries the pre-populated fee amount for the calculator input form.
- **Fields**:
  - `defaultFeeAmount` (decimal, nullable) — the `feesPaid` value from the most recent
    payment ledger entry; `null` if the ledger is empty.
  - `sourcePaymentDate` (date, nullable) — the `paymentDate` of the ledger entry used as the
    source, displayed as context hint in the UI; `null` if ledger is empty.
- **Validation rules**:
  - Zero is a valid `defaultFeeAmount`; the frontend must not treat it as "no default".

---

## UI States (Frontend)

| State    | Trigger                                                | Expected Behavior                                     |
|----------|--------------------------------------------------------|-------------------------------------------------------|
| Loading  | After user clicks "Calculate"; while ledger read + compute in progress | Spinner on submit button; form disabled      |
| Empty    | Payoff date not yet selected                           | Schedule table hidden; summary hidden                |
| Error    | API returns 400 (validation), 404 (no loan), or 5xx   | Inline error message near affected field; no schedule |
| Fallback | `rateQuote.isFallback === true`                        | Yellow warning banner: "No payments recorded; using configured loan rate" |
| Warning  | `rateQuote.rateChangeWarning === true`                 | Orange notice banner with baseline vs ledger rate delta  |
| Success  | Schedule loaded with entries                           | Table renders; summary row visible below             |
