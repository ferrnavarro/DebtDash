# Tasks: True Cost Loan Tracker

**Input**: Design documents from `/specs/001-track-loan-true-cost/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Tests are required by the constitution and are included for each user story.

**Organization**: Tasks are grouped by user story so each story is independently implementable and testable.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Initialize .NET 10 React SPA solution, tooling, and project skeleton.

- [X] T001 Create .NET 10 React SPA project in src/DebtDash.Web/ using `dotnet new react`
- [X] T002 Create solution and add test projects in DebtDash.sln, tests/DebtDash.Web.UnitTests/, tests/DebtDash.Web.IntegrationTests/, tests/DebtDash.Web.E2ETests/
- [X] T003 [P] Add NuGet dependencies (EF Core SQLite, EF Core Design, FluentValidation) in src/DebtDash.Web/DebtDash.Web.csproj
- [X] T004 [P] Configure frontend chart dependency in src/DebtDash.Web/ClientApp/package.json
- [X] T005 [P] Configure code formatting and linting in .editorconfig and src/DebtDash.Web/ClientApp/.eslintrc.cjs
- [X] T006 [P] Configure static analysis and nullable/warnings-as-errors in src/DebtDash.Web/DebtDash.Web.csproj and Directory.Build.props
- [X] T007 Define baseline performance validation scripts in scripts/perf/validate-baseline.ps1 and scripts/perf/validate-baseline.sh
- [X] T008 Define shared UX state and accessibility checklist in docs/ux/loan-tracker-checklist.md

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish core architecture, persistence, calculation engine scaffolding, API wiring, and observability before story work.

**CRITICAL**: No user story implementation starts before this phase is complete.

- [X] T009 Create domain entities and value objects scaffold in src/DebtDash.Web/Domain/Models/
- [X] T010 Create DbContext and entity configurations in src/DebtDash.Web/Infrastructure/Persistence/DebtDashDbContext.cs and src/DebtDash.Web/Infrastructure/Persistence/Configurations/
- [X] T011 Create initial EF Core migration in src/DebtDash.Web/Infrastructure/Persistence/Migrations/
- [X] T012 [P] Configure SQLite connection and app settings in src/DebtDash.Web/appsettings.json and src/DebtDash.Web/appsettings.Development.json
- [X] T013 [P] Implement common validation pipeline and error response mapping in src/DebtDash.Web/Api/ValidationExtensions.cs and src/DebtDash.Web/Api/ErrorHandlingExtensions.cs
- [X] T014 [P] Implement structured logging setup for calculation/projection events in src/DebtDash.Web/Infrastructure/Logging/LoggingSetup.cs
- [X] T015 [P] Register Minimal API route groups and service composition root in src/DebtDash.Web/Program.cs
- [X] T016 Create financial calculation service interfaces and base implementations in src/DebtDash.Web/Domain/Calculations/
- [X] T017 Create projection and dashboard aggregation service skeletons in src/DebtDash.Web/Domain/Services/
- [X] T018 Create shared API DTO contracts in src/DebtDash.Web/Api/Contracts/
- [X] T019 Configure integration test host and SQLite test fixture in tests/DebtDash.Web.IntegrationTests/TestInfrastructure/
- [X] T020 Configure Playwright test runner and smoke setup in tests/DebtDash.Web.E2ETests/playwright.config.ts

**Checkpoint**: Foundation complete. User story phases can proceed.

---

## Phase 3: User Story 1 - Configure Loan Baseline (Priority: P1) MVP

**Goal**: Let users create and edit baseline loan configuration used by all calculations.

**Independent Test**: User can save and edit loan settings and retrieve them via API/UI with validated inputs and persisted state.

### Tests for User Story 1

- [X] T021 [P] [US1] Add unit tests for loan profile validation rules in tests/DebtDash.Web.UnitTests/Domain/LoanProfileValidationTests.cs
- [X] T022 [P] [US1] Add integration tests for GET/PUT /api/loan in tests/DebtDash.Web.IntegrationTests/Api/LoanEndpointsTests.cs
- [X] T023 [P] [US1] Add e2e test for loan setup form journey in tests/DebtDash.Web.E2ETests/specs/loan-setup.spec.ts

### Implementation for User Story 1

- [X] T024 [P] [US1] Implement LoanProfile entity and mapping in src/DebtDash.Web/Domain/Models/LoanProfile.cs and src/DebtDash.Web/Infrastructure/Persistence/Configurations/LoanProfileConfiguration.cs
- [X] T025 [US1] Implement loan profile repository/service in src/DebtDash.Web/Domain/Services/LoanProfileService.cs
- [X] T026 [US1] Implement loan profile endpoints in src/DebtDash.Web/Api/LoanEndpoints.cs
- [X] T027 [US1] Implement loan setup/edit UI page in src/DebtDash.Web/ClientApp/src/pages/LoanSetupPage.tsx
- [X] T028 [US1] Implement loan API client and state store in src/DebtDash.Web/ClientApp/src/services/loanApi.ts and src/DebtDash.Web/ClientApp/src/state/loanStore.ts
- [X] T029 [US1] Add loading/empty/error/success states and accessibility labels in src/DebtDash.Web/ClientApp/src/pages/LoanSetupPage.tsx
- [X] T030 [US1] Add audit logging for loan create/update operations in src/DebtDash.Web/Domain/Services/LoanProfileService.cs
- [X] T031 [US1] Validate US1 performance budget for loan read/write endpoints in tests/DebtDash.Web.IntegrationTests/Performance/LoanEndpointsPerformanceTests.cs

**Checkpoint**: US1 is functional and independently testable.

---

## Phase 4: User Story 2 - Record and Audit Payments (Priority: P1)

**Goal**: Let users create/edit/delete payment logs with day-based calculations and rate variance detection.

**Independent Test**: User can manage ledger entries and see recalculated balance, real rate, and variance flags after each change.

### Tests for User Story 2

- [X] T032 [P] [US2] Add unit tests for day-count, interest, and real-rate formulas in tests/DebtDash.Web.UnitTests/Domain/FinancialCalculationServiceTests.cs
- [X] T033 [P] [US2] Add integration tests for payments CRUD and recalculation in tests/DebtDash.Web.IntegrationTests/Api/PaymentEndpointsTests.cs
- [X] T034 [P] [US2] Add integration tests for rate variance threshold behavior in tests/DebtDash.Web.IntegrationTests/Api/RateVarianceTests.cs
- [X] T035 [P] [US2] Add e2e test for manual payment logging and variance visibility in tests/DebtDash.Web.E2ETests/specs/payment-ledger.spec.ts

### Implementation for User Story 2

- [X] T036 [P] [US2] Implement PaymentLogEntry and RateVarianceRecord entities in src/DebtDash.Web/Domain/Models/PaymentLogEntry.cs and src/DebtDash.Web/Domain/Models/RateVarianceRecord.cs
- [X] T037 [P] [US2] Implement entity configurations for payments and variance in src/DebtDash.Web/Infrastructure/Persistence/Configurations/PaymentLogEntryConfiguration.cs and src/DebtDash.Web/Infrastructure/Persistence/Configurations/RateVarianceRecordConfiguration.cs
- [X] T038 [US2] Implement financial calculation and variance services in src/DebtDash.Web/Domain/Calculations/FinancialCalculationService.cs and src/DebtDash.Web/Domain/Services/RateVarianceService.cs
- [X] T039 [US2] Implement payment ledger application service with downstream recalculation in src/DebtDash.Web/Domain/Services/PaymentLedgerService.cs
- [X] T040 [US2] Implement payments endpoints (GET/POST/PUT/DELETE) in src/DebtDash.Web/Api/PaymentEndpoints.cs
- [X] T041 [US2] Implement ledger table page and editor form in src/DebtDash.Web/ClientApp/src/pages/LedgerPage.tsx and src/DebtDash.Web/ClientApp/src/components/PaymentEntryForm.tsx
- [X] T042 [US2] Implement payment API client and store in src/DebtDash.Web/ClientApp/src/services/paymentApi.ts and src/DebtDash.Web/ClientApp/src/state/paymentStore.ts
- [X] T043 [US2] Implement manual rate override UX and rate variance indicators in src/DebtDash.Web/ClientApp/src/components/PaymentEntryForm.tsx and src/DebtDash.Web/ClientApp/src/components/RateVarianceBadge.tsx
- [X] T044 [US2] Add loading/empty/error/success states and keyboard navigation for ledger in src/DebtDash.Web/ClientApp/src/pages/LedgerPage.tsx
- [X] T045 [US2] Add structured logging for recalculation and variance events in src/DebtDash.Web/Domain/Services/PaymentLedgerService.cs
- [X] T046 [US2] Validate US2 performance budget for payment create/update/delete recalculation in tests/DebtDash.Web.IntegrationTests/Performance/PaymentRecalculationPerformanceTests.cs

**Checkpoint**: US2 is functional and independently testable.

---

## Phase 5: User Story 3 - Monitor True Cost and Payoff Forecast (Priority: P2)

**Goal**: Provide dashboard KPIs, projection outputs, and trend charts for true cost visibility.

**Independent Test**: User opens dashboard and sees accurate aggregate KPIs, projection date changes, and chart trends after payment updates.

### Tests for User Story 3

- [X] T047 [P] [US3] Add unit tests for projection and principal velocity formulas in tests/DebtDash.Web.UnitTests/Domain/ProjectionServiceTests.cs
- [X] T048 [P] [US3] Add unit tests for dashboard KPI aggregation in tests/DebtDash.Web.UnitTests/Domain/DashboardAggregationServiceTests.cs
- [X] T049 [P] [US3] Add integration tests for GET /api/dashboard and GET /api/projections/true-end-date in tests/DebtDash.Web.IntegrationTests/Api/DashboardAndProjectionEndpointsTests.cs
- [X] T050 [P] [US3] Add e2e test for dashboard metrics and chart rendering in tests/DebtDash.Web.E2ETests/specs/dashboard.spec.ts

### Implementation for User Story 3

- [X] T051 [P] [US3] Implement ProjectionSnapshot entity and configuration in src/DebtDash.Web/Domain/Models/ProjectionSnapshot.cs and src/DebtDash.Web/Infrastructure/Persistence/Configurations/ProjectionSnapshotConfiguration.cs
- [X] T052 [US3] Implement projection service for true end-date and remaining-month calculations in src/DebtDash.Web/Domain/Services/ProjectionService.cs
- [X] T053 [US3] Implement dashboard aggregation service for KPI and chart series in src/DebtDash.Web/Domain/Services/DashboardAggregationService.cs
- [X] T054 [US3] Implement dashboard and projection endpoints in src/DebtDash.Web/Api/DashboardEndpoints.cs and src/DebtDash.Web/Api/ProjectionEndpoints.cs
- [X] T055 [US3] Implement dashboard page and KPI cards in src/DebtDash.Web/ClientApp/src/pages/DashboardPage.tsx and src/DebtDash.Web/ClientApp/src/components/KpiCard.tsx
- [X] T056 [US3] Implement principal-vs-interest and debt-countdown charts in src/DebtDash.Web/ClientApp/src/charts/PrincipalInterestTrendChart.tsx and src/DebtDash.Web/ClientApp/src/charts/DebtCountdownChart.tsx
- [X] T057 [US3] Implement dashboard/projection API clients and state in src/DebtDash.Web/ClientApp/src/services/dashboardApi.ts and src/DebtDash.Web/ClientApp/src/state/dashboardStore.ts
- [X] T058 [US3] Add loading/empty/error/success states and accessibility semantics for dashboard in src/DebtDash.Web/ClientApp/src/pages/DashboardPage.tsx
- [X] T059 [US3] Add observability logs for projection recalculation and dashboard fetch in src/DebtDash.Web/Domain/Services/ProjectionService.cs and src/DebtDash.Web/Domain/Services/DashboardAggregationService.cs
- [X] T060 [US3] Validate US3 performance budget for dashboard and projection endpoints in tests/DebtDash.Web.IntegrationTests/Performance/DashboardProjectionPerformanceTests.cs

**Checkpoint**: US3 is functional and independently testable.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Finalize cross-story quality, documentation, and release readiness.

- [X] T061 [P] Update implementation and API documentation in docs/architecture/loan-tracker.md and docs/api/true-cost-api.md
- [X] T062 Perform cross-cutting refactor cleanup in src/DebtDash.Web/Domain/ and src/DebtDash.Web/Api/
- [X] T063 [P] Add regression integration suite for multi-step end-to-end API behavior in tests/DebtDash.Web.IntegrationTests/Regression/LoanLifecycleRegressionTests.cs
- [X] T064 [P] Execute cross-story UX consistency and accessibility audit in docs/ux/loan-tracker-audit.md
- [X] T065 Execute quickstart validation and update runbook notes in specs/001-track-loan-true-cost/quickstart.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: no dependencies.
- **Phase 2 (Foundational)**: depends on Phase 1 completion; blocks all user story work.
- **Phase 3 (US1)**: depends on Phase 2 completion.
- **Phase 4 (US2)**: depends on Phase 2 completion and integrates with US1 baseline loan profile.
- **Phase 5 (US3)**: depends on Phase 2 completion and uses payment history from US2 for meaningful dashboard outputs.
- **Phase 6 (Polish)**: depends on completion of all targeted user stories.

### User Story Dependencies

- **US1**: independent after foundational setup.
- **US2**: depends on US1 loan profile existing for meaningful payment tracking.
- **US3**: depends on US2 payment history for projections and dashboard metrics.

### Within Each User Story

- Write tests first and confirm they fail.
- Implement data model/configuration before services.
- Implement services before API endpoints.
- Implement API contracts before frontend integration.
- Complete story-level performance and UX validation before story checkpoint.

---

## Parallel Execution Examples

### User Story 1

```bash
# Parallel test authoring
Task: "T021 [US1] Unit tests for loan profile validation rules"
Task: "T022 [US1] Integration tests for GET/PUT /api/loan"
Task: "T023 [US1] E2E loan setup form journey"

