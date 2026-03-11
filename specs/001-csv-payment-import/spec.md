# Feature Specification: CSV Payment Import

**Feature Branch**: `001-csv-payment-import`  
**Created**: March 9, 2026  
**Status**: Draft  
**Input**: User description: "feature to allow to populate payments via csv"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Bulk Upload Payments via CSV (Priority: P1)

A user managing one or more loans wants to record many past or recent payments at once rather than entering them individually. They navigate to the payments section, choose the "Import from CSV" option, select a CSV file from their device, and submit it. The system parses and validates the file, shows a preview of valid and invalid rows, and after the user confirms, all valid rows are persisted as payment log entries on the appropriate loans.

**Why this priority**: This is the core value of the feature — eliminating manual row-by-row entry of payments. Without this story there is no feature.

**Independent Test**: Integration + E2E tests covering file upload → validation → confirmation → persistence. A minimal test verifies that a well-formed CSV of 3 rows produces 3 new payment entries linked to the correct loans.

**Acceptance Scenarios**:

1. **Given** a user has at least one existing loan, **When** they upload a valid CSV file with correct headers and well-formed rows referencing that loan, **Then** the system displays a preview showing all rows as valid and the count of rows to be imported.
2. **Given** the user has reviewed the preview, **When** they click "Confirm Import", **Then** all valid rows are saved as payment log entries and the user sees an import result showing the total successfully imported count.
3. **Given** a CSV file in which some rows are valid and some contain errors, **When** the user uploads the file, **Then** only the valid rows are listed for import and invalid rows are listed separately with row-level error messages; no data is persisted until confirmation.

---

### User Story 2 - Download CSV Template (Priority: P2)

A user wants to prepare a CSV file for import but is unsure of the required column names and format. They download a blank template from the import page, fill it in their preferred spreadsheet tool, and upload it.

**Why this priority**: Without a downloadable template, users must guess column names and format, causing high error rates on first attempt. Providing the template dramatically reduces failed uploads.

**Independent Test**: Unit + E2E test verifying that the template download returns a file with the exact expected headers in the correct order and that uploading the template (with sample data added) passes header validation.

**Acceptance Scenarios**:

1. **Given** a user is on the CSV import page, **When** they click "Download Template", **Then** a CSV file is downloaded containing the correct column headers and one example row with placeholder values.
2. **Given** a user populates the downloaded template and uploads it, **When** the system validates it, **Then** no header validation errors are reported.

---

### User Story 3 - View and Dismiss Import Result Summary (Priority: P3)

After confirming an import, the user wants to see a clear summary: how many payments were imported successfully and how many rows were skipped due to errors. They can then navigate away or start another import.

**Why this priority**: Gives users confidence that the import completed correctly and provides actionable information about any failures.

**Independent Test**: Integration test confirming the result payload contains `importedCount`, `skippedCount`, and a list of `failedRows` with row number and reason.

**Acceptance Scenarios**:

1. **Given** a confirmed import with 10 valid rows and 2 invalid rows, **When** the import completes, **Then** the result summary displays "10 payments imported, 2 rows skipped" with details for each skipped row.
2. **Given** an import where all rows were valid, **When** the import completes, **Then** the result summary displays the total count imported and no error rows.

---

### Edge Cases

