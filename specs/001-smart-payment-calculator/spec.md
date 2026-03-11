# Feature Specification: Smart Monthly Payment Calculator with Live Rate Integration

**Feature Branch**: `001-smart-payment-calculator`
**Created**: 2026-03-11
**Status**: Draft
**Input**: User description: "Smart Monthly Payment Calculator with Live Rate Integration — calculates a precise monthly payment plan for an existing loan by specifying a desired payoff end date, distributing the outstanding balance across remaining periods, fetching the current interest rate in real time, defaulting insurance/ancillary fees from the last ledger entry, and presenting a transparent principal/interest/fee monthly breakdown."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Generate Payment Schedule by Payoff Date (Priority: P1)

As a borrower with an existing loan in the project, I can enter a target payoff
date and immediately receive a month-by-month payment schedule so that I know
exactly how much to pay each period to be debt-free on time.

**Why this priority**: This is the core value proposition of the feature. All other
stories augment this journey. Without a generated schedule, the feature does not exist.

**Independent Test**: Unit tests verify amortization math (principal/interest split,
remaining-balance reduction to zero by final period). Integration tests confirm the
calculator reads the live outstanding balance from the loan store and that the
resulting schedule is returned correctly. An end-to-end test walks a user through
entering a payoff date and confirms the schedule table renders with at least one row
per expected period.

**Acceptance Scenarios**:

1. **Given** an active loan with a known outstanding balance and current date,
   **When** the user selects a payoff end date that is at least one full month away,
   **Then** the system derives the number of remaining monthly periods, calculates the
   equal monthly payment required to eliminate the balance by that date, and displays
   the full schedule.
2. **Given** a generated payment schedule,
   **When** the user reviews it,
   **Then** every row shows the period number, due date, total payment, principal
   component, interest component, fee component, and remaining balance after payment.
3. **Given** a generated payment schedule,
   **When** the user reviews the final row,
   **Then** the remaining balance shown is zero (or within a rounding tolerance of
   ±$0.01).
4. **Given** the user enters a payoff date in the past or a date that produces zero
   remaining periods,
   **When** the system attempts to compute the schedule,
   **Then** a clear validation message is shown and no schedule is generated.

---

### User Story 2 - Live Rate Fetch at Calculation Time (Priority: P2)

As a borrower, I can trust that the interest rate used in my payment schedule
reflects current market conditions, not a stale stored value, so that my plan is
financially accurate at the moment I create it.

**Why this priority**: A schedule computed on an outdated rate misleads the user
about their true monthly obligation. Rate accuracy is a trust and compliance
requirement. It is placed below P1 because the schedule shape (user story 1) is
independently valuable even if the rate source is simplified.

**Independent Test**: Contract/integration tests verify the external rate source is
called during calculation and that the returned rate is applied to the amortization
formula. Unit tests cover fallback behavior when the rate source is unavailable.

**Acceptance Scenarios**:

1. **Given** the user initiates a payment schedule calculation,
   **When** the calculation begins,
   **Then** the system fetches the current interest rate in real time before computing
   any amortization figures, and the fetched rate is displayed alongside the schedule
   for transparency.
2. **Given** the live rate fetch fails (network error, timeout, or service outage),
   **When** the calculation is attempted,
   **Then** the system falls back to the most recently stored interest rate for the
   loan, displays a visible warning that the live rate could not be retrieved and which
   fallback rate was used, and still produces the schedule.
3. **Given** the fetched live rate differs from the previously stored rate,
   **When** the schedule is displayed,
   **Then** the new rate is shown and the user is informed that an updated rate was
   applied.

---

### User Story 3 - Auto-Default Insurance and Ancillary Fees (Priority: P3)

As a borrower, I do not need to re-enter my recurring loan fees each time I run a
calculation, because the system defaults to the most recent fee I recorded in my
payment ledger, ensuring continuity and reducing manual effort.

**Why this priority**: Fee defaults improve usability and reduce data-entry errors.
However, the calculator delivers its primary value even when fees are manually
entered, making this an enhancement rather than a blocker.

**Independent Test**: Unit tests verify the ledger-lookup logic for the most recent
fee entry (including the case when the ledger is empty). Integration tests confirm
the pre-populated fee value is drawn from the correct ledger record. An end-to-end
test confirms the fee field is pre-filled and the user can override it before
generating the schedule.

