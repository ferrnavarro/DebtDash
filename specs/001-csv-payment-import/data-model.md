# Data Model: CSV Payment Import

## Overview

The CSV import feature is **stateless** — no new database tables or entities are introduced. All import processing uses transient in-memory objects. Only confirmed valid rows produce persisted `PaymentLogEntry` records, which already exist in the domain model.

---

## Transient Objects (not persisted)

### `CsvPaymentRow`

Represents a single parsed and validated row from the uploaded CSV, passed between the validate and confirm endpoints.

| Field | Type | Required | Notes |
|---|---|---|---|
| `RowIndex` | `int` | Yes | 1-based row number in the file (excluding header) |
| `LoanId` | `Guid` | Yes | Must reference an existing `LoanProfile.Id` |
| `PaymentDate` | `DateOnly` | Yes | ISO 8601 format (`YYYY-MM-DD`) in CSV |
| `TotalPaid` | `decimal` | Yes | Must be > 0 |
| `PrincipalPaid` | `decimal` | Yes | Must be ≥ 0 |
| `InterestPaid` | `decimal` | Yes | Must be ≥ 0 |
| `FeesPaid` | `decimal` | Yes | Must be ≥ 0 |

**Constraints:**
- `PrincipalPaid + InterestPaid + FeesPaid` should equal `TotalPaid` (warn if mismatched; not a hard error to preserve flexibility).
- `ManualRateOverrideEnabled` defaults to `false`; `ManualRateOverride` defaults to `null` for all imported rows.

---

### `CsvRowError`

Represents a validation failure for one row.

| Field | Type | Notes |
|---|---|---|
| `RowIndex` | `int` | 1-based row number |
| `Errors` | `List<string>` | One or more human-readable error messages for this row |

---

### `ImportPreviewResponse`

Returned by `POST /api/payments/import/validate`.

| Field | Type | Notes |
|---|---|---|
| `TotalRows` | `int` | Total data rows read (excluding header) |
| `ValidCount` | `int` | Number of rows that passed all validation |
| `InvalidCount` | `int` | Number of rows that failed validation |
| `ValidRows` | `List<CsvPaymentRow>` | Rows ready for confirmation |
| `InvalidRows` | `List<CsvRowError>` | Rows with errors, for user review |

---

### `ImportConfirmRequest`

Sent by the client to `POST /api/payments/import/confirm`.

| Field | Type | Notes |
|---|---|---|
| `Rows` | `List<CsvPaymentRow>` | Valid rows from the preview (client echoes back) |

**Validation:**
- Must not be empty.
- Maximum 500 rows.

---

### `ImportConfirmResponse`

Returned by `POST /api/payments/import/confirm`.

| Field | Type | Notes |
|---|---|---|
| `ImportedCount` | `int` | Rows successfully persisted |
| `SkippedCount` | `int` | Rows skipped due to duplicate detection |
| `SkippedRows` | `List<SkippedRowDetail>` | Details on each skipped row |

---

### `SkippedRowDetail`

| Field | Type | Notes |
|---|---|---|
| `RowIndex` | `int` | 1-based index from original CSV |
| `Reason` | `string` | Human-readable skip reason (e.g. "Duplicate: payment with same date and amount already exists") |

---

## Existing Domain Entities (unchanged)

### `LoanProfile` (existing, read-only during import)

Used to validate that `LoanId` values in the CSV reference real loans.

| Key Fields | Notes |
|---|---|
| `Id` (Guid) | Looked up during row validation |
| `Payments` (navigation) | Used for duplicate detection during confirm |

---

### `PaymentLogEntry` (existing, created during confirm)

Each confirmed valid non-duplicate row produces one new `PaymentLogEntry`. Derived fields are computed after batch insert using the existing calculation chain.

| Key Fields | Notes |
|---|---|
| `LoanProfileId` | Set from `CsvPaymentRow.LoanId` |
| `PaymentDate` | Set from `CsvPaymentRow.PaymentDate` |
| `TotalPaid` / `PrincipalPaid` / `InterestPaid` / `FeesPaid` | Set from CSV row |
| `DaysSincePreviousPayment` | **Computed** after insert |
| `RemainingBalanceAfterPayment` | **Computed** after insert |
| `CalculatedRealRate` | **Computed** after insert |
| `ManualRateOverrideEnabled` | Defaults to `false` |
| `ManualRateOverride` | Defaults to `null` |

---

## CSV Format

### Required Columns (order-independent, case-insensitive header matching)

| Column | Maps To | Format |
|---|---|---|
| `LoanId` | `CsvPaymentRow.LoanId` | UUID string |
| `PaymentDate` | `CsvPaymentRow.PaymentDate` | `YYYY-MM-DD` |
| `TotalPaid` | `CsvPaymentRow.TotalPaid` | Decimal number |
| `PrincipalPaid` | `CsvPaymentRow.PrincipalPaid` | Decimal number |
| `InterestPaid` | `CsvPaymentRow.InterestPaid` | Decimal number |
| `FeesPaid` | `CsvPaymentRow.FeesPaid` | Decimal number |

### Template (example row)

```csv
LoanId,PaymentDate,TotalPaid,PrincipalPaid,InterestPaid,FeesPaid
00000000-0000-0000-0000-000000000000,2026-01-15,1500.00,1200.00,280.00,20.00
```

---

## Entity Relationship (unchanged)

```
LoanProfile (1) ──── (N) PaymentLogEntry
                              ↑
                         Created from each confirmed valid CsvPaymentRow
```
