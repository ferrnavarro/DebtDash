# Feature Specification: Advanced Loan Comparison Dashboards

**Feature Branch**: `001-advanced-loan-dashboards`  
**Created**: 2026-03-09  
**Status**: Draft  
**Input**: User description: "create more advanced dashboards and graphs to see how is the loan is in behaving in time, compared to how loan would evolve if no extra capital payments"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Compare Actual Progress Against Baseline (Priority: P1)

As a borrower, I can compare my actual loan progress against the same loan without
extra principal payments so I can tell whether my current behavior is helping me pay
off the debt faster or cheaper.

**Why this priority**: The core value of the request is not just showing more charts,
but making the effect of extra capital payments understandable through a meaningful
baseline comparison.

**Independent Test**: Using a loan with known payment history, confirm the dashboard
shows both actual and no-extra-payment trajectories and clearly states the current
difference in balance, payoff timing, and total cost.

**Acceptance Scenarios**:

1. **Given** a loan with payment history that includes extra principal payments,
   **When** the user opens the comparison dashboard, **Then** the dashboard shows
   actual progress and the no-extra-principal baseline side by side for the same
   timeline.
2. **Given** a loan with only scheduled payments and no extra principal,
   **When** the user opens the comparison dashboard, **Then** the dashboard shows
   that actual and baseline trajectories are aligned and explains that no acceleration
   has occurred yet.

---

### User Story 2 - Understand Change Over Time (Priority: P2)

As a borrower, I can view time-based charts that show how balance, interest, and
principal reduction changed over time so I can see when my payment behavior started
to diverge from the original path.

**Why this priority**: Historical patterns make the comparison actionable. Users need
to see when the gap opened, not just the latest totals.

**Independent Test**: Using a history with multiple payment periods, confirm the user
can review chronological charts and identify where extra principal payments created a
visible difference from the baseline.

**Acceptance Scenarios**:

1. **Given** multiple months of payment history, **When** the user reviews the
   comparison charts, **Then** the charts show the evolution of remaining balance and
   cumulative interest for both actual and baseline paths across time.
2. **Given** one or more extra principal payments were made in specific periods,
   **When** the user inspects the time-based view, **Then** the dashboard highlights
   the resulting acceleration in balance reduction and projected payoff timing after
   those periods.

---

### User Story 3 - Focus on Meaningful Savings and Milestones (Priority: P3)

As a borrower, I can see summary indicators for time saved, interest avoided, and
current payoff position so I can quickly understand whether my strategy is worth
continuing.

**Why this priority**: Summary indicators turn raw chart data into a decision aid and
help users quickly interpret the value of extra payments.

**Independent Test**: With comparison data available, confirm the dashboard surfaces
clear summary values that match the visible chart trends and remain understandable
without reviewing the full payment ledger.

**Acceptance Scenarios**:

1. **Given** a loan with enough history to project both actual and baseline outcomes,
   **When** the user views the dashboard, **Then** the dashboard shows current months
   saved, current interest difference, and whether the user is ahead of, on, or
   behind the no-extra-principal path.
2. **Given** the user adds, edits, or removes a payment that changes principal
   reduction, **When** the dashboard refreshes, **Then** the summary indicators update
   to reflect the revised comparison outcome.

### Edge Cases

- A loan has too little history to show a reliable trend; the dashboard explains the
  limitation while still showing available baseline context.
- No extra principal payments have been recorded; the dashboard shows overlap between
  actual and baseline paths instead of implying savings.
- A payment is corrected retroactively; all timeline comparisons and summary values
  recalculate using the revised history.
- A final payoff occurs earlier than the original schedule; charts and summaries stop
  the actual path at payoff while preserving the longer baseline path for comparison.
- An unusually large one-time extra payment creates a sharp divergence; the dashboard
  remains readable and preserves the relationship between the two paths.
- A selected time window contains no divergence between actual and baseline; the
  dashboard communicates that no measurable impact occurred during that window.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST generate a no-extra-principal baseline for each tracked loan
  using the original loan terms and scheduled payment behavior.
- **FR-002**: System MUST compare the actual loan path against the no-extra-principal
  baseline across a shared timeline.
- **FR-003**: System MUST show summary indicators for remaining balance difference,
  projected payoff time difference, and cumulative cost difference between actual and
  baseline paths.
- **FR-004**: System MUST provide at least one time-based visualization of remaining
  balance for actual and baseline paths on the same view.
