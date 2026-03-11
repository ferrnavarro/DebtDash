# Research: CSV Payment Import

## CSV Parsing Strategy

- **Decision**: Use `System.IO.StreamReader` with manual header detection and `string.Split(',')` — no new NuGet CSV parser package.
- **Rationale**: The import format is a fixed, controlled schema with no quoted multi-value fields or embedded commas in amounts. A lightweight line-by-line parser is sufficient, avoids new dependencies, and keeps the dependency graph clean. The 500-row / 2 MB limit means file size will never stress a naive parser.
- **Alternatives considered**: `CsvHelper` (popular NuGet package) was evaluated — excluded because introducing an external dependency for a simple fixed schema violates the "keep it simple" constitution principle. `System.Formats.Csv` was considered but is preview-stage in .NET 10 and brings API stability risk.

## Stateless vs. Stateful Import Preview

- **Decision**: Stateless two-call pattern — `POST /validate` returns parsed + validated rows to the client; `POST /confirm` accepts those rows back and persists them.
- **Rationale**: Eliminates the need for a server-side import session entity, DB table, or token cleanup job. The payload is small (≤ 500 rows × ~6 decimal fields) and fits comfortably in a single JSON request.
- **Alternatives considered**: A server-side import session with a correlation ID was considered to avoid round-tripping data. Rejected because it requires a new DB table or in-memory cache, adds a cleanup strategy, and provides no meaningful user benefit at the current scale. The spec explicitly favours "simplest design that meets current requirements" (Constitution Principle V).

## Derived Field Calculation

- **Decision**: Reuse `IFinancialCalculationService` and the same `RecalculateFromEntry` logic already used in `PaymentLedgerService.CreateAsync`. Loop over all confirmed imported rows and trigger recalculation in order (by `PaymentDate`).
- **Rationale**: The existing `CreateAsync` single-payment path already produces correct `DaysSincePreviousPayment`, `RemainingBalanceAfterPayment`, and `CalculatedRealRate` values. Reusing this path avoids duplicating financial logic and ensures imported payments are calculatee identically to manually entered ones.
- **Alternatives considered**: Pre-computing derived fields during the validate step was rejected because it would couple the validation preview to the loan state at preview time, which may change before confirmation. Computing only at confirm time ensures correctness.

## Duplicate Detection

- **Decision**: Compare incoming rows against existing `PaymentLogEntries` on (`LoanId`, `PaymentDate`, `TotalPaid`). Matches are skipped with a warning in the result, not rejected as errors.
- **Rationale**: Re-importing the same CSV after correcting some rows is a natural user workflow. Treating unchanged valid rows as errors would force manual cleanup. Skipping with an explicit warning gives visibility without blocking the rest of the import. The duplicate check is a single `WHERE ... IN` query on the confirmed rows list.
- **Alternatives considered**: Rejecting duplicates as validation errors (Option A) was considered — too disruptive for re-import workflows. Allowing all duplicates (Option C) was considered — rejected because it silently corrupts calculations.

## CSV Template Generation

- **Decision**: Generate the template dynamically in the endpoint from the same column-name constants used by the validator, ensuring the template is always in sync with the accepted format. The template includes one example row with safe placeholder values.
- **Rationale**: A hard-coded static template file can drift from the validator's expected headers. Generating it from shared constants is a one-line change and guarantees consistency.
- **Alternatives considered**: Serving a static file from `wwwroot` was considered — rejected due to template/validator drift risk.

## File Validation Strategy

- **Decision**: Validate MIME type and `.csv` extension on upload. Enforce 2 MB size limit using ASP.NET Core's `IFormFile.Length`. Enforce 500-row limit during line enumeration (abort after limit exceeded).
- **Rationale**: These are boundary validations that are quick and inexpensive and protect the server from processing malformed or oversized input before any CSV parsing begins.
- **Alternatives considered**: Relying only on the file extension was rejected as insufficient (any file can be renamed `.csv`). Deep content inspection was considered but is over-engineered for this use case.

## Frontend Integration Point

- **Decision**: Integrate the CSV import panel as a collapsible section on the existing Payments page, not a new route.
- **Rationale**: Adding a new route for a supplementary data-entry mechanism increases navigation complexity for a feature that is not used on every visit. A collapsible / modal panel keeps context (the loans/payments list) visible and follows the existing UX patterns already on the page.
- **Alternatives considered**: A dedicated `/payments/import` route was considered — rejected because it adds a navigation step and splits the import result from the payments list the user wants to verify afterward.

## Test Strategy for Performance Budget

- **Decision**: Add a specific integration test that uploads a 500-row fixture CSV, asserts HTTP 200 within 5 000 ms wall-clock time, and asserts the `validCount` equals 500.
- **Rationale**: The 5-second budget can be verified without a dedicated benchmarking framework by asserting response time in a standard integration test using `Stopwatch`.
- **Alternatives considered**: A separate benchmark project (BenchmarkDotNet) was considered but is heavyweight for a single two-budget check at this project scale.
