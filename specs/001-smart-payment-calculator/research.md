# Research: Smart Monthly Payment Calculator with Live Rate Integration

## Amortization Formula

- **Decision**: Use standard level-payment (PMT) amortization with monthly compounding.
  Monthly payment M = P × [r(1+r)^n] / [(1+r)^n − 1], where P = outstanding balance,
  r = annualRate / 12 / 100, n = remaining monthly periods. The final period's principal
  is adjusted to absorb rounding so the ending balance is exactly zero.
- **Rationale**: PMT is the industry-standard formula for fixed monthly payments, directly
  matches the spec requirement of "equal monthly payment", and produces the deterministic
  breakdown required by FR-009. The existing `FinancialCalculationService` uses day-count
  interest (ACT/365) for payment logging; the calculator feature uses monthly-period math
  because it is projecting equal future payments, not reconstructing historical day-based accruals.
- **Alternatives considered**: Day-count amortization (ACT/365) was evaluated and rejected
  because it cannot produce truly equal monthly payments for projection purposes.

## Period Derivation

- **Decision**: Derive remaining periods as the total number of whole calendar months between
  today (calculation date) and the specified payoff end date, inclusive of the payoff month.
  Use `(endYear − currentYear) × 12 + (endMonth − currentMonth)` — if the result is ≤ 0,
  reject the request with a validation error (FR-011).
- **Rationale**: Calendar months map naturally to monthly payment schedules, are easy for
  users to reason about, and avoid fractional-period ambiguity.
- **Alternatives considered**: Counting 30-day periods was rejected because it creates
  discrepancies when displayed as month labels.

## Live Rate Source Design

- **Decision**: The "current interest rate" is the `CalculatedRealRate` from the most recent
  `PaymentLogEntry` (ordered by `PaymentDate` descending, then `CreatedAt` descending as
  tiebreaker). This is the rate actually computed from real payment data (principal
  reduction, interest charged, days elapsed) using the existing `FinancialCalculationService`
  formula. When the payment ledger is empty, the system falls back to `LoanProfile.AnnualRate`
  (the baseline configured rate) with `source: "baseline"` and `isFallback: true` in the
  response. No external HTTP service is involved.
- **Rationale**: The project has no external rate API. The most recently computed real rate
  from the ledger is the most accurate reflection of what the lender is currently applying —
  it comes from actual statement data rather than a nominal stated rate. This approach is
  entirely self-contained, requires no configuration, adds no network dependency, and is
  naturally unit-testable using the existing ledger query pattern already established in
  `PaymentLedgerService`. It fully satisfies FR-004 ("fetch current interest rate") by
  reading it from the live payment ledger, and FR-005 (fallback) by using the loan profile
  baseline when the ledger is empty.
- **Alternatives considered**:
  - Calling an external HTTP rate API was rejected because no such service is available in
    this deployment. Adding one would require API key management, network dependency, and
    timeout/retry infrastructure that is out of scope.
  - Using `LoanProfile.AnnualRate` unconditionally (ignoring ledger data) was rejected
    because the spec requires the "current" rate, and the ledger's computed rate is more
    current and accurate than the static configured baseline.

## Fee Defaulting Strategy

- **Decision**: Query the payment ledger for the most recent entry ordered by `PaymentDate`
  descending (then `CreatedAt` descending as tiebreaker). Return `FeesPaid` from that entry
  as the default. If no entries exist, return `null` (fee field empty). Zero is a valid default.
- **Rationale**: `FeesPaid` on `PaymentLogEntry` is the exact field that stores recurring
  ancillary fees per period, directly matching the spec's "last ledger entry fee" semantics.
- **Alternatives considered**: Using `LoanProfile.FixedMonthlyCosts` was considered but
  rejected because the spec specifically says "most recent fee recorded in the payment  ledger",
  implying the actual charged amount from the ledger rather than the baseline configuration field.

## Outstanding Balance Source

- **Decision**: Calculate outstanding balance as `LoanProfile.InitialPrincipal` minus the
  sum of all `PrincipalPaid` amounts in the payment ledger. If the payment ledger is empty,
  the outstanding balance equals `InitialPrincipal`. If the derived balance is ≤ 0, return
  an error (zero-balance edge case from the spec).
- **Rationale**: `RemainingBalanceAfterPayment` on the most recent payment entry could also
  be used, but computing it from the cumulative principal paid is more reliable in case of
  ledger inconsistency (belt-and-suspenders approach for a financial tool).
- **Alternatives considered**: Using `PaymentLogEntry.RemainingBalanceAfterPayment` from
  the latest entry was considered but rejected to avoid dependency on derived field
  consistency (it could be stale if a recalculation is in flight).

## No New Database Entities

- **Decision**: The computed schedule is returned in the API response only; it is not persisted.
- **Rationale**: The spec explicitly states "The calculator produces a projected plan only;
  it does not record payments or modify the loan ledger." No EF Core migration is needed.
- **Alternatives considered**: Persisting schedules for history/comparison was deferred per
  spec scope boundaries.

## Rate Change Warning Threshold

- **Decision**: Display a prominent UI warning when the ledger-derived `CalculatedRealRate`
  differs from the baseline `LoanProfile.AnnualRate` by more than 50 basis points
  (0.5 percentage points). No appsettings configuration is needed; 50 bp is hardcoded as
  a constant in `PaymentScheduleCalculatorService`.
- **Rationale**: 50 basis points represents a meaningful drift between the stated loan rate
  and what the lender is actually applying. Surfacing this prompts the user to verify the
  schedule is based on an accurate rate. The threshold is hardcoded (not configurable)
  because the single-user, single-loan scope has no need for per-deployment tuning, and
  configurable thresholds add complexity without delivering value (Principle V).
- **Alternatives considered**: The spec mentions "more than 5%" as an example edge case.
  5 percentage points absolute was rejected as too large for a practical warning signal;
  50 basis points (0.5pp) maps well to real-world rate adjustment increments.

## Testing Strategy for Rate Resolution

- **Decision**: Rate resolution logic (ledger rate vs. baseline fallback) lives in
  `PaymentScheduleCalculatorService`. Unit tests seed the service with ledger entries
  (rate derived from entries) and test the empty-ledger fallback path using 
  an empty payment list. Integration tests use `WebApplicationFactory` with a seeded
  SQLite database — one test with payments (expects `source: "ledger"`), one without
  (expects `source: "baseline"` and `isFallback: true`). No mocking of HTTP clients or
  external services required.
- **Rationale**: Because rate resolution is a pure in-memory query on already-loaded data,
  it can be fully tested at the unit level without any stubs or mocks. Integration tests
  validate the full DB-read-to-API-response path with minimal setup.
- **Alternatives considered**: Introducing an `IRateProvider` interface was considered to
  mirror the original external-service design, but rejected as unnecessary complexity
  (Principle V) — the fallback is a trivial null-check on the latest ledger entry, not a
  network circuit that needs to be substituted.

## Frontend State Management

- **Decision**: No global state store. The calculator page uses React `useState` for inputs
  (payoff date, fee amount) and the resulting schedule. `useEffect` loads the default fee
  on mount. Submission calls the API and updates local state.
- **Rationale**: The calculator is a self-contained page with no shared state requirements.
  Adding a global store (e.g., Zustand or Redux) would be over-engineering for this scope
  (Principle V: Keep It Simple).
- **Alternatives considered**: React Context was also considered and rejected for the same simplicity reason.
