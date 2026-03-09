# Feature Specification: True Cost Loan Tracker

**Feature Branch**: `001-track-loan-true-cost`  
**Created**: 2026-03-06  
**Status**: Draft  
**Input**: User description: "A web-based financial tracking tool that lets users manually log loan payments, calculate real interest by actual elapsed days, detect rate variance, and forecast true payoff date and loan cost metrics."

## User Scenarios & Testing *(mandatory)*

<!--
  IMPORTANT: User stories should be PRIORITIZED as user journeys ordered by importance.
  Each user story/journey must be INDEPENDENTLY TESTABLE - meaning if you implement just ONE of them,
  you should still have a viable MVP (Minimum Viable Product) that delivers value.
  
  Assign priorities (P1, P2, P3, etc.) to each story, where P1 is the most critical.
  Think of each story as a standalone slice of functionality that can be:
  - Developed independently
  - Tested independently
  - Deployed independently
  - Demonstrated to users independently
-->

### User Story 1 - Configure Loan Baseline (Priority: P1)

As a borrower, I can define my loan's baseline terms so that all later payment logs
and real-cost calculations are grounded in a known starting point.

**Why this priority**: Without an accurate baseline, downstream calculations and
projections are not trustworthy.

**Independent Test**: Unit tests validate field rules and derived baseline values,
integration tests verify persisted setup is loaded correctly, and an end-to-end flow
confirms a user can create a loan profile and view initial balance and schedule context.

**Acceptance Scenarios**:

1. **Given** no loan has been configured, **When** the user enters initial principal,
  annual rate, term in months, start date, and recurring fixed costs, **Then** the
  loan profile is saved and shown as the baseline for tracking.
2. **Given** a loan profile exists, **When** the user updates one or more baseline
  fields, **Then** future projections and baseline comparisons reflect the new values
  while preserving historical payment entries.

---

### User Story 2 - Record and Audit Payments (Priority: P1)

As a borrower, I can log each payment event with principal, interest, fees, and date
details so I can audit what the bank charged and how my debt is actually shrinking.

**Why this priority**: Manual logging and audit visibility are the core value of the
tool and must work before advanced analytics.

**Independent Test**: Unit tests validate day-count and rate calculations,
integration tests verify ledger ordering and balance updates, and an end-to-end flow
confirms users can add/edit entries and see variance alerts.

**Acceptance Scenarios**:

1. **Given** a configured loan and at least one payment, **When** the user logs a new
  payment with date, total paid, principal, interest, and insurance/fees, **Then**
  the ledger adds the entry in chronological order with days since last payment,
  remaining balance, and calculated real rate.
2. **Given** a payment entry where calculated interest and logged interest diverge,
  **When** the user saves the entry without override, **Then** the system flags a
  rate variance for review.
3. **Given** a payment entry where the user enables manual rate override, **When**
  the user provides the bank-stated rate, **Then** the ledger preserves both
  calculated and overridden values with a clear variance indicator.

---

### User Story 3 - Monitor True Cost and Payoff Forecast (Priority: P2)

As a borrower, I can view dashboard metrics, trend charts, and a projected true end
date so I can understand real borrowing cost and the payoff impact of my behavior.

**Why this priority**: Decision support and motivation depend on clear progress and
forecast insight, but require payment history from previous stories.

**Independent Test**: Unit tests verify aggregate metric formulas, integration tests
validate projection recalculation after new logs, and end-to-end tests confirm users
can interpret KPI cards and chart trends from their recorded history.

**Acceptance Scenarios**:

1. **Given** multiple logged payments, **When** the user opens the dashboard, **Then**
  total interest paid, total capital paid, average real rate, and time remaining are
  displayed and compared against original term.
2. **Given** the user records extra principal payments over time, **When** projections
  are recalculated, **Then** the predicted true end date moves earlier and remaining
  months decrease.
3. **Given** payment history exists, **When** the user views trend visualizations,
  **Then** principal-vs-interest and debt countdown charts reflect chronological
  payment behavior.

---

### Edge Cases

- First payment log has no prior payment date; system uses loan start date for
  day-count calculations.
- Two payments are logged with the same date; system allows both and computes zero
  elapsed days for the second entry.
- Logged line-item components do not equal total paid; system blocks save and explains
  mismatch.
- Principal payment would reduce remaining balance below zero; system caps payoff and
  marks final payment as complete.
- User enters a historical payment out of chronological order; ledger reorders entries
  and recalculates dependent balances/projections.
- Manual override rate is provided without interest value or vice versa; system
  requires all linked fields before saving.
