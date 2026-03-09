# Tasks: Advanced Loan Comparison Dashboards

**Input**: Design documents from `/specs/001-advanced-loan-dashboards/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api.yaml, quickstart.md

**Tests**: Tests are REQUIRED by the constitution and are included for each user story at unit, integration, and end-to-end levels.

**Organization**: Tasks are grouped by user story so each story can be implemented, tested, and demonstrated independently.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare shared test scaffolding, validation hooks, and feature-level guardrails.

- [X] T001 Create dashboard comparison Playwright spec scaffold in tests/DebtDash.Web.E2ETests/specs/dashboard-comparison-summary.spec.ts
- [X] T002 [P] Create comparison scenario builder helpers in tests/DebtDash.Web.IntegrationTests/TestInfrastructure/ComparisonScenarioBuilder.cs
- [X] T003 [P] Create shared comparison test data helpers in tests/DebtDash.Web.UnitTests/Domain/ComparisonTestData.cs
- [X] T004 [P] Extend dashboard performance validation coverage in tests/DebtDash.Web.IntegrationTests/Performance/DashboardProjectionPerformanceTests.cs
- [X] T005 [P] Update dashboard UX state checklist for comparison flows in docs/ux/loan-tracker-checklist.md
- [X] T006 Update feature verification steps for comparison dashboards in specs/001-advanced-loan-dashboards/quickstart.md

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build the shared contract, calculation, and client plumbing required by all user stories.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T007 Add shared comparison DTOs and window enums in src/DebtDash.Web/Api/Contracts/ApiContracts.cs
- [X] T008 [P] Implement baseline comparison timeline calculator in src/DebtDash.Web/Domain/Calculations/ComparisonTimelineCalculator.cs
- [X] T009 [P] Extend dashboard aggregation orchestration for comparison payloads in src/DebtDash.Web/Domain/Services/DashboardAggregationService.cs
- [X] T010 Update dashboard endpoint query handling and response mapping in src/DebtDash.Web/Api/DashboardEndpoints.cs
- [X] T011 [P] Extend dashboard API client types and windowed fetch support in src/DebtDash.Web/ClientApp/src/services/dashboardApi.ts
- [X] T012 Add comparison logging and limited-data state plumbing in src/DebtDash.Web/Domain/Services/DashboardAggregationService.cs

**Checkpoint**: Shared comparison contracts and plumbing are ready; user stories can now be implemented independently.

---

## Phase 3: User Story 1 - Compare Actual Progress Against Baseline (Priority: P1) 🎯 MVP

**Goal**: Show side-by-side actual versus no-extra-principal summary information so the borrower can immediately tell whether they are ahead, on track, or behind.

**Independent Test**: Load a loan with and without extra principal payments and verify the dashboard summary cards and status text correctly show balance, payoff, and cost deltas against baseline.

### Tests for User Story 1 (REQUIRED) ⚠️

- [X] T013 [P] [US1] Add unit tests for comparison summary deltas and status outcomes in tests/DebtDash.Web.UnitTests/Domain/DashboardAggregationServiceTests.cs
- [X] T014 [P] [US1] Add dashboard summary integration coverage for GET /api/dashboard in tests/DebtDash.Web.IntegrationTests/Api/DashboardAndProjectionEndpointsTests.cs
- [X] T015 [P] [US1] Add end-to-end summary comparison scenarios in tests/DebtDash.Web.E2ETests/specs/dashboard-comparison-summary.spec.ts

### Implementation for User Story 1

- [X] T016 [P] [US1] Implement comparison summary derivation for actual-versus-baseline status in src/DebtDash.Web/Domain/Services/DashboardAggregationService.cs
- [X] T017 [P] [US1] Create comparison summary card component in src/DebtDash.Web/ClientApp/src/components/ComparisonSummaryCards.tsx
- [X] T018 [P] [US1] Create dashboard comparison status banner in src/DebtDash.Web/ClientApp/src/components/ComparisonStatusBanner.tsx
- [X] T019 [US1] Integrate summary cards and status banner into the dashboard page in src/DebtDash.Web/ClientApp/src/pages/DashboardPage.tsx
- [X] T020 [US1] Add empty, error, and limited-data messaging for summary comparison states in src/DebtDash.Web/ClientApp/src/pages/DashboardPage.tsx
- [X] T021 [US1] Validate summary accessibility and UX consistency requirements in docs/ux/loan-tracker-audit.md
- [X] T022 [US1] Validate summary refresh and API response budgets in tests/DebtDash.Web.IntegrationTests/Performance/DashboardProjectionPerformanceTests.cs

**Checkpoint**: User Story 1 is independently functional and can show current comparison value without the deeper chart views.

---

## Phase 4: User Story 2 - Understand Change Over Time (Priority: P2)

**Goal**: Show synchronized time-based charts and window controls so the borrower can see when actual behavior diverged from the original no-extra-principal path.

**Independent Test**: Load a history with several payments, switch dashboard windows, and confirm the balance and cumulative-cost charts show aligned actual and baseline series across time.

### Tests for User Story 2 (REQUIRED) ⚠️

- [X] T023 [P] [US2] Add unit tests for comparison timeline windowing and series alignment in tests/DebtDash.Web.UnitTests/Domain/DashboardAggregationServiceTests.cs
- [X] T024 [P] [US2] Add integration coverage for windowed dashboard series responses in tests/DebtDash.Web.IntegrationTests/Api/DashboardAndProjectionEndpointsTests.cs
- [X] T025 [P] [US2] Add end-to-end chart and window-switching scenarios in tests/DebtDash.Web.E2ETests/specs/dashboard-comparison-history.spec.ts

### Implementation for User Story 2

- [X] T026 [P] [US2] Implement windowed balance and cost series generation in src/DebtDash.Web/Domain/Services/DashboardAggregationService.cs
- [X] T027 [P] [US2] Create actual-versus-baseline balance chart component in src/DebtDash.Web/ClientApp/src/charts/ComparisonBalanceChart.tsx
- [X] T028 [P] [US2] Create actual-versus-baseline cumulative cost chart component in src/DebtDash.Web/ClientApp/src/charts/ComparisonCostChart.tsx
- [X] T029 [P] [US2] Create dashboard comparison window selector component in src/DebtDash.Web/ClientApp/src/components/DashboardWindowSelector.tsx
- [X] T030 [US2] Integrate comparison charts and window switching into the dashboard page in src/DebtDash.Web/ClientApp/src/pages/DashboardPage.tsx
- [X] T031 [US2] Add chart text alternatives and keyboard-accessible window controls in src/DebtDash.Web/ClientApp/src/pages/DashboardPage.tsx
- [X] T032 [US2] Validate chart rendering and window-switch performance budgets in tests/DebtDash.Web.IntegrationTests/Performance/DashboardProjectionPerformanceTests.cs

**Checkpoint**: User Stories 1 and 2 both work independently, and the historical comparison is visible across supported dashboard windows.

---

## Phase 5: User Story 3 - Focus on Meaningful Savings and Milestones (Priority: P3)

**Goal**: Surface milestone callouts and savings indicators so the borrower can quickly understand whether extra principal payments are worth continuing.

**Independent Test**: Use loans with no divergence, steady extra payments, and early payoff behavior to verify the dashboard explains milestones, interest avoided, months saved, and limited-data states without needing the ledger.

### Tests for User Story 3 (REQUIRED) ⚠️

- [X] T033 [P] [US3] Add unit tests for milestones, savings indicators, and limited-data states in tests/DebtDash.Web.UnitTests/Domain/DashboardAggregationServiceTests.cs
- [X] T034 [P] [US3] Add integration coverage for milestone and limited-data payload fields in tests/DebtDash.Web.IntegrationTests/Api/DashboardAndProjectionEndpointsTests.cs
- [X] T035 [P] [US3] Add end-to-end milestone and savings scenarios in tests/DebtDash.Web.E2ETests/specs/dashboard-comparison-milestones.spec.ts

### Implementation for User Story 3

- [X] T036 [P] [US3] Implement comparison milestone and savings derivation in src/DebtDash.Web/Domain/Services/DashboardAggregationService.cs
- [X] T037 [P] [US3] Create comparison milestones component in src/DebtDash.Web/ClientApp/src/components/ComparisonMilestones.tsx
- [X] T038 [P] [US3] Create savings highlights component in src/DebtDash.Web/ClientApp/src/components/ComparisonSavingsHighlights.tsx
- [X] T039 [US3] Integrate milestones, savings highlights, and limited-data copy into the dashboard page in src/DebtDash.Web/ClientApp/src/pages/DashboardPage.tsx
- [X] T040 [US3] Align milestone and savings API response contract details in src/DebtDash.Web/Api/Contracts/ApiContracts.cs
- [X] T041 [US3] Validate milestone accessibility and user-facing copy consistency in docs/ux/loan-tracker-audit.md
- [X] T042 [US3] Validate savings and milestone regression scenarios in tests/DebtDash.Web.IntegrationTests/Regression/DashboardComparisonRegressionTests.cs

**Checkpoint**: All user stories are independently functional, including milestone interpretation and savings-focused decision support.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final consistency, documentation, and regression work spanning multiple stories.

- [X] T043 [P] Document advanced comparison dashboard behavior in docs/api/true-cost-api.md
- [X] T044 Clean up superseded dashboard components and response mappings in src/DebtDash.Web/ClientApp/src/pages/DashboardPage.tsx
- [X] T045 [P] Add cross-story regression coverage for payment edits and dashboard refresh in tests/DebtDash.Web.IntegrationTests/Regression/DashboardComparisonRefreshRegressionTests.cs
- [X] T046 [P] Run full quickstart validation and update any mismatches in specs/001-advanced-loan-dashboards/quickstart.md
- [X] T047 [P] Run final cross-story UX consistency review in docs/ux/loan-tracker-audit.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; can start immediately.
- **Foundational (Phase 2)**: Depends on Setup completion and blocks all user stories.
- **User Story 1 (Phase 3)**: Depends on Foundational completion; delivers the MVP.
- **User Story 2 (Phase 4)**: Depends on Foundational completion and can be developed after or alongside US1, but is easiest once US1 comparison summary exists.
- **User Story 3 (Phase 5)**: Depends on Foundational completion and benefits from US1 and US2 comparison outputs being in place.
- **Polish (Phase 6)**: Depends on all target user stories being complete.

### User Story Dependencies

- **User Story 1 (P1)**: Starts after Phase 2 and has no dependency on other user stories.
- **User Story 2 (P2)**: Starts after Phase 2 and reuses shared comparison contracts, but remains independently testable.
- **User Story 3 (P3)**: Starts after Phase 2 and reuses shared comparison outputs, but remains independently testable.

### Within Each User Story

- Tests must be written and observed failing before implementation changes begin.
- Backend comparison derivation must precede frontend wiring for the same story.
- Reusable components should be created before page integration.
- UX and performance validation complete the story before it is considered done.

### Parallel Opportunities

- **Setup**: T002, T003, T004, and T005 can run in parallel.
- **Foundational**: T008, T009, and T011 can run in parallel after T007 starts the contract shape.
- **US1**: T013, T014, and T015 can run in parallel; T017 and T018 can run in parallel.
- **US2**: T023, T024, and T025 can run in parallel; T027, T028, and T029 can run in parallel.
- **US3**: T033, T034, and T035 can run in parallel; T037 and T038 can run in parallel.
- **Polish**: T043, T045, T046, and T047 can run in parallel.

---

## Parallel Example: User Story 1

```bash
# Run User Story 1 test work together:
Task: "Add unit tests for comparison summary deltas and status outcomes in tests/DebtDash.Web.UnitTests/Domain/DashboardAggregationServiceTests.cs"
Task: "Add dashboard summary integration coverage for GET /api/dashboard in tests/DebtDash.Web.IntegrationTests/Api/DashboardAndProjectionEndpointsTests.cs"
Task: "Add end-to-end summary comparison scenarios in tests/DebtDash.Web.E2ETests/specs/dashboard-comparison-summary.spec.ts"

