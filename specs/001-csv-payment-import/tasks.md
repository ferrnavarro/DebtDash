# Tasks: CSV Payment Import

**Input**: Design documents from `/specs/001-csv-payment-import/`
**Prerequisites**: plan.md ✅ · spec.md ✅ · research.md ✅ · data-model.md ✅ · contracts/api.yaml ✅ · quickstart.md ✅

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- All paths are relative to repo root

---

## Phase 1: Setup

**Purpose**: Wire the new service into the existing DI container — required before any story can run end-to-end.

- [X] T001 Register `ICsvImportService` / `CsvImportService` in `src/DebtDash.Web/Program.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: API contracts and the service skeleton that every user story and every test depend on. No story work can begin until this phase is complete.

**⚠️ CRITICAL**: These tasks block ALL user stories.

- [X] T002 Add import contract record types (`CsvPaymentRow`, `CsvRowError`, `ImportPreviewResponse`, `ImportConfirmRequest`, `ImportConfirmResponse`, `SkippedRowDetail`) to `src/DebtDash.Web/Api/Contracts/ApiContracts.cs`
- [X] T003 Create `ICsvImportService` interface and `CsvImportService` skeleton (stub methods, `RequiredHeaders` constant) in `src/DebtDash.Web/Domain/Services/CsvImportService.cs`

**Checkpoint**: Contract types compile; service is registered; stub methods exist — story work can now begin independently.

---

## Phase 3: User Story 1 — Bulk Upload Payments via CSV (Priority: P1) 🎯 MVP

**Goal**: Users can upload a CSV file, see a validation preview of valid/invalid rows, confirm, and have valid payments persisted with all derived fields correctly calculated.

**Independent Test**: POST a well-formed 3-row CSV to `POST /api/payments/import/validate` → 200 + `validCount: 3`; then POST those rows to `POST /api/payments/import/confirm` → 200 + `importedCount: 3`; then query the payments list and see 3 new entries.

### Tests for User Story 1 ⚠️

> Write these tests first; verify they FAIL before implementing the production code.

- [X] T004 [P] [US1] Write unit tests for `CsvImportService.ParseAndValidate` covering: valid CSV, missing headers, bad date format, non-numeric amount, `TotalPaid = 0`, unknown `LoanId`, empty file, 501-row file, non-CSV MIME type in `tests/DebtDash.Web.UnitTests/Domain/CsvImportServiceTests.cs`
- [X] T005 [P] [US1] Write integration tests for `POST /api/payments/import/validate` (happy path, mixed valid/invalid rows) and `POST /api/payments/import/confirm` (batch persist, duplicate skipping, derived fields in DB) in `tests/DebtDash.Web.IntegrationTests/Api/PaymentImportEndpointsTests.cs`

### Implementation for User Story 1

- [X] T006 [P] [US1] Implement `CsvImportService.ParseAndValidate(IFormFile)`: file-level guards (MIME, size ≤ 2 MB, rows ≤ 500), header detection (case-insensitive), per-row parsing and validation (required fields, `DateOnly` parse, positive decimals, `LoanId` existence check), return `ImportPreviewResponse` in `src/DebtDash.Web/Domain/Services/CsvImportService.cs`
- [X] T007 [P] [US1] Add `ImportAsync(List<CsvPaymentRow>)` to `IPaymentLedgerService` and implement in `PaymentLedgerService`: duplicate detection query (`LoanId + PaymentDate + TotalPaid`), batch insert of non-duplicate rows as `PaymentLogEntry`, call `RecalculateFromEntry` per entry in payment-date order, return `ImportConfirmResponse` in `src/DebtDash.Web/Domain/Services/PaymentLedgerService.cs`
- [X] T008 [US1] Add `ImportConfirmRequestValidator` (non-empty rows, max 500) to `src/DebtDash.Web/Api/Validators/Validators.cs`
- [X] T009 [US1] Add `POST /api/payments/import/validate` route (accept `IFormFile`, call `CsvImportService.ParseAndValidate`, return 400 on file-level error, 200 with `ImportPreviewResponse`) to `src/DebtDash.Web/Api/PaymentEndpoints.cs`
- [X] T010 [US1] Add `POST /api/payments/import/confirm` route (accept `ImportConfirmRequest`, validate with `ImportConfirmRequestValidator`, call `IPaymentLedgerService.ImportAsync`, return `ImportConfirmResponse`) to `src/DebtDash.Web/Api/PaymentEndpoints.cs`
- [X] T011 [P] [US1] Create `CsvImportDropzone.tsx`: file `<input accept=".csv">` with `aria-label`, calls `POST /api/payments/import/validate` on file selection, manages `idle → validating` state transition, passes `ImportPreviewResponse` up via callback in `src/DebtDash.Web/ClientApp/src/components/PaymentCsvImport/CsvImportDropzone.tsx`
- [X] T012 [P] [US1] Create `ImportPreviewTable.tsx`: displays valid-row count, renders invalid rows with row number and error messages in an accessible list (`role="list"`), Confirm and Cancel buttons (keyboard-focusable), calls `POST /api/payments/import/confirm` on Confirm, manages `preview → confirming` state transition in `src/DebtDash.Web/ClientApp/src/components/PaymentCsvImport/ImportPreviewTable.tsx`

**Checkpoint**: User Story 1 is fully functional end-to-end. A 3-row CSV can be uploaded, previewed, confirmed, and verified in the payments list.

---

## Phase 4: User Story 2 — Download CSV Template (Priority: P2)

**Goal**: Users can download a blank CSV template with the correct headers and one example row so they can prepare a valid import file without guessing column names.

**Independent Test**: `GET /api/payments/import/template` returns 200 with `Content-Type: text/csv`, the first line exactly matches `LoanId,PaymentDate,TotalPaid,PrincipalPaid,InterestPaid,FeesPaid`, and uploading the returned template file (after adding a data row) passes header validation in `POST /api/payments/import/validate`.

### Tests for User Story 2 ⚠️

- [X] T013 [P] [US2] Write unit test asserting `CsvImportService.GenerateTemplate()` produces a string whose first line exactly matches the `RequiredHeaders` constant (ensures template/validator never drift) in `tests/DebtDash.Web.UnitTests/Domain/CsvImportServiceTests.cs`
- [X] T014 [P] [US2] Write integration test for `GET /api/payments/import/template`: asserts 200, `Content-Type: text/csv`, `Content-Disposition: attachment; filename="payment-import-template.csv"`, and correct headers row in `tests/DebtDash.Web.IntegrationTests/Api/PaymentImportEndpointsTests.cs`

### Implementation for User Story 2

- [X] T015 [P] [US2] Implement `CsvImportService.GenerateTemplate()`: build header row from `RequiredHeaders` constant + one example row with safe placeholder values, return as `string` in `src/DebtDash.Web/Domain/Services/CsvImportService.cs`
- [X] T016 [US2] Add `GET /api/payments/import/template` route: call `CsvImportService.GenerateTemplate()`, return `text/csv` response with `Content-Disposition: attachment; filename="payment-import-template.csv"` in `src/DebtDash.Web/Api/PaymentEndpoints.cs`
- [X] T017 [US2] Add "Download Template" link/button to `CsvImportDropzone.tsx` that triggers a browser download of `/api/payments/import/template` when the panel is in `idle` state in `src/DebtDash.Web/ClientApp/src/components/PaymentCsvImport/CsvImportDropzone.tsx`

**Checkpoint**: Clicking "Download Template" downloads a valid CSV. Filling it in and uploading it passes validation.

---

## Phase 5: User Story 3 — View and Dismiss Import Result Summary (Priority: P3)

**Goal**: After confirming an import, users see a clear summary of how many payments were imported and how many rows were skipped (with skip reasons), and can start a new import or navigate away.

**Independent Test**: After `POST /api/payments/import/confirm` with 3 valid rows + 1 duplicate row, the UI displays "3 payments imported, 1 row skipped" with the skip reason visible; clicking "Import another file" resets to `idle` state.

### Tests for User Story 3 ⚠️

- [X] T018 [P] [US3] Write integration test confirming that `ImportConfirmResponse` with a duplicate present returns `importedCount: N`, `skippedCount: 1`, and `skippedRows[0].reason` is non-empty in `tests/DebtDash.Web.IntegrationTests/Api/PaymentImportEndpointsTests.cs`

### Implementation for User Story 3

- [X] T019 [P] [US3] Create `ImportResultSummary.tsx`: displays `importedCount` and `skippedCount`, renders `skippedRows` list with row index and reason (accessible: `role="list"`, screen-reader-friendly counts), "Import another file" Reset button that calls `onReset` callback in `src/DebtDash.Web/ClientApp/src/components/PaymentCsvImport/ImportResultSummary.tsx`
- [X] T020 [US3] Integrate full import state machine (`idle → validating → preview → confirming → result → idle`) and compose `CsvImportDropzone`, `ImportPreviewTable`, and `ImportResultSummary` into a collapsible "Import from CSV" section in `src/DebtDash.Web/ClientApp/src/pages/Payments.tsx`; trigger payments list refresh after confirmed import

**Checkpoint**: All three user stories are independently functional and integrated on the Payments page.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Performance validation, accessibility audit, and observability checks that span all three stories.

- [X] T021 [P] Write performance integration test: upload a 500-row fixture CSV to `POST /api/payments/import/validate`, assert HTTP 200 and elapsed time ≤ 5 000 ms using `Stopwatch` in `tests/DebtDash.Web.IntegrationTests/Api/PaymentImportEndpointsTests.cs`
- [X] T022 [P] Audit accessibility on all three import components (`CsvImportDropzone`, `ImportPreviewTable`, `ImportResultSummary`): verify `<input type="file">` has a visible `<label>`, error and skip-row lists have `role="list"`, Confirm/Cancel/Reset buttons are keyboard-reachable and have descriptive `aria-label` or button text in `src/DebtDash.Web/ClientApp/src/components/PaymentCsvImport/`
- [X] T023 Verify `PaymentLedgerService.ImportAsync` logs `importedCount`, `skippedCount` at `Information` level (matching the existing `CreateAsync` log pattern) in `src/DebtDash.Web/Domain/Services/PaymentLedgerService.cs`
- [X] T024 [P] Run `dotnet build` and confirm zero warnings/errors; run frontend `eslint` and confirm zero new lint errors for all changed files in `src/DebtDash.Web/` and `src/DebtDash.Web/ClientApp/src/`

---

## Dependencies & Execution Order

### Phase Dependencies

| Phase | Depends On | Can Parallelize With |
|---|---|---|
| Phase 1: Setup | — | — |
| Phase 2: Foundational | Phase 1 | — |
| Phase 3: US1 (P1) | Phase 2 | — |
| Phase 4: US2 (P2) | Phase 2 (T003 for `GenerateTemplate`) | Phase 3 (different files) |
| Phase 5: US3 (P3) | Phase 3 complete (needs US1 backend + `ImportPreviewTable`) | — |
| Phase 6: Polish | Phase 3, 4, 5 | All polish tasks [P] with each other |

### User Story Dependencies

- **US1 (P1)**: Depends only on Phase 2. No dependency on US2 or US3.
- **US2 (P2)**: Depends only on Phase 2 (specifically T003 for `GenerateTemplate` stub). Fully independent of US1 backend; shares `CsvImportDropzone.tsx` file with US1 frontend (T017 is an additive change to T011's file).
- **US3 (P3)**: Depends on US1 being complete (needs `ImportPreviewTable` and `ImportConfirmResponse`). The `ImportResultSummary.tsx` component (T019) can be built in parallel with US1 integration work; T020 (Payments.tsx wiring) depends on T019, T012, and T011.

### Within Each User Story

- Tests (T004/T005, T013/T014, T018) should be written before implementing production code
- Service implementation (T006, T007, T015) before endpoint wiring (T009, T010, T016)
- Frontend components (T011, T012, T019) before Payments.tsx integration (T020)

---

## Parallel Opportunities

### Phase 3 (US1) — can run together after T006 + T007 are done

```
Parallel group A (no shared files, after T003):
  T004 — CsvImportServiceTests.cs (unit)
  T005 — PaymentImportEndpointsTests.cs (integration)
  T011 — CsvImportDropzone.tsx (frontend)
  T012 — ImportPreviewTable.tsx (frontend)

