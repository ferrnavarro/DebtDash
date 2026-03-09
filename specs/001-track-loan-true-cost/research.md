# Research: True Cost Loan Tracker

## Runtime and Project Template

- Decision: Use .NET 10 with the React SPA template (`dotnet new react`) as the base project, keeping a single host for API and SPA.
- Rationale: Matches the requested stack, minimizes deployment complexity, and enables shared configuration/logging for backend and frontend.
- Alternatives considered: Separate frontend/backend repositories were rejected because they add orchestration overhead without current scale needs.

## API Architecture

- Decision: Implement backend endpoints with ASP.NET Core Minimal APIs organized by feature areas (loan, payments, dashboard, projections).
- Rationale: Minimal APIs provide concise endpoint definitions and fast delivery for CRUD plus calculation-focused workflows.
- Alternatives considered: MVC controllers were rejected due to additional ceremony without clear benefit for this feature set.

## Persistence Strategy

- Decision: Use SQLite with EF Core for persistence and migrations.
- Rationale: SQLite satisfies the request for a lightweight local database, supports deterministic local development, and is sufficient for single-workspace scope.
- Alternatives considered: In-memory storage was rejected because persistence across sessions is required; PostgreSQL was deferred as unnecessary operational complexity.

## Financial Calculation Strategy

- Decision: Centralize financial formulas in a domain calculation service with deterministic decimal arithmetic and day-based period calculations.
- Rationale: Keeping formulas in one place improves auditability, repeatability, and testability for rate variance and projections.
- Alternatives considered: Computing formulas directly in endpoints or UI was rejected due to duplication and higher risk of inconsistency.

## Rate Variance Handling

- Decision: Compute expected interest from balance/day-count/rate and flag variance when periodic rate delta exceeds 0.05 percentage points; store both calculated and override contexts.
- Rationale: Provides transparent audit feedback while supporting real-world bank statement discrepancies.
- Alternatives considered: Hard-failing on mismatch was rejected because users need to record actual statements even when variance exists.

## Projection Method

- Decision: Recalculate projected payoff date using observed principal payment velocity with comparison against original term baseline.
- Rationale: Reflects user behavior changes (extra principal payments) and supports the "true end date" requirement.
- Alternatives considered: Static amortization-only projection was rejected because it cannot reflect manual overpayments realistically.

## Visualization Approach

- Decision: Use a charting library compatible with React SPA (Recharts) for principal-vs-interest trend and debt countdown views.
- Rationale: Recharts offers straightforward time-series and stacked-area visualizations with low setup overhead.
- Alternatives considered: Building custom SVG charts was rejected due to unnecessary implementation effort and maintenance burden.

## Testing Strategy

- Decision: Use xUnit for formula/service tests, WebApplicationFactory for API integration tests, and Playwright for critical end-to-end journeys.
- Rationale: This layered approach aligns with constitution testing gates and protects against regressions in calculations, persistence, and user workflows.
- Alternatives considered: Unit-tests-only strategy was rejected because it cannot validate API contracts and full user flows.

## Security Scope

- Decision: No authentication/authorization in the current feature increment.
- Rationale: Explicit user request is to defer security for now and prioritize functional correctness and financial transparency.
- Alternatives considered: Lightweight auth bootstrapping was deferred to avoid adding non-essential scope in this phase.