- Large gap between payments creates unusually high periodic interest; system shows
  variance context without treating it as an error.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow a user to create and edit a loan configuration with:
  initial principal, initial annual interest rate, total term in months, start date,
  and recurring fixed monthly costs.
- **FR-002**: System MUST persist loan configuration and all payment logs so users can
  return to prior history and continue tracking.
- **FR-003**: System MUST allow users to create, edit, and delete payment log entries
  with payment date, total paid, principal, interest, insurance/fees, and optional
  manual rate override.
- **FR-004**: System MUST calculate elapsed days between payment events using actual
  calendar days.
- **FR-005**: System MUST compute calculated interest for each period from remaining
  balance, annual rate, and days elapsed.
- **FR-006**: System MUST compute and display per-entry calculated real annual rate
  based on actual interest paid, applicable balance, and time elapsed.
- **FR-007**: System MUST flag rate variance when logged interest materially differs
  from calculated interest for the same period.
- **FR-008**: System MUST support a manual rate override mode and preserve both
  calculated and override rate context in the ledger.
- **FR-009**: System MUST present a chronological ledger with columns for date, days
  since last payment, principal, interest, insurance/fees, remaining balance, and
  calculated real rate.
- **FR-010**: System MUST track principal velocity by comparing actual principal
  reduction against expected reduction from the original term baseline.
- **FR-011**: System MUST project a true end date and remaining months based on
  observed payment behavior, updating projection after each log change.
- **FR-012**: System MUST provide dashboard KPIs for total interest paid, total capital
  paid, average real rate, and time remaining versus original schedule.
- **FR-013**: System MUST provide trend visualizations for principal-vs-interest mix
  and remaining balance countdown over time.
- **FR-014**: System MUST validate that payment components are internally consistent
  before accepting an entry.

### Quality & Maintainability Requirements

- **QR-001**: Changes MUST pass formatting, linting, and static analysis checks in CI.
- **QR-002**: Non-trivial implementation decisions MUST be documented in the feature
  plan or PR notes.

### Testing Requirements

- **TR-001**: New behavior MUST include unit tests for core logic and edge cases.
- **TR-002**: Boundary interactions MUST include integration tests.
- **TR-003**: Critical user journeys or contracts MUST include end-to-end or contract
  tests.
- **TR-004**: Failing tests MUST block merge and release.

### User Experience Consistency Requirements

- **UXR-001**: User-facing features MUST reuse existing components/tokens unless an
  approved exception is documented.
- **UXR-002**: Loading, empty, error, and success states MUST be explicitly defined.
- **UXR-003**: Accessibility expectations (keyboard, screen reader, contrast, etc.)
  MUST be documented and validated.

### Performance Requirements

- **PRF-001**: The feature MUST define measurable performance budgets before
  implementation.
- **PRF-002**: The feature MUST include a validation method for each budget.
- **PRF-003**: Budget regressions MUST be treated as release blockers unless an
  approved, time-bound exception exists.

### Assumptions

- The first release supports one loan profile per user workspace.
- Users provide line-item payment breakdown values from their statements manually.
- A variance threshold of 0.05 percentage points in periodic rate comparison is used
  to flag potential overcharge or fluctuation.
- Projections use the observed average principal payment behavior from recent history,
  while still exposing comparison to original term.
- All monetary values are entered and displayed in a single currency per loan.

### Key Entities *(include if feature involves data)*

- **Loan Profile**: Baseline loan definition including principal, annual rate, term,
  start date, fixed recurring costs, and original payoff target.
- **Payment Log Entry**: A dated payment event containing total paid, principal,
  interest, insurance/fees, optional override rate, calculated days-in-period,
  and resulting remaining balance.
- **Rate Variance Record**: A derived audit artifact that stores calculated rate,
  stated/override rate when present, variance amount, and status.
- **Projection Snapshot**: Derived forecast state containing predicted payoff date,
  remaining months, and comparison against original schedule.
- **Dashboard Metric Set**: Aggregated totals and averages for capital paid, interest
  paid, weighted real rate, and payoff progress.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 95% of users can complete initial loan setup in under 5 minutes without
  external assistance.
- **SC-002**: 98% of valid payment logs are saved and reflected in ledger totals in
  under 2 seconds.
- **SC-003**: For a reference set of amortization scenarios, displayed rate and
  interest calculations remain within 0.1% of expected financial results.
- **SC-004**: After at least 6 logged payments, true end-date projection error is
  within plus/minus 1 month for at least 85% of tracked loans.
- **SC-005**: At least 90% of users can correctly identify whether they are paying
  above or below expected schedule using dashboard KPIs and charts.