**Acceptance Scenarios**:

1. **Given** the payment ledger contains at least one historical payment entry with a
   fee amount,
   **When** the user opens the payment calculator,
   **Then** the fee input field is pre-populated with the fee value from the most
   recent ledger entry, and the source (last recorded payment date) is displayed as a
   hint.
2. **Given** the fee field is pre-populated,
   **When** the user clears or changes the value,
   **Then** the user-provided value is used for the calculation instead of the default.
3. **Given** no prior payment entries with fee data exist in the ledger,
   **When** the user opens the payment calculator,
   **Then** the fee field is empty and a placeholder prompts the user to enter a value;
   zero is treated as a valid explicit input.

---

### User Story 4 - Transparent Full-Cost Breakdown Summary (Priority: P4)

As a borrower, I can view a summary of total principal paid, total interest paid,
and total fees paid over the entire schedule so that I understand the real cost of
the loan before committing to the plan.

**Why this priority**: Transparency in the full cost of the loan is a stated goal of
the feature. It is placed P4 because it is a summary derived from the schedule
computed in P1 and can be added incrementally.

**Independent Test**: Unit tests verify the summation across all schedule rows for
each component. End-to-end test confirms the summary totals row is visible and
matches the sum of per-period values in the schedule table.

**Acceptance Scenarios**:

1. **Given** a fully generated payment schedule,
   **When** the user views the schedule,
   **Then** a summary row or section below the schedule shows: total payments, total
   principal, total interest, and total fees across the entire plan.
2. **Given** the user changes the payoff date or fee value and regenerates the
   schedule,
   **When** the new schedule is displayed,
   **Then** the summary totals update to reflect the new plan.

---

### Edge Cases

- What happens when the outstanding loan balance is already zero? The calculator
  must detect this and inform the user that there is nothing to schedule.
- What happens when the target payoff date falls on a non-business day or
  mid-month? The system accepts any calendar date and aligns payment periods to
  calendar months from the current date.
- What happens when the live rate fetch returns a rate that is significantly higher
  than the stored rate (e.g., more than 5% difference)? The system displays a
  prominent notice so the user can verify the rate before proceeding.
- What if the ledger has fee entries with value zero? Zero is treated as a valid
  default (no fee), and the fee field is pre-populated with zero without forcing
  re-entry.
- What happens if the loan has no associated project balance (data integrity issue)?
  The calculator must surface a clear error and prevent schedule generation.
- What if the derived schedule requires hundreds of rows (e.g., a 30-year plan)?
  The schedule must render without degrading to the point of being unusable; virtual
  scrolling or pagination is acceptable.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST automatically load the current outstanding balance,
  interest rate history, and loan metadata from the active loan in the project
  context without requiring manual data entry.
- **FR-002**: Users MUST be able to specify a target payoff end date for the loan
  through a date-input control on the calculator screen.
- **FR-003**: System MUST derive the number of remaining monthly payment periods
  by calculating the number of whole calendar months between the current date and
  the specified end date.
- **FR-004**: System MUST fetch the current interest rate from the live rate source
  at the moment the user initiates a calculation.
- **FR-005**: When the live rate fetch fails, the system MUST fall back to the most
  recently stored interest rate for the loan and display a visible warning to the
  user identifying the fallback rate and its source date.
- **FR-006**: System MUST present an editable fee input field pre-populated with the
  fee amount from the most recent payment ledger entry.
- **FR-007**: When no prior fee amount exists in the ledger, the system MUST
  present an empty (or zero-valued) fee field and prompt the user to enter a value.
- **FR-008**: System MUST compute the equal monthly payment amount that fully
  amortizes the outstanding balance over the derived number of periods at the
  current interest rate, incorporating the specified fee per period.
- **FR-009**: System MUST present a full payment schedule as a tabular breakdown
  showing, for each period: period number, due date, total payment amount, principal
  component, interest component, fee component, and remaining balance.
- **FR-010**: System MUST display a summary section showing the total principal
  paid, total interest paid, total fees paid, and total amount paid across all periods.
