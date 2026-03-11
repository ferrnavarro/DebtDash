# Tasks: Smart Monthly Payment Calculator with Live Rate Integration

**Input**: Design documents from `/specs/001-smart-payment-calculator/`
**Prerequisites**: plan.md ✅ | spec.md ✅ | research.md ✅ | data-model.md ✅ | contracts/ ✅

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1–US4)
- Exact file paths included in every task description

---

## Phase 1: Setup

**Purpose**: Wire the new endpoint group and service into the existing ASP.NET Core host.

- [X] T001 Register `PaymentScheduleCalculatorService` as a scoped DI service and add `MapCalculatorEndpoints()` route call in `src/DebtDash.Web/Program.cs`

---

## Phase 2: Foundational (Data Contracts & Validation)

**Purpose**: Shared C# and TypeScript contract types that every user story phase depends on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T002 Add C# records for all calculator contracts (PaymentScheduleRequest, PaymentScheduleResponse, SchedulePeriodEntry, ScheduleSummary, RateQuoteContext, FeeDefaultResponse) to `src/DebtDash.Web/Api/Contracts/ApiContracts.cs`
- [X] T003 [P] Add `PaymentScheduleRequestValidator` (payoffDate must produce ≥ 1 full calendar month; feeAmount ≥ 0 when non-null) to `src/DebtDash.Web/Api/Validators/Validators.cs`
- [X] T004 [P] Add TypeScript interfaces (PaymentScheduleRequest, PaymentScheduleResponse, SchedulePeriodEntry, ScheduleSummary, RateQuoteContext, FeeDefaultResponse) to `src/DebtDash.Web/ClientApp/src/services/calculatorApi.ts`

**Checkpoint**: Contracts defined — all user story phases can now begin.

---

## Phase 3: User Story 1 — Generate Payment Schedule by Payoff Date (Priority: P1) 🎯 MVP

**Goal**: User enters a target payoff date and receives a month-by-month amortization schedule where every period shows principal, interest, fee, and remaining balance, and the final balance is ≤ $0.01.

**Independent Test**: `POST /api/calculator/schedule` with a seeded loan profile and known balance returns HTTP 200 with `entries.length` equal to the derived period count and `entries[last].remainingBalance` within ±0.01 of zero.

### Tests for User Story 1 ⚠️

> **Write these first — they MUST fail before implementation begins.**

- [X] T005 [P] [US1] Unit tests for PMT formula (known principal/rate/period inputs, expected monthly payment), period derivation formula, and balance computation (InitialPrincipal − ΣPrincipalPaid); edge cases: single period, annualRate = 0, large principal (360 periods) in `tests/DebtDash.Web.UnitTests/Domain/PaymentScheduleCalculatorTests.cs`
- [X] T006 [P] [US1] Integration test for `POST /api/calculator/schedule` with seeded loan and payment entries: asserts HTTP 200, correct period count, final period `remainingBalance` ≈ 0 in `tests/DebtDash.Web.IntegrationTests/Api/CalculatorEndpointsTests.cs`
- [X] T007 [P] [US1] E2E test: navigate to `/calculator`, enter a payoff date at least one month out, click Calculate, assert schedule table renders with at least one row in `tests/DebtDash.Web.E2ETests/specs/payment-calculator.spec.ts`

### Implementation for User Story 1

- [X] T008 [P] [US1] Add `CalculateMonthlyAmortizationSchedule(decimal balance, decimal annualRate, int periods, decimal feePerPeriod)` method to `src/DebtDash.Web/Domain/Calculations/FinancialCalculationService.cs` (PMT formula M = P×[r(1+r)^n]/[(1+r)^n−1], period loop, final-period rounding adjustment to absorb ±$0.01)
- [X] T009 [US1] Create `PaymentScheduleCalculatorService` (load `LoanProfile` + `PaymentLogEntry` ledger via EF, compute outstanding balance as `InitialPrincipal − ΣPrincipalPaid`, resolve rate from most recent `PaymentLogEntry.CalculatedRealRate` or fall back to `LoanProfile.AnnualRate`, call `FinancialCalculationService.CalculateMonthlyAmortizationSchedule`, build `PaymentScheduleResponse`) in `src/DebtDash.Web/Domain/Services/PaymentScheduleCalculatorService.cs`
- [X] T010 [US1] Create `CalculatorEndpoints` with `POST /api/calculator/schedule` endpoint (validates with `PaymentScheduleRequestValidator`, delegates to `PaymentScheduleCalculatorService`, returns 200/400/404) in `src/DebtDash.Web/Api/CalculatorEndpoints.cs`
- [X] T011 [P] [US1] Add `postSchedule(request: PaymentScheduleRequest): Promise<PaymentScheduleResponse>` function to `src/DebtDash.Web/ClientApp/src/services/calculatorApi.ts`
- [X] T012 [US1] Create `PaymentCalculatorPage` with payoff date input, fee amount input, Calculate button, and schedule table (period, dueDate, totalPayment, principal, interest, fee, remainingBalance columns); handle loading, error, empty, and success states per data-model.md UI States in `src/DebtDash.Web/ClientApp/src/pages/PaymentCalculatorPage.tsx`
- [X] T013 [US1] Add `/calculator` route to the router and a navigation link to the main nav in `src/DebtDash.Web/ClientApp/src/App.tsx`

