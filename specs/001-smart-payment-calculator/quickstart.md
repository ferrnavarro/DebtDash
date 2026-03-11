# Quickstart: Smart Monthly Payment Calculator with Live Rate Integration

## 1. Prerequisites

- .NET 10 SDK (`dotnet --version` → 10.x)
- Node.js 22+ and npm (`node --version` → 22.x)
- An existing DebtDash workspace with a configured loan profile and at least one payment log entry
  (to exercise fee-defaulting; the calculator works without prior payments but the fee will be empty).

## 2. Build & Run

```bash
# From repository root
dotnet build

# Install frontend dependencies (first time or after package.json changes)
cd src/DebtDash.Web/ClientApp
npm install
cd ../../..

# Run the application
cd src/DebtDash.Web
dotnet run
```

The backend serves on `http://localhost:5000`. The SPA dev server proxies on port 5173.

## 3. How the Interest Rate Is Resolved

No external rate service is required. When you click **Calculate**, the system reads the
`CalculatedRealRate` from the most recent entry in the payment ledger. This is the actual
rate computed from your real payment data (principal reduction, interest charged, elapsed days).

If the ledger is empty (no payments recorded yet), the system falls back to the baseline
`LoanProfile.AnnualRate` and the response will have `rateQuote.isFallback: true` with a
yellow warning in the UI.

## 4. Use the Calculator

1. Navigate to `http://localhost:5000` → click **Payment Calculator** in the navigation.
2. The **Monthly Fee** field is pre-populated from the most recent payment ledger entry.
3. Select a **Payoff Date** at least one full month from today.
4. Click **Calculate**. The system reads the current rate from the ledger and generates the schedule.
5. Review the month-by-month schedule and total-cost summary.
6. Modify the payoff date or fee and recalculate as needed.

## 5. New API Endpoints

### Get Default Fee
```
GET /api/calculator/default-fee
```
Returns the fee pre-population value from the most recent payment ledger entry.

```bash
curl http://localhost:5000/api/calculator/default-fee
# Response: { "defaultFeeAmount": 75.00, "sourcePaymentDate": "2026-02-15" }
# Or when ledger is empty: { "defaultFeeAmount": null, "sourcePaymentDate": null }
```

### Calculate Payment Schedule
```
POST /api/calculator/schedule
Content-Type: application/json
```

**Request body:**
```json
{
  "payoffDate": "2028-03-01",
  "feeAmount": 75.00
}
```
Pass `"feeAmount": null` to use the ledger default.

**Example with curl:**
```bash
curl -X POST http://localhost:5000/api/calculator/schedule \
  -H "Content-Type: application/json" \
  -d '{"payoffDate": "2028-03-01", "feeAmount": 75.00}'
```

**Validation errors (400)**:
- Payoff date in the past or < 1 full month ahead
- Negative `feeAmount`
- No loan profile configured (returns 404)
- Outstanding balance ≤ 0

See [contracts/api.yaml](./contracts/api.yaml) for the full OpenAPI schema.

## 6. Run Tests

```bash
# All tests
dotnet test

# Unit tests for this feature (amortization formula, fee defaulting, period derivation)
dotnet test tests/DebtDash.Web.UnitTests --filter "PaymentScheduleCalculator"

# Integration tests (schedule endpoint + fallback path)
dotnet test tests/DebtDash.Web.IntegrationTests --filter "CalculatorEndpoints"

# E2E tests (payoff date → schedule render user journey)
dotnet test tests/DebtDash.Web.E2ETests
```

## 7. Performance Validation

The following scenarios should complete within budget during integration testing:

| Scenario | Budget |
|----------|--------|
| `POST /api/calculator/schedule` (360 periods) | p95 < 500 ms |
| Frontend schedule table render (360 rows) | ≤ 200 ms paint time |

To manually test the 360-period scenario:
```bash
curl -X POST http://localhost:5000/api/calculator/schedule \
  -H "Content-Type: application/json" \
  -d '{"payoffDate": "2056-03-01", "feeAmount": 0}'
```

## 8. Existing API Endpoints (Unchanged)

- `GET/PUT /api/loan` — Loan profile
- `GET/POST/PUT/DELETE /api/payments` — Payment ledger
- `GET /api/dashboard` — Dashboard metrics
- `GET /api/projections/true-end-date` — Payoff projection

See [docs/api/true-cost-api.md](../../docs/api/true-cost-api.md) for full existing API documentation.