- What happens when the uploaded file is empty (no data rows after the header)?
- What happens when a CSV row references a loan ID that does not exist in the system?
- What happens when required columns (e.g., `LoanId`, `PaymentDate`, `TotalPaid`) are missing from the header row?
- How does the system handle a file that is not CSV format (e.g., `.xlsx`, `.pdf`)?
- What happens when a file exceeds the maximum allowed row count or file size?
- What happens when a payment date is in an unrecognized format?
- What happens when a numeric field (amount) contains non-numeric characters?
- How does the system handle duplicate payments (same LoanId + PaymentDate + TotalPaid) already present in the database? Duplicates are **skipped with a warning** — reported in the result summary but not treated as errors that block other rows.
- What happens when performance budgets are at risk (e.g., very large file approaching the row limit)?
- How do loading and error states remain consistent with existing UX patterns in the application?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST accept CSV file uploads containing one or more payment records targeting existing loans.
- **FR-002**: System MUST validate that the uploaded file has the required column headers (`LoanId`, `PaymentDate`, `TotalPaid`, `PrincipalPaid`, `InterestPaid`, `FeesPaid`) and reject any file missing required headers with a clear error message.
- **FR-003**: System MUST validate each data row for presence of required fields, correct data types (numeric amounts, valid date format), and positive non-zero values for payment amounts.
- **FR-004**: System MUST associate each valid row with the existing loan identified by `LoanId` and reject rows referencing loan IDs that do not exist.
- **FR-005**: System MUST present a preview before confirming the import, showing the count of valid rows, the count of invalid rows, and per-invalid-row details (row number and specific reason for rejection).
- **FR-006**: System MUST allow users to confirm or cancel the import after reviewing the preview.
- **FR-007**: System MUST persist only valid rows when the user confirms the import; invalid rows are never persisted.
- **FR-008**: System MUST display an import result summary after confirmation, reporting the count of successfully imported payments and the count of skipped rows with reasons.
- **FR-009**: System MUST provide a downloadable CSV template with the correct headers and one example row.
- **FR-010**: System MUST reject files exceeding 500 rows or 2 MB, displaying a clear limit-exceeded error.
- **FR-011**: System MUST reject files that are not valid CSV format (e.g., binary files, wrong MIME type).
- **FR-012**: System MUST calculate derived fields (`DaysSincePreviousPayment`, `RemainingBalanceAfterPayment`, `CalculatedRealRate`) from imported data; these fields must NOT be required in the CSV.
- **FR-013**: System MUST skip rows where a payment with the same `LoanId`, `PaymentDate`, and `TotalPaid` already exists in the database; these rows MUST appear in the import result summary as skipped duplicates (not as errors).

### Quality & Maintainability Requirements

- **QR-001**: Changes MUST pass formatting, linting, and static analysis checks in CI.
- **QR-002**: Non-trivial implementation decisions MUST be documented in the feature plan or PR notes.

### Testing Requirements

- **TR-001**: New behavior MUST include unit tests for core logic and edge cases.
- **TR-002**: Boundary interactions MUST include integration tests.
- **TR-003**: Critical user journeys or contracts MUST include end-to-end or contract tests.
- **TR-004**: Failing tests MUST block merge and release.

### User Experience Consistency Requirements

- **UXR-001**: User-facing features MUST reuse existing components/tokens unless an approved exception is documented.
- **UXR-002**: Loading, empty, error, and success states MUST be explicitly defined:
  - *Loading*: File is being parsed and validated.
  - *Empty*: No file selected yet; prompt with upload control and template download link.
  - *Error*: Invalid file, header mismatch, or row-level errors displayed before confirmation.
  - *Success*: Import result summary after confirmation.
- **UXR-003**: Accessibility expectations (keyboard navigation, screen-reader labels for file input and error lists, sufficient contrast) MUST be documented and validated.

### Performance Requirements

- **PRF-001**: CSV validation and preview rendering MUST complete within 5 seconds for files up to 500 rows on standard hardware.
- **PRF-002**: A validation method (automated integration test or manual benchmark) MUST verify the 5-second budget before release.
- **PRF-003**: Budget regressions MUST be treated as release blockers unless an approved, time-bound exception exists.

### Key Entities

- **Payment Import Session**: Represents a single batch upload operation — file name, upload timestamp, total row count, valid row count, invalid row count, status (pending review / confirmed / cancelled).
- **Payment Import Row**: An individual row within an import session — row number, raw CSV values, validation status (valid / invalid), error messages if invalid, mapped payment fields if valid.
- **Payment Log Entry**: The persisted payment record created for each valid confirmed row — linked to a specific loan, contains payment date, total paid, principal paid, interest paid, fees paid, and system-calculated derived fields.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can upload and import up to 500 payment records from a single CSV file without errors.
- **SC-002**: CSV validation and preview display complete within 5 seconds for files up to 500 rows.
- **SC-003**: Every invalid row is identified before confirmation with a specific, human-readable reason, enabling users to correct and re-upload without guessing.
- **SC-004**: All valid rows from a confirmed import are persisted without data loss (zero silent failures).
- **SC-005**: Users can complete the full import workflow — upload, review, confirm, view result — in under 3 minutes for a 100-row file.

## Assumptions

- The CSV uses a fixed predefined column set (described in FR-002). Column mapping by users is out of scope for this feature.
- Partial imports are supported: valid rows are imported even when some rows fail validation, unless duplicate handling policy (FR-013) requires otherwise.
- The `ManualRateOverride` and `ManualRateOverrideEnabled` fields are not required in the CSV; they default to `false`/`null` for all imported rows.
- File size and row limits (500 rows / 2 MB) are appropriate starting defaults; these may be revisited based on observed usage.
- The date format accepted in the CSV is ISO 8601 (`YYYY-MM-DD`) as a reasonable universal default.