**Checkpoint**: User Story 1 is fully functional — enter a payoff date and receive a complete amortization schedule.

---

## Phase 4: User Story 2 — Rate Transparency at Calculation Time (Priority: P2)

**Goal**: The UI displays which interest rate was used and its source (ledger vs. baseline); fallback and rate-drift warnings are visibly communicated.

**Independent Test**: With a loan that has no payment entries, `POST /api/calculator/schedule` returns `rateQuote.source === "baseline"` and `rateQuote.isFallback === true`; the frontend renders a yellow warning banner. With a ledger entry whose `CalculatedRealRate` differs from `LoanProfile.AnnualRate` by > 50 bp, the frontend renders an orange rate-change notice.

### Tests for User Story 2 ⚠️

- [X] T014 [P] [US2] Unit tests for rate resolution: ledger path (most recent `CalculatedRealRate` used, source = "ledger"), empty-ledger fallback (source = "baseline", isFallback = true), rateChangeWarning = true when |CalculatedRealRate − AnnualRate| > 0.5 pp in `tests/DebtDash.Web.UnitTests/Domain/PaymentScheduleCalculatorTests.cs`
- [X] T015 [P] [US2] Integration test for baseline fallback path: `POST /api/calculator/schedule` with no payment entries returns `rateQuote.isFallback: true`, `rateQuote.source: "baseline"`, and uses `LoanProfile.AnnualRate` in the calculation in `tests/DebtDash.Web.IntegrationTests/Api/CalculatorEndpointsTests.cs`

### Implementation for User Story 2

- [X] T016 [P] [US2] Display rate info (annualRate value, source label: "Ledger" or "Baseline") in the schedule results header in `src/DebtDash.Web/ClientApp/src/pages/PaymentCalculatorPage.tsx`
- [X] T017 [US2] Implement yellow fallback warning banner when `rateQuote.isFallback === true` ("No payments recorded; using configured loan rate") in `src/DebtDash.Web/ClientApp/src/pages/PaymentCalculatorPage.tsx`
- [X] T018 [US2] Implement orange rate-change notice when `rateQuote.rateChangeWarning === true` (show baseline vs. ledger rate delta, prompt user to verify) in `src/DebtDash.Web/ClientApp/src/pages/PaymentCalculatorPage.tsx`

**Checkpoint**: User Story 2 complete — rate source and all warning states are visible in every scenario.

---

## Phase 5: User Story 3 — Auto-Default Insurance and Ancillary Fees (Priority: P3)

**Goal**: Fee input is pre-populated from the most recent payment ledger entry on page load; user can clear or override it; empty-ledger state is handled gracefully.

**Independent Test**: `GET /api/calculator/default-fee` returns `{ defaultFeeAmount: 75.00, sourcePaymentDate: "2026-02-15" }` when a ledger entry exists, and `{ defaultFeeAmount: null, sourcePaymentDate: null }` when the ledger is empty. Fee input on the page reflects the returned value immediately after mount.

### Tests for User Story 3 ⚠️

