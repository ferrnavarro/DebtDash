# Implementation Plan: CSV Payment Import

**Branch**: `001-csv-payment-import` | **Date**: March 9, 2026 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-csv-payment-import/spec.md`

## Summary

Allow users to bulk-import payment log entries by uploading a CSV file. The workflow is: upload → server-side parse and validate → preview (valid + invalid rows) → user confirms → batch persist. A downloadable CSV template is also provided. No new NuGet packages are required; parsing uses `System.IO` + `System.Text`. Derived fields (`DaysSincePreviousPayment`, `RemainingBalanceAfterPayment`, `CalculatedRealRate`) are computed from imported data using the existing `IFinancialCalculationService`, matching the existing `CreateAsync` pattern in `PaymentLedgerService`.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (backend) · TypeScript 5.9 (frontend)  
**Primary Dependencies**: ASP.NET Core Minimal APIs, EF Core 10 + SQLite, FluentValidation 12, React 19, Vite 7, React Router 7  
**Storage**: SQLite via EF Core (`DebtDashDbContext`) — no new tables required  
**Testing**: xUnit (unit + integration), Playwright (E2E), `WebApplicationFactory` for integration tests  
**Target Platform**: Local/self-hosted web application (single-user)  
**Project Type**: Web application (full-stack, backend + SPA in single project)  
**Performance Goals**: CSV validate + preview ≤ 5 s for 500 rows; template download ≤ 200 ms p95  
**Constraints**: Max 500 rows / 2 MB per upload; no new NuGet packages for CSV parsing  
**Scale/Scope**: Single-user app; maximum import batch of 500 payment entries

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality Gate**: New backend code follows existing patterns: Minimal API extension methods in `Api/`, contracts as `record` types in `ApiContracts.cs`, validators as `AbstractValidator<T>` in `Validators.cs`, service interface + implementation in `Domain/Services/`. Frontend follows existing ESLint + TypeScript-strict configuration. No dead code or placeholder stubs may merge.
- **Testing Gate**:
  - *Unit*: `CsvImportService` — all validation rules (missing headers, bad date, negative amount, missing loan, duplicate), edge cases (empty file, file too large, non-CSV). Must block merge if failing.
  - *Integration*: `POST /api/payments/import/validate` (valid + mixed CSV), `POST /api/payments/import/confirm` (batch persist + duplicate skipping + derived fields), `GET /api/payments/import/template` (correct headers returned). Must block merge if failing.
  - *E2E*: Upload happy path → preview → confirm → result summary visible. Template download leads to correct file.
- **UX Consistency Gate**: Reuse existing component patterns. Define all four states explicitly: *Empty* (upload control + template download link), *Loading* (spinner during validation), *Preview* (valid/invalid row table before confirm), *Result* (success count + skipped rows). Accessible: `<input type="file">` with visible label, error list with ARIA role, keyboard-navigable confirm/cancel actions.
- **Performance Gate**: Integration test asserts that a 500-row CSV validates in ≤ 5 s (CPU-bound only, no network delay). Template endpoint asserts ≤ 200 ms response in integration test. Both budgets must be verified before merge.
- **Observability & Simplicity Gate**: `IPaymentLedgerService.ImportAsync` logs `importedCount`, `skippedCount`, and any validation summary at `Information` level (matching existing `CreateAsync` log pattern). No server-side session or import-session DB table — the confirm endpoint accepts the validated rows directly from the client (stateless). This is simpler and avoids synchronization risk. The only alternative considered (server-side import session with a token) was rejected because it requires DB schema changes and a cleanup job.

## Project Structure

### Documentation (this feature)

```text
specs/001-csv-payment-import/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── api.yaml         # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code

```text
src/DebtDash.Web/
├── Api/
│   ├── Contracts/
│   │   └── ApiContracts.cs          # add CsvPaymentRow, ImportPreviewResponse,
│   │                                #     ImportConfirmRequest, ImportConfirmResponse
│   ├── Validators/
│   │   └── Validators.cs            # add ImportConfirmRequestValidator
│   └── PaymentEndpoints.cs          # add /import/template, /import/validate,
│                                    #     /import/confirm routes
└── Domain/
    └── Services/
        ├── PaymentLedgerService.cs  # add ImportAsync(rows) method
        └── CsvImportService.cs      # NEW: CSV parse, header/row validation,
                                     #      duplicate detection, template generation

src/DebtDash.Web/ClientApp/src/
├── components/
│   └── PaymentCsvImport/
│       ├── CsvImportDropzone.tsx    # file input + validation trigger
│       ├── ImportPreviewTable.tsx   # valid/invalid row preview before confirm
│       └── ImportResultSummary.tsx  # post-confirm success/skip count display
└── pages/
    └── Payments.tsx (existing)      # integrate import panel (collapsible section)

tests/DebtDash.Web.UnitTests/Domain/
└── CsvImportServiceTests.cs         # NEW: unit tests for all parsing/validation

tests/DebtDash.Web.IntegrationTests/Api/
└── PaymentImportEndpointsTests.cs   # NEW: integration tests for 3 import endpoints
```

**Structure Decision**: Extended the single-project layout (`src/DebtDash.Web`) matching the existing full-stack pattern. The import panel is integrated into the existing Payments page rather than a new route, keeping navigation flat. No new DB entities — import is stateless (validate → client-holds-rows → confirm). `CsvImportService` is a dedicated service to keep parsing logic isolated and unit-testable.

## Complexity Tracking

No constitution violations introduced. All design choices follow the simplest path available.
