# Quickstart: CSV Payment Import

## What This Feature Does

Adds bulk payment import to DebtDash. Users upload a CSV file, review a validation preview, confirm, and see the result — all without leaving the Payments page. A downloadable template removes guesswork about column names.

---

## Backend

### 1. Add import contracts to `ApiContracts.cs`

Add these record types to `src/DebtDash.Web/Api/Contracts/ApiContracts.cs`:

```csharp
// ──────────────────────────────────────────────────────────────────────────────
// CSV Payment Import Contracts (Feature 001-csv-payment-import)
// ──────────────────────────────────────────────────────────────────────────────

public record CsvPaymentRow(
    int RowIndex,
    Guid LoanId,
    DateOnly PaymentDate,
    decimal TotalPaid,
    decimal PrincipalPaid,
    decimal InterestPaid,
    decimal FeesPaid);

public record CsvRowError(int RowIndex, List<string> Errors);

public record ImportPreviewResponse(
    int TotalRows,
    int ValidCount,
    int InvalidCount,
    List<CsvPaymentRow> ValidRows,
    List<CsvRowError> InvalidRows);

public record ImportConfirmRequest(List<CsvPaymentRow> Rows);

public record SkippedRowDetail(int RowIndex, string Reason);

public record ImportConfirmResponse(
    int ImportedCount,
    int SkippedCount,
    List<SkippedRowDetail> SkippedRows);
```

---

### 2. Create `CsvImportService`

New file: `src/DebtDash.Web/Domain/Services/CsvImportService.cs`

Responsibilities:
- Parse raw CSV bytes into `CsvPaymentRow` objects (header detection, per-row field extraction).
- Validate each row (required fields, types, positive amounts, valid LoanId).
- Detect duplicates on confirm (query existing payments for matching `LoanId + PaymentDate + TotalPaid`).
- Generate the template CSV string from column-name constants.

Key constants (shared between template generator and parser/validator):

```csharp
private static readonly string[] RequiredHeaders =
    ["LoanId", "PaymentDate", "TotalPaid", "PrincipalPaid", "InterestPaid", "FeesPaid"];
```

---

### 3. Add import routes to `PaymentEndpoints.cs`

Add three routes inside `MapPaymentEndpoints`:

```
GET  /api/payments/import/template  → CsvImportService.GenerateTemplate()
POST /api/payments/import/validate  → CsvImportService.ParseAndValidate(file)
POST /api/payments/import/confirm   → PaymentLedgerService.ImportAsync(rows)
```

`ImportAsync` on `IPaymentLedgerService`:
- Accepts `List<CsvPaymentRow>` and `DebtDashDbContext`.
- Checks for duplicates.
- Saves non-duplicate rows as `PaymentLogEntry` records.
- Calls `RecalculateFromEntry` (existing pattern) for each saved entry.
- Returns `ImportConfirmResponse`.

---

### 4. Add `ImportConfirmRequestValidator` to `Validators.cs`

```csharp
public class ImportConfirmRequestValidator : AbstractValidator<ImportConfirmRequest>
{
    public ImportConfirmRequestValidator()
    {
        RuleFor(x => x.Rows).NotEmpty().WithMessage("Import rows must not be empty.");
        RuleFor(x => x.Rows.Count).LessThanOrEqualTo(500).WithMessage("Cannot import more than 500 rows at once.");
    }
}
```

---

## Frontend

### 5. Create `PaymentCsvImport/` components

Three focused components under `src/DebtDash.Web/ClientApp/src/components/PaymentCsvImport/`:

| Component | Purpose |
|---|---|
| `CsvImportDropzone.tsx` | File `<input>`, calls `POST /api/payments/import/validate`, shows loading state |
| `ImportPreviewTable.tsx` | Renders valid-row count, invalid rows with per-row errors; Confirm/Cancel buttons |
| `ImportResultSummary.tsx` | Shows imported count, skipped rows after confirm; Reset button |

State machine (local to the parent panel):
- `idle` → `validating` → `preview` → `confirming` → `result`
- Back to `idle` on Cancel or Reset.

---

### 6. Integrate into `Payments.tsx`

Add a collapsible "Import from CSV" section above the payments table. The section contains the `CsvImportDropzone` + state machine. After a successful import the payments list refreshes automatically.

---

## Testing

### Unit Tests (`tests/DebtDash.Web.UnitTests/Domain/CsvImportServiceTests.cs`)

Cover:
- Valid 3-row CSV → 3 valid rows, 0 errors.
- Missing required header → file-level error.
- Row with non-numeric `TotalPaid` → row-level error.
- Row with `PaymentDate` not in `YYYY-MM-DD` format → row-level error.
- Row with `TotalPaid = 0` → row-level error.
- Row with unknown `LoanId` → row-level error.
- File with 501 rows → file-level limit error.
- Duplicate detection: same LoanId + PaymentDate + TotalPaid → `SkippedRowDetail` in result.

### Integration Tests (`tests/DebtDash.Web.IntegrationTests/Api/PaymentImportEndpointsTests.cs`)

Cover:
- `GET /api/payments/import/template` → 200, `Content-Type: text/csv`, correct headers row.
- `POST /api/payments/import/validate` with valid 3-row CSV → 200, `validCount: 3`, `invalidCount: 0`.
- `POST /api/payments/import/validate` with mixed CSV → correct split.
- `POST /api/payments/import/confirm` with valid rows → 200, `importedCount` matches, entries in DB.
- `POST /api/payments/import/confirm` with duplicate rows → `skippedCount > 0`, no duplicate entries in DB.
- **Performance**: 500-row CSV validates in ≤ 5 000 ms (asserted with `Stopwatch`).

---

## API Reference

See [contracts/api.yaml](contracts/api.yaml) for full OpenAPI 3.0 contract.

### Quick Reference

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/payments/import/template` | Download CSV template |
| `POST` | `/api/payments/import/validate` | Validate CSV, returns preview |
| `POST` | `/api/payments/import/confirm` | Persist confirmed rows |

### CSV Column Format

| Column | Format | Required |
|---|---|---|
| `LoanId` | UUID (`xxxxxxxx-xxxx-...`) | Yes |
| `PaymentDate` | `YYYY-MM-DD` | Yes |
| `TotalPaid` | Decimal (`1500.00`) | Yes |
| `PrincipalPaid` | Decimal (`1200.00`) | Yes |
| `InterestPaid` | Decimal (`280.00`) | Yes |
| `FeesPaid` | Decimal (`20.00`) | Yes |

Derived fields (`DaysSincePreviousPayment`, `RemainingBalanceAfterPayment`, `CalculatedRealRate`) are **not** accepted in the CSV — they are computed server-side after import.