# Parallel model + frontend service work
Task: "T024 [US1] Implement LoanProfile entity and mapping"
Task: "T028 [US1] Implement loan API client and state store"
```

### User Story 2

```bash
# Parallel test authoring
Task: "T032 [US2] Unit tests for day-count and rate formulas"
Task: "T033 [US2] Integration tests for payments CRUD"
Task: "T035 [US2] E2E payment logging and variance visibility"

# Parallel data model implementation
Task: "T036 [US2] Implement PaymentLogEntry and RateVarianceRecord entities"
Task: "T037 [US2] Implement entity configurations for payments and variance"
```

### User Story 3

```bash
# Parallel test authoring
Task: "T047 [US3] Unit tests for projection and principal velocity formulas"
Task: "T049 [US3] Integration tests for dashboard/projection endpoints"
Task: "T050 [US3] E2E dashboard metrics and chart rendering"

# Parallel dashboard UI work
Task: "T055 [US3] Implement dashboard page and KPI cards"
Task: "T056 [US3] Implement principal/interest and debt-countdown charts"
```

---

## Implementation Strategy

### MVP First (US1)

1. Complete Phase 1 and Phase 2.
2. Deliver Phase 3 (US1) completely.
3. Validate independent test criteria and performance for US1.
4. Demo baseline configuration flow.

### Incremental Delivery

1. Add US2 for full payment ledger and rate variance auditing.
2. Add US3 for projections and dashboard decision support.
3. Complete Phase 6 polish and full quickstart validation.

### Team Parallelization

1. One engineer focuses on backend services/endpoints while another builds frontend UI/state and a third builds tests in parallel using `[P]` tasks.
2. Re-sync at story checkpoints to keep each increment independently releasable.

---

## Notes

- All tasks follow required checklist format: `- [ ] T### [P] [US#] Description with file path`.
- `[US#]` labels appear only in user story phases.
- No auth tasks are included because security is explicitly out of scope for this increment.
- API contract tasks are integrated into story phases to keep each story independently testable.