# Build User Story 1 UI pieces together:
Task: "Create comparison summary card component in src/DebtDash.Web/ClientApp/src/components/ComparisonSummaryCards.tsx"
Task: "Create dashboard comparison status banner in src/DebtDash.Web/ClientApp/src/components/ComparisonStatusBanner.tsx"
```

## Parallel Example: User Story 2

```bash
# Run User Story 2 test work together:
Task: "Add unit tests for comparison timeline windowing and series alignment in tests/DebtDash.Web.UnitTests/Domain/DashboardAggregationServiceTests.cs"
Task: "Add integration coverage for windowed dashboard series responses in tests/DebtDash.Web.IntegrationTests/Api/DashboardAndProjectionEndpointsTests.cs"
Task: "Add end-to-end chart and window-switching scenarios in tests/DebtDash.Web.E2ETests/specs/dashboard-comparison-history.spec.ts"

# Build User Story 2 UI pieces together:
Task: "Create actual-versus-baseline balance chart component in src/DebtDash.Web/ClientApp/src/charts/ComparisonBalanceChart.tsx"
Task: "Create actual-versus-baseline cumulative cost chart component in src/DebtDash.Web/ClientApp/src/charts/ComparisonCostChart.tsx"
Task: "Create dashboard comparison window selector component in src/DebtDash.Web/ClientApp/src/components/DashboardWindowSelector.tsx"
```

## Parallel Example: User Story 3

```bash
# Run User Story 3 test work together:
Task: "Add unit tests for milestones, savings indicators, and limited-data states in tests/DebtDash.Web.UnitTests/Domain/DashboardAggregationServiceTests.cs"
Task: "Add integration coverage for milestone and limited-data payload fields in tests/DebtDash.Web.IntegrationTests/Api/DashboardAndProjectionEndpointsTests.cs"
Task: "Add end-to-end milestone and savings scenarios in tests/DebtDash.Web.E2ETests/specs/dashboard-comparison-milestones.spec.ts"