- **FR-011**: System MUST reject end dates that produce zero or negative remaining
  payment periods and display a validation message to the user.
- **FR-012**: System MUST display the interest rate used in the calculation
  (whether live or fallback) prominently alongside the schedule.

### Quality & Maintainability Requirements

- **QR-001**: Changes MUST pass formatting, linting, and static analysis checks in CI.
- **QR-002**: Non-trivial implementation decisions MUST be documented in the
  feature plan or PR notes.

### Testing Requirements

- **TR-001**: Amortization calculation logic MUST be covered by unit tests with
  known input/output pairs, including boundary cases (single period, rate of zero,
  large principal).
- **TR-002**: Ledger fee-defaulting logic MUST have unit tests covering: ledger with
  entries, ledger empty, ledger entries all with zero fee.
- **TR-003**: Live rate fetch integration (success path and failure/fallback path)
  MUST be covered by integration tests.
- **TR-004**: The end-to-end flow from entering a payoff date to viewing a rendered
  schedule MUST be covered by at least one end-to-end test.
- **TR-005**: Failing tests MUST block merge and release.

### User Experience Consistency Requirements

- **UXR-001**: The calculator screen MUST reuse existing UI components and design
  tokens already established in the project unless an approved exception is documented.
- **UXR-002**: Loading state (while fetching live rate), empty state (no prior fee,
  zero balance), error state (rate fetch failure, invalid date), and success state
  (schedule rendered) MUST all be explicitly handled and communicated to the user.
- **UXR-003**: The fee input field, date selector, and schedule table MUST meet
  keyboard navigation and screen-reader accessibility standards consistent with the
  rest of the application.

### Performance Requirements

- **PRF-001**: The feature MUST define response-time budgets for live rate fetch
  and schedule rendering before implementation begins.
- **PRF-002**: Each budget MUST have a corresponding test or measurement method.
- **PRF-003**: Budget regressions MUST be treated as release blockers unless an
  approved, time-bound exception exists.

### Key Entities

- **Payment Schedule**: A computed, ordered set of monthly payment entries that
  maps a loan's remaining balance to zero by a specified end date; belongs to a
  single loan and a single calculation session.
- **Payment Period Entry**: One row in a Payment Schedule representing a single
  month; has a due date, total payment amount, principal component, interest
  component, fee component, and remaining balance after payment.
- **Rate Quote**: The interest rate value retrieved at calculation time along with
  its retrieval timestamp and whether it came from the live source or a stored
  fallback; associated with a Payment Schedule for audit purposes.
- **Fee Default**: The fee amount pre-populated from the most recent payment ledger
  entry or explicitly entered by the user; used uniformly across all periods in a
  schedule.

## Assumptions

- The project context always contains exactly one active loan to calculate against.
  If multi-loan support is needed in the future, scope expansion will be required.
- "Current interest rate" refers to the rate sourced from the live rate integration
  already available in the project (see `docs/api/true-cost-api.md`); the spec does
  not prescribe a new external source.
- Payment periods are monthly (not bi-weekly or weekly). Daily-interest accrual
  between uneven periods is out of scope.
- Equal monthly payments (level-payment amortization) are assumed. Graduated or
  balloon payment structures are out of scope.
- Fees are treated as a flat per-period addition rather than compounding into the
  principal.
- The calculator produces a projected plan only; it does not record payments or
  modify the loan ledger. Persisting the schedule for future reference is out of
  scope.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can generate a complete monthly payment schedule in under
  10 seconds from the moment they initiate the calculation (including live rate fetch).
- **SC-002**: The final remaining balance in any generated schedule is within ±$0.01
  of zero, ensuring mathematical correctness of the amortization model.
- **SC-003**: 100% of generated schedule rows display distinct, non-null values for
  principal, interest, fee, and remaining balance components.
- **SC-004**: When a live rate fetch fails, the fallback warning is visible and
  acknowledged by users in 100% of fallback scenarios, with no silent failures.
- **SC-005**: When a fee default is applied from the ledger, users see the source of
  the default value before initiating calculation, eliminating surprise about the fee
  used.
- **SC-006**: The payment schedule renders correctly for plans up to 360 periods
  (30 years) without noticeable degradation in the user interface.
