# Implementation Plan: Advanced Loan Comparison Dashboards

**Branch**: `001-advanced-loan-dashboards` | **Date**: 2026-03-09 | **Spec**: `/Users/fernandomagana/Developer/mytools/DebtDash/specs/001-advanced-loan-dashboards/spec.md`
**Input**: Feature specification from `/specs/001-advanced-loan-dashboards/spec.md`

## Summary

Extend the existing DebtDash dashboard so borrowers can compare actual loan behavior
against a no-extra-principal baseline over time, using richer comparison summaries,
time-windowed trend graphs, and milestone deltas while preserving the current
single-loan ASP.NET Core plus React architecture and dashboard endpoint surface.

## Technical Context

**Language/Version**: C# 13 on .NET 10, TypeScript 5.9, React 19  
**Primary Dependencies**: ASP.NET Core Minimal APIs, EF Core 10 with SQLite,
FluentValidation, React Router, Recharts, ESLint 9  
**Storage**: SQLite single-file application database for persisted loan and payment data  
**Testing**: xUnit plus FluentAssertions for unit tests, ASP.NET Core integration
tests with WebApplicationFactory, Playwright for critical dashboard flows  
**Target Platform**: Modern desktop and mobile browsers with ASP.NET Core host on
macOS, Linux, and Windows
**Project Type**: Web application with a single ASP.NET Core host and Vite-powered
React client  
**Performance Goals**: Dashboard comparison summary load p95 under 300 ms from the
API for routine datasets, full dashboard render under 2 s for up to 5,000 payments,
payment edit to refreshed comparison view under 3 s for 95% of routine actions  
**Constraints**: Preserve single-loan scope, keep financial calculations
deterministic and auditable, reuse current dashboard interaction patterns, support
loading/empty/error/limited-data/success states, avoid introducing new infrastructure
services or asynchronous processing  
**Scale/Scope**: One active loan profile, up to 5,000 payment entries, comparison
views across full history plus bounded recent windows, one enriched dashboard API and
related frontend charts

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-Research Gates

- **Code Quality Gate**: PASS. Backend changes will continue to use compiler
  enforcement, existing solution build validation, and frontend ESLint. No new rule
  configuration is required; changed files must pass dotnet build, test projects, and
  client lint/build.
- **Testing Gate**: PASS. The feature requires unit tests for comparison timeline and
  savings calculations, integration tests for dashboard endpoint payloads across data
  conditions, and Playwright coverage for actual-versus-baseline dashboard review.
  Merge is blocked on any failing unit, integration, contract, or end-to-end test.
- **UX Consistency Gate**: PASS. The plan reuses current dashboard pages, KPI cards,
  charts, and state messaging patterns. Required states are loading, empty,
  limited-data, error, and refreshed success after payment edits. Accessibility checks
  include keyboard navigation, chart-adjacent textual summaries, heading hierarchy,
  contrast, and screen-reader-readable status text.
- **Performance Gate**: PASS. Budgets are defined in Technical Context. Validation
  will use seeded integration data, frontend build checks, and the existing
  performance/regression test lanes.
- **Observability & Simplicity Gate**: PASS. The design stays within the existing
  monolith and dashboard endpoint. Critical flows will log comparison generation and
  window selection context at service boundaries. No extra microservices, background
  jobs, or duplicate read models are planned.

### Post-Design Re-Check

- **Code Quality Gate**: PASS. The design centralizes comparison derivation in domain
  services and uses an explicit API contract, which keeps logic testable and avoids
  duplicated formulas across backend and UI.
- **Testing Gate**: PASS. Research, data model, contract, and quickstart artifacts now
  define concrete coverage for no-extra, steady-extra, retroactive-edit, and early-
  payoff scenarios.
- **UX Consistency Gate**: PASS. Design artifacts preserve the current dashboard route
  and expand it with comparison cards, synchronized charts, and required text
  explanations for empty or low-history states.
- **Performance Gate**: PASS. The contract favors a single comparison payload per
  dashboard view and bounded time windows, avoiding chatty fetch patterns and keeping
  render costs measurable.
- **Observability & Simplicity Gate**: PASS. The chosen model adds derived comparison
  snapshots to the response contract without introducing persisted duplicate truth.

## Project Structure

### Documentation (this feature)

```text
specs/001-advanced-loan-dashboards/
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
    │   ├── DashboardEndpoints.cs
    │   ├── Contracts/
    │   └── Validators/
    ├── Domain/
    │   ├── Calculations/
    │   ├── Models/
    │   └── Services/
    ├── Infrastructure/
    │   └── Persistence/
    └── ClientApp/
        ├── src/
        │   ├── charts/
        │   ├── components/
        │   ├── pages/
        │   └── services/
        └── public/

tests/
├── DebtDash.Web.UnitTests/
├── DebtDash.Web.IntegrationTests/
└── DebtDash.Web.E2ETests/
```

**Structure Decision**: Keep the existing single ASP.NET Core plus React web
application structure. Implement comparison calculations in backend domain services,
extend the dashboard API contract, and render the richer comparison views within the
existing client dashboard page and chart/component directories.

## Complexity Tracking

No constitutional violations identified that require justification.