- [X] T019 [P] [US3] Unit tests for fee defaulting: ledger with entries (returns `FeesPaid` from most recent entry), ledger empty (returns null), most recent entry has zero fee (returns 0, not null) in `tests/DebtDash.Web.UnitTests/Domain/PaymentScheduleCalculatorTests.cs`
- [X] T020 [P] [US3] Integration tests for `GET /api/calculator/default-fee`: with seeded payment entries (expects fee value and source date) and with empty ledger (expects null fields) in `tests/DebtDash.Web.IntegrationTests/Api/CalculatorEndpointsTests.cs`
- [X] T021 [P] [US3] E2E test: open `/calculator`, assert fee input is pre-filled with ledger value, clear the field, enter a different fee value, generate schedule, and assert the overridden fee appears in the schedule entries in `tests/DebtDash.Web.E2ETests/specs/payment-calculator.spec.ts`

### Implementation for User Story 3

- [X] T022 [P] [US3] Add `GetDefaultFeeAsync()` method (query most recent `PaymentLogEntry` ordered by `PaymentDate` desc then `CreatedAt` desc; return `FeesPaid` and `PaymentDate`; return nulls when ledger is empty) to `src/DebtDash.Web/Domain/Services/PaymentScheduleCalculatorService.cs`
- [X] T023 [US3] Add `GET /api/calculator/default-fee` endpoint (delegates to `PaymentScheduleCalculatorService.GetDefaultFeeAsync()`, returns 200 with `FeeDefaultResponse` or 404 when no loan profile) to `src/DebtDash.Web/Api/CalculatorEndpoints.cs`
- [X] T024 [P] [US3] Add `getDefaultFee(): Promise<FeeDefaultResponse>` function to `src/DebtDash.Web/ClientApp/src/services/calculatorApi.ts`
- [X] T025 [US3] Implement `useEffect` fee pre-population on page mount (calls `getDefaultFee`, sets fee input value), source hint text ("From payment on {sourcePaymentDate}"), and empty-state placeholder text when ledger is empty in `src/DebtDash.Web/ClientApp/src/pages/PaymentCalculatorPage.tsx`

**Checkpoint**: User Story 3 complete — fee field pre-populates from the ledger and is freely overridable.

---

## Phase 6: User Story 4 — Transparent Full-Cost Breakdown Summary (Priority: P4)

**Goal**: A summary section below the schedule table shows total principal paid, total interest paid, total fees paid, and total amount paid across all periods; it updates on every recalculation.

**Independent Test**: After a successful schedule generation, the summary section is visible and `totalAmountPaid === totalPrincipal + totalInterest + totalFees` (verified by summing the column values in the schedule table via E2E test).

### Tests for User Story 4 ⚠️

- [X] T026 [P] [US4] Unit tests for `ScheduleSummary` computation: sum of all `principalComponent` values equals `totalPrincipal`, same for interest and fees, `totalAmountPaid` equals the three-way sum; edge case: single-period schedule in `tests/DebtDash.Web.UnitTests/Domain/PaymentScheduleCalculatorTests.cs`
- [X] T027 [P] [US4] E2E test: assert summary section is visible after schedule render; assert `totalAmountPaid` displayed equals the sum of `totalPayment` column values in the table in `tests/DebtDash.Web.E2ETests/specs/payment-calculator.spec.ts`

### Implementation for User Story 4

- [X] T028 [P] [US4] Verify `ScheduleSummary` is fully populated in `PaymentScheduleCalculatorService` (sum all period components: totalPrincipal, totalInterest, totalFees, totalAmountPaid = sum of all totalPayment; set periodCount) in `src/DebtDash.Web/Domain/Services/PaymentScheduleCalculatorService.cs`
- [X] T029 [US4] Implement summary section below the schedule table (total principal, total interest, total fees, total amount paid, period count) in `src/DebtDash.Web/ClientApp/src/pages/PaymentCalculatorPage.tsx`
- [X] T030 [US4] Verify summary section updates on recalculation (payoff date or fee change triggers new API call; React state replacement causes summary to re-render with updated totals) in `src/DebtDash.Web/ClientApp/src/pages/PaymentCalculatorPage.tsx`

**Checkpoint**: All user stories complete — the full feature is functional, transparent, and independently testable.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Structured logging, performance budget validation, accessibility, code hygiene.

