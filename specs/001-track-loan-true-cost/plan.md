# Implementation Plan: True Cost Loan Tracker

**Branch**: `001-track-loan-true-cost` | **Date**: 2026-03-06 | **Spec**: `specs/001-track-loan-true-cost/spec.md`
**Input**: Feature specification from `/specs/001-track-loan-true-cost/spec.md`

## Summary

Build a .NET 10 web application using the React SPA template, with ASP.NET Core
Minimal APIs and SQLite persistence, to let users configure a loan, manually log
payments, compute real day-based rate behavior, detect variance against expected
interest, and visualize true cost/projection metrics in a dashboard.

## Technical Context

**Language/Version**: C# 13 on .NET 10, TypeScript + React 18  
**Primary Dependencies**: ASP.NET Core Minimal APIs, EF Core (SQLite provider), React SPA template client, Recharts for dashboard visualizations  
**Storage**: SQLite (single file database)  
**Testing**: xUnit + FluentAssertions (unit), ASP.NET Core integration tests via
WebApplicationFactory, Playwright for critical end-to-end flows  
**Target Platform**: Web browsers (latest Chrome/Edge/Firefox/Safari) with ASP.NET
Core server on macOS/Linux/Windows
**Project Type**: Web application (single ASP.NET host with React SPA client)  
**Performance Goals**: Save/payment recalculation response p95 < 2s for datasets up to
5,000 payment logs; dashboard initial load < 2s on local broadband; API p95 < 300ms
for read operations under normal single-user workload  
**Constraints**: No authentication/authorization in this phase; all monetary
calculations must be deterministic and auditable; explicit loading/empty/error/success
states for all data views; maintain simple single-loan-per-workspace model  
**Scale/Scope**: Single user workspace, one active loan profile, up to 5,000 payment
entries with full recalculation on insert/update/delete

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-Research Gates

- **Code Quality Gate**: PASS. Enforce `dotnet format`, compiler warnings as errors,
  nullable reference types, and frontend linting/formatting in CI.
- **Testing Gate**: PASS. Plan includes unit tests for formulas/validators,
  integration tests for Minimal API and persistence boundaries, plus e2e tests for
  setup-log-dashboard journey.
- **UX Consistency Gate**: PASS. Plan requires shared design tokens, standardized
  states (loading/empty/error/success), and accessibility checks for forms/tables.
- **Performance Gate**: PASS. Measurable budgets are defined in Technical Context and
  validated with benchmark-like seeded dataset tests.
- **Observability & Simplicity Gate**: PASS. Use simple monolithic host architecture;
  structured logging around calculation, projection, and variance detection paths.

### Post-Design Re-Check

- **Code Quality Gate**: PASS. Data model and contracts isolate domain calculations in
  testable services and avoid unnecessary abstractions.
- **Testing Gate**: PASS. Contracts and quickstart include concrete test commands and
  required test levels.
- **UX Consistency Gate**: PASS. Design artifacts define consistent ledger/dashboard
  states and error-handling UX expectations.
- **Performance Gate**: PASS. Contracts include endpoints optimized for aggregation and
  pagination; performance validation steps are documented in quickstart.
- **Observability & Simplicity Gate**: PASS. No premature decomposition into multiple
  services; projection/variance events are explicitly loggable.

## Project Structure

### Documentation (this feature)

```text
specs/001-track-loan-true-cost/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── api.yaml
└── tasks.md
```

### Source Code (repository root)

```text
src/
└── DebtDash.Web/
    ├── DebtDash.Web.csproj
    ├── Program.cs
    ├── Api/
    │   ├── LoanEndpoints.cs
    │   ├── PaymentEndpoints.cs
    │   ├── DashboardEndpoints.cs
    │   └── ProjectionEndpoints.cs
    ├── Domain/
    │   ├── Models/
    │   ├── Services/
    │   └── Calculations/
    ├── Infrastructure/
    │   ├── Persistence/
    │   └── Logging/
    └── ClientApp/
        ├── src/
        │   ├── components/
        │   ├── pages/
        │   ├── charts/
        │   ├── services/
        │   └── state/
        └── public/

tests/
├── DebtDash.Web.UnitTests/
├── DebtDash.Web.IntegrationTests/
└── DebtDash.Web.E2ETests/
```

**Structure Decision**: Use a single ASP.NET Core + React SPA host generated from the
.NET template to keep deployment and local setup simple while preserving separation of
API/domain/infrastructure/client concerns inside one solution.

## Complexity Tracking

No constitutional violations identified that require justification.
