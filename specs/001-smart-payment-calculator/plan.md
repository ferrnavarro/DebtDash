# Implementation Plan: Smart Monthly Payment Calculator with Live Rate Integration

**Branch**: `001-smart-payment-calculator` | **Date**: 2026-03-11 | **Spec**: `specs/001-smart-payment-calculator/spec.md`
**Input**: Feature specification from `/specs/001-smart-payment-calculator/spec.md`

## Summary

Extend the existing DebtDash web application with a payment calculator screen that
takes a user-specified payoff end date, derives the remaining monthly periods, reads
the current interest rate from the most recent payment ledger entry's `CalculatedRealRate`
(falling back to `LoanProfile.AnnualRate` when the ledger is empty), optionally
pre-populates the fee from the most recent payment ledger entry, applies standard
level-payment (PMT) amortization to the outstanding balance, and returns a full
month-by-month breakdown of principal, interest, fee, and remaining balance alongside
a total-cost summary.

## Technical Context

**Language/Version**: C# 13 on .NET 10, TypeScript 5.9 + React 19
**Primary Dependencies**: ASP.NET Core Minimal APIs, EF Core 10 (SQLite), FluentValidation 12,
  React 19, Recharts 3, react-router-dom 7
**Storage**: SQLite (no new tables; schedule is computed in-memory and returned in the response)
**Testing**: xUnit + FluentAssertions (unit), WebApplicationFactory (integration), Playwright (e2e)
**Target Platform**: Web browsers (Chrome/Edge/Firefox/Safari) + ASP.NET Core on macOS/Linux/Windows
**Project Type**: Web application (single ASP.NET Core host with React SPA)
**Performance Goals**: Schedule calculation API response p95 < 500ms for any plan up to 360 periods;
  frontend schedule table renders 360 rows in ≤ 200 ms paint time
**Constraints**: Schedule is read-only projection (no persistence); single active loan per deployment;
  level-payment amortization (PMT) with 30/360 or monthly periods; monetary values rounded to 2dp;
  no authentication in this phase
**Scale/Scope**: Single user, one active loan profile; schedule up to 360 periods

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-Research Gates

- **Code Quality Gate**: PASS. Existing CI enforces `dotnet format` and frontend linting.
  New `CalculateMonthlyAmortizationSchedule` method on `IFinancialCalculationService`
  and new `IPaymentScheduleCalculatorService` follow existing naming/structure conventions.
  No new linting configuration required.
- **Testing Gate**: PASS. Unit tests required for: PMT formula (known inputs/outputs), period
  derivation, fee defaulting (3 cases), rate resolution (ledger present vs. empty-ledger
  fallback). Integration tests required for: schedule endpoint with seeded payments (ledger
  rate path), schedule endpoint with no payments (baseline fallback path). E2E test required
  for payoff date → schedule render user journey. All tests gate merge per TR-005.
- **UX Consistency Gate**: PASS. New `PaymentCalculatorPage` reuses existing KpiCard,
  loading/error/empty state patterns established in `DashboardPage`. DatePicker is a native
  HTML `<input type="date">` reusing existing form input styling. No new design tokens.
  Accessibility: fee input, date input, and schedule table must be keyboard-navigable and
  screen-reader labeled (consistent with `PaymentEntryForm`).
- **Performance Gate**: PASS. Budgets defined in Technical Context. Validated by:
  (a) integration test seeded with 360-period calculation timed against 500ms budget;
  (b) frontend Lighthouse run and manual render timing in tests.
- **Observability & Simplicity Gate**: PASS. `IPaymentScheduleCalculatorService` is a
  single focused service. No new infrastructure layers and no external HTTP dependencies.
  `ILogger` structured log events added to rate resolution (ledger rate used, baseline
  fallback used) and schedule calculation (loan id, periods, rate source). Simpler
  alternative (inline endpoint logic) rejected because calculation logic must be
  unit-testable in isolation.

### Post-Design Re-Check

- **Code Quality Gate**: PASS. Data model is all in-memory records; no EF migration needed.
  Contracts follow existing naming conventions. New endpoint registered in `Program.cs`
  following existing `MapGroup` pattern.
- **Testing Gate**: PASS. contracts/api.yaml and quickstart.md include test commands and
  concrete test scenarios for all required test levels.
- **UX Consistency Gate**: PASS. Loading/empty/error/success states defined in data-model.md
  and contracts. Fee default hint uses existing secondary text pattern.
- **Performance Gate**: PASS. No N+1 queries; schedule generation is pure in-memory math
  after a single `FirstOrDefaultAsync` + `OrderByDescending.FirstOrDefaultAsync` for the
  fee default. 360-period schedule is a simple loop.
- **Observability & Simplicity Gate**: PASS. No unnecessary abstractions. No `IRateProvider`
  interface needed — rate resolution is a ledger query with a null-check fallback, fully
  testable inline in `PaymentScheduleCalculatorService` unit tests.

## Project Structure

### Documentation (this feature)

```text
specs/001-smart-payment-calculator/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── api.yaml         # Phase 1 output
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src/
└── DebtDash.Web/
    ├── Api/
    │   ├── Contracts/
    │   │   └── ApiContracts.cs          # Add calculator contracts (request/response records)
    │   ├── Validators/
    │   │   └── Validators.cs            # Add PaymentScheduleRequestValidator
    │   └── CalculatorEndpoints.cs       # NEW: POST /api/calculator/schedule
    │                                    #      GET  /api/calculator/default-fee
    ├── Domain/
    │   ├── Calculations/
    │   │   └── FinancialCalculationService.cs  # Add CalculateMonthlyAmortizationSchedule
    │   └── Services/
    │       ├── PaymentScheduleCalculatorService.cs  # NEW: orchestrates loan load, rate, fee, schedule
    │       └── RateProvider.cs                      # NEW: IRateProvider + ExternalRateProvider
    └── ClientApp/
        └── src/
            ├── pages/
            │   └── PaymentCalculatorPage.tsx   # NEW: calculator screen
            ├── services/
            │   └── calculatorApi.ts            # NEW: API client for calculator endpoints
            └── App.tsx                         # Add /calculator route

tests/
├── DebtDash.Web.UnitTests/
│   └── Domain/
│       └── PaymentScheduleCalculatorTests.cs   # NEW: amortization math, fee defaulting, period derivation
├── DebtDash.Web.IntegrationTests/
│   └── Api/
│       └── CalculatorEndpointsTests.cs         # NEW: success path + fallback path integration tests
└── DebtDash.Web.E2ETests/
    └── specs/
        └── payment-calculator.spec.ts          # NEW: e2e payoff date → schedule render journey
```

**Structure Decision**: Single ASP.NET Core + React SPA host, consistent with the existing
project structure. No new projects or infrastructure layers added.

## Complexity Tracking

No constitutional violations identified that require justification.