- [X] T031 [P] Add structured `ILogger` events in `PaymentScheduleCalculatorService`: log rate source (ledger/baseline), loan ID, resolved period count, and computed outstanding balance on every schedule generation in `src/DebtDash.Web/Domain/Services/PaymentScheduleCalculatorService.cs`
- [X] T032 [P] Add 360-period performance integration test: `POST /api/calculator/schedule` with `payoffDate` = 30 years ahead must complete within 500ms; assert response time per PRF-001/PRF-002 in `tests/DebtDash.Web.IntegrationTests/Api/CalculatorEndpointsTests.cs`
- [X] T033 [P] Keyboard navigation and screen-reader accessibility audit: fee input and date input have aria-labels, schedule table uses `<th scope="col">`, tab order follows form → table; consistent with existing `PaymentEntryForm` per UXR-003 in `src/DebtDash.Web/ClientApp/src/pages/PaymentCalculatorPage.tsx`
- [X] T034 Run quickstart.md end-to-end validation: build app, start server, execute all curl commands in quickstart.md Section 5, confirm all responses match documented examples
- [X] T035 [P] Run `dotnet format` and `npm run lint` across all new and modified files; fix any violations before marking feature complete

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — can start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 — **BLOCKS** all user story phases
- **Phase 3 (US1)**: Depends on Phase 2 — core schedule generation
- **Phase 4 (US2)**: Depends on Phase 3 backend complete (reuses `PaymentScheduleCalculatorService`); frontend banners (T016–T018) can start after Phase 2
- **Phase 5 (US3)**: Depends on Phase 2 — independent of Phase 3 (separate endpoint and method); **can run in parallel with Phase 4**
- **Phase 6 (US4)**: Depends on Phase 3 backend (ScheduleSummary populated in service); frontend work (T029–T030) can start after Phase 2
- **Phase 7 (Polish)**: Depends on all desired user story phases being complete

### User Story Dependencies

| Story | Depends On | Can Parallelize With |
|---|---|---|
| US1 (P1) | Phase 2 | Nothing yet |
| US2 (P2) | US1 backend (T008–T009) | US3 (completely independent) |
| US3 (P3) | Phase 2 only | US1, US2 |
| US4 (P4) | US1 backend (T028 verify) | US2, US3 |

### Within Each User Story (TDD Order)

1. Write tests first (MUST fail before implementation)
2. Add domain calculation methods (`FinancialCalculationService`)
3. Implement service layer (`PaymentScheduleCalculatorService`)
4. Implement API endpoint (`CalculatorEndpoints`)
5. Implement frontend API client (`calculatorApi.ts`)
6. Implement UI page (`PaymentCalculatorPage`)

---

## Parallel Execution Examples

### Pair A — US1 and US3 in parallel (after Phase 2)
- Developer 1: T005 → T008 → T009 → T010 → T011 → T012 → T013 (US1 backend + frontend)
- Developer 2: T019 → T022 → T023 → T024 → T025 (US3 fee defaulting backend + frontend)

### Pair B — Tests and implementation in parallel within US1
- T005 (unit tests), T006 (integration test), T007 (E2E test) all [P] — write concurrently
- T008 (FinancialCalculationService) [P] — can be written concurrently with tests

### Pair C — Frontend banners in parallel (US2)
- T016, T017, T018 all touch the same file but are independent UI blocks — one developer can knock them out in sequence while another works on US3 frontend (T024–T025)

---

## Implementation Strategy

**Suggested MVP scope (US1 only)**:
Complete Phase 1 + Phase 2 + Phase 3. This delivers a fully functional payment schedule generator covering FR-001 through FR-003, FR-008, FR-009, FR-011, FR-012, SC-001 through SC-003, TR-001, TR-004, and the P95 < 500ms budget.

**Incremental delivery order**:
US1 (schedule table) → US2 (rate transparency) → US3 (fee pre-population) → US4 (cost summary)

Each story is independently deployable and testable against its own checkpoint.

---

## Task Summary

| Phase | Stories Covered | Task Count |
|---|---|---|
| Phase 1: Setup | — | 1 |
| Phase 2: Foundational | — | 3 |
| Phase 3: US1 (P1) | Generate Schedule | 9 |
| Phase 4: US2 (P2) | Rate Transparency | 5 |
| Phase 5: US3 (P3) | Fee Defaulting | 7 |
| Phase 6: US4 (P4) | Cost Summary | 5 |
| Phase 7: Polish | Cross-cutting | 5 |
| **Total** | | **35** |

**Parallelizable tasks**: 18 of 35 (marked [P])
**Independent test criteria**: Defined for each of the 4 user story phases
**Suggested MVP**: Phase 1 + Phase 2 + Phase 3 (13 tasks, US1 only)