Parallel group B (after T003):
  T006 — CsvImportService.ParseAndValidate (backend)
  T007 — PaymentLedgerService.ImportAsync (backend)
```

### Phase 4 (US2) — can run in parallel with Phase 3 after Phase 2

```
Parallel with Phase 3:
  T013 — GenerateTemplate unit test (different file from T004)
  T014 — Template integration test (additive to T005's file)
  T015 — CsvImportService.GenerateTemplate (additive to T006's file)
```

### Phase 6 (Polish) — all [P] tasks run together

```
T021 — performance test
T022 — accessibility audit
T023 — logging verification
T024 — build/lint check
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001)
2. Complete Phase 2: Foundational (T002, T003)
3. Complete Phase 3: User Story 1 (T004–T012)
4. **STOP and VALIDATE**: Upload a real CSV, preview it, confirm, verify entries in the payment list
5. Ship this increment — users can already bulk-import payments

### Incremental Delivery

| Increment | Stories Done | User Value |
|---|---|---|
| MVP | US1 | Bulk upload + validate + confirm |
| +US2 | US1 + US2 | Template download reduces first-try errors |
| Full | US1 + US2 + US3 | Rich result summary with duplicate visibility |

### Task Count Summary

| Phase | Tasks | Story |
|---|---|---|
| Setup | 1 | — |
| Foundational | 2 | — |
| US1 implementation | 7 | P1 |
| US1 tests | 2 | P1 |
| US2 implementation | 3 | P2 |
| US2 tests | 2 | P2 |
| US3 implementation | 2 | P3 |
| US3 tests | 1 | P3 |
| Polish | 4 | — |
| **Total** | **24** | |