- **FR-005**: System MUST provide at least one time-based visualization of cumulative
  borrowing cost for actual and baseline paths on the same view.
- **FR-006**: System MUST show how extra principal payments changed the loan trajectory
  over time, not only as a final total.
- **FR-007**: System MUST update all dashboard comparisons when loan terms or payment
  history change.
- **FR-008**: Users MUST be able to review comparison information for the full loan
  history and for shorter recent periods.
- **FR-009**: System MUST indicate whether the borrower is currently ahead of, on, or
  behind the no-extra-principal path.
- **FR-010**: System MUST explain when comparison outputs are limited because there is
  insufficient history or no meaningful divergence from baseline.
- **FR-011**: System MUST keep comparison figures and charts internally consistent so
  that summary indicators align with the detailed timeline view.
- **FR-012**: System MUST preserve chronological accuracy when historical payments are
  added, edited, or removed.

### Quality & Maintainability Requirements

- **QR-001**: The feature definition and delivered behavior MUST remain understandable
  to product and business stakeholders without relying on source-code knowledge.
- **QR-002**: Material comparison rules and assumptions MUST be documented so future
  enhancements preserve consistent financial meaning.

### Testing Requirements

- **TR-001**: Core comparison calculations MUST be validated with representative loan
  histories, including cases with no extra payments, steady extra payments, and large
  one-time extra payments.
- **TR-002**: End-to-end dashboard behavior MUST be validated for the primary user
  journey of reviewing actual-versus-baseline progress after payment changes.
- **TR-003**: Regression coverage MUST confirm that charted values and summary
  indicators continue to match after changes to loan data.
- **TR-004**: Release readiness MUST be blocked if comparison results become
  misleading, inconsistent, or unverifiable.

### User Experience Consistency Requirements

- **UXR-001**: The dashboard MUST present comparison views using the same visual and
  interaction patterns users already rely on elsewhere in the product, unless a
  documented exception is approved.
- **UXR-002**: Loading, empty, limited-data, error, and refreshed states MUST be
  explicitly defined for the comparison dashboard.
- **UXR-003**: Time-based charts and summary indicators MUST remain understandable for
  keyboard-only and assistive-technology users.

### Performance Requirements

- **PRF-001**: The dashboard MUST define measurable responsiveness targets for loading
  comparison summaries and historical graphs before implementation begins.
- **PRF-002**: The feature MUST define how those responsiveness targets will be
  checked with realistic loan histories.
- **PRF-003**: Performance regressions that materially reduce dashboard usability MUST
  be treated as release blockers unless a documented, time-bound exception exists.

### Assumptions

- The feature extends the existing single-loan tracking experience rather than adding
  multi-loan portfolio comparison.
- "Extra capital payments" means payment amounts applied above the scheduled
  principal reduction for the loan.
- The no-extra-principal baseline uses the borrower’s original loan terms and assumes
  the borrower only makes scheduled payments with no voluntary acceleration.
- Comparison views focus on helping users interpret behavior over time rather than
  replacing the detailed payment ledger.
- Recent-period views are limited to user-meaningful windows such as recent months or
  the full loan history, without requiring users to define custom analytical rules.

### Key Entities *(include if feature involves data)*

- **Baseline Comparison Path**: The expected loan evolution over time if the borrower
  makes no extra principal payments beyond the scheduled plan.
- **Actual Loan Path**: The observed loan evolution derived from the borrower’s real
  payment history.
- **Comparison Timeline Point**: A dated snapshot containing actual and baseline
  values for balance, cumulative cost, payoff progress, and variance.
- **Savings Summary**: The current comparison totals that express months saved,
  balance difference, interest difference, and acceleration status.
- **Dashboard View Window**: The selected history scope used to display either the
  full timeline or a shorter recent period.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: At least 90% of users can determine within 1 minute whether they are
  ahead of, on, or behind the no-extra-principal path.
- **SC-002**: At least 85% of users with recorded extra principal payments can
  correctly identify the periods where their payment behavior began reducing payoff
  time faster than the baseline.
- **SC-003**: For a reference set of loan histories, displayed comparison summaries
  stay within 1 payment period and 1% of expected balance and cost outcomes.
- **SC-004**: After a payment change is saved, updated dashboard summaries and graphs
  reflect the revised comparison in under 3 seconds for 95% of routine user actions.
- **SC-005**: At least 85% of users report that the dashboard makes the impact of
  extra principal payments understandable without needing to inspect the full ledger.