# Build User Story 3 UI pieces together:
Task: "Create comparison milestones component in src/DebtDash.Web/ClientApp/src/components/ComparisonMilestones.tsx"
Task: "Create savings highlights component in src/DebtDash.Web/ClientApp/src/components/ComparisonSavingsHighlights.tsx"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational.
3. Complete Phase 3: User Story 1.
4. Validate actual-versus-baseline summary behavior independently before expanding the dashboard.

### Incremental Delivery

1. Ship the shared comparison contracts and aggregation pipeline.
2. Deliver User Story 1 for current comparison understanding.
3. Deliver User Story 2 for historical divergence analysis.
4. Deliver User Story 3 for savings and milestone guidance.
5. Finish with cross-story regression, documentation, and UX/performance validation.

### Parallel Team Strategy

1. One developer completes Setup plus Foundational API and calculation work.
2. After Phase 2, one developer can own summary UX (US1), one can own chart UX (US2), and one can own milestone UX plus regression work (US3).
3. Merge stories in priority order, validating each story independently before moving on.

---

## Notes

- [P] tasks touch different files and can be executed in parallel.
- [US1], [US2], and [US3] map directly to the prioritized user stories in spec.md.
- Each story includes unit, integration, and end-to-end coverage because the constitution requires all three test layers for this feature.
- The active feature scripts reported a duplicate numeric prefix under specs/, so future spec automation should correct that repository issue even though it does not block this tasks file.