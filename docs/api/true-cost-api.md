# True Cost Loan Tracker API

## Base URL

`/api`

## Endpoints

### Loan Profile

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/loan` | Get the current loan profile (404 if none configured) |
| PUT | `/api/loan` | Create or update the loan profile |

**PUT /api/loan Request Body:**
```json
{
  "initialPrincipal": 200000.00,
  "annualRate": 5.5,
  "termMonths": 360,
  "startDate": "2024-01-15",
  "fixedMonthlyCosts": 50.00,
  "currencyCode": "USD"
}
```

### Payments

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/payments?page=1&pageSize=50` | List payments (paginated, ordered by date) |
| POST | `/api/payments` | Create a payment entry |
| PUT | `/api/payments/{id}` | Update a payment entry |
| DELETE | `/api/payments/{id}` | Delete a payment entry |

**POST/PUT Request Body:**
```json
{
  "paymentDate": "2024-02-15",
  "totalPaid": 1500.00,
  "principalPaid": 1000.00,
  "interestPaid": 450.00,
  "feesPaid": 50.00,
  "manualRateOverrideEnabled": false,
  "manualRateOverride": null
}
```

**Validation**: `principalPaid + interestPaid + feesPaid` must equal `totalPaid` (±0.01 tolerance).

**Response includes calculated fields**: `daysSincePreviousPayment`, `remainingBalanceAfterPayment`, `calculatedRealRate`, and `rateVariance` (if applicable).

### Dashboard

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/dashboard` | Get aggregated dashboard KPIs and chart data |

**Response:**
- `totalInterestPaid`, `totalCapitalPaid` - Running totals
- `averageRealRateWeighted` - Balance-weighted average real annual rate
- `timeRemainingMonths` - Proportional estimate based on remaining balance
- `principalInterestTrendSeries` - Per-payment principal vs interest breakdown
- `debtCountdownSeries` - Per-payment remaining balance trend

### Projections

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/projections/true-end-date` | Get payoff date projection |

**Response:**
- `predictedEndDate` - Projected payoff date based on payment velocity
- `remainingMonthsEstimate` - Months to payoff at current velocity
- `principalVelocity` - Average monthly principal payment rate
- `baselineRemainingMonths` - Remaining months on original schedule
- `deltaMonthsVsBaseline` - Difference (negative = ahead of schedule)

## Error Responses

- **400**: Validation error or business rule violation
- **404**: Resource not found (no loan configured, payment not found)
- **500**: Internal server error (with generic message in production)
