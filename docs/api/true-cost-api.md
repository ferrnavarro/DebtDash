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
| GET | `/api/dashboard` | Get comparison dashboard relative to no-extra-principal baseline |
| GET | `/api/dashboard?window=trailing-6-months` | Same, scoped to a time window |

**Window parameter values:**

| Value | Description |
|-------|-------------|
| `full-history` | All payments start-to-latest (default) |
| `trailing-6-months` | Last 6 months from latest payment date |
| `trailing-12-months` | Last 12 months from latest payment date |
| `year-to-date` | Jan 1 of current year to latest payment date |

**Response fields:**

- `state` — `"ready"` / `"limitedData"` / `"empty"`
  - `ready`: ≥2 payments, full comparison available
  - `limitedData`: exactly 1 payment, summary only
  - `empty`: no payments, guidance message only
- `activeWindow` — `{ key, label, rangeStart, rangeEnd }`
- `availableWindows` — array of `{ key, label, rangeStart, rangeEnd }` (always 4 entries)
- `summary` — comparison summary object:
  - `windowKey` — active window key (camelCase)
  - `currentStatus` — `"ahead"` / `"onTrack"` / `"behind"` / `"insufficientData"`
  - `remainingBalanceDelta` — positive = actual balance lower than baseline (ahead)
  - `cumulativeInterestAvoided` — positive = interest saved vs baseline
  - `monthsSaved` — projected months removed from payoff term
  - `projectedPayoffDateDelta` — days ahead (-) or behind (+) projected payoff
  - `firstMeaningfulDivergenceDate` — ISO date of first statistically significant divergence (nullable)
  - `lastRecalculatedAt` — ISO datetime of last calculation
  - `explanatoryStateMessage` — human-readable explanation of current state
- `balanceSeries` — array of `{ date, actualRemainingBalance, baselineRemainingBalance, balanceDelta, interestDelta, containsExtraPrincipalEffect }`
- `costSeries` — same shape, tracking cumulative interest instead of balance
- `milestones` — array of `{ type, date, title, description, value? }`
  - `type` values: `"divergenceStart"`, `"highestBalanceGap"`, `"highestInterestSavings"`, `"earlyPayoff"`

**Baseline calculation**: The baseline amortizes the original principal at the loan's annual rate over the original term, using a standard annuity PMT formula. Interest is computed using 30-day periods (ACT/365 day-count). Extra principal payments diverge the actual balance from this baseline.

**Example response (ready state):**
```json
{
  "state": "ready",
  "activeWindow": { "key": "fullHistory", "label": "Full History", "rangeStart": "2024-01-15", "rangeEnd": "2024-07-15" },
  "availableWindows": [
    { "key": "fullHistory",      "label": "Full History",        "rangeStart": "2024-01-15", "rangeEnd": "2024-07-15" },
    { "key": "trailing6Months",  "label": "Trailing 6 Months",   "rangeStart": "2024-01-15", "rangeEnd": "2024-07-15" },
    { "key": "trailing12Months", "label": "Trailing 12 Months",  "rangeStart": "2024-01-15", "rangeEnd": "2024-07-15" },
    { "key": "yearToDate",       "label": "Year to Date",        "rangeStart": "2024-01-01", "rangeEnd": "2024-07-15" }
  ],
  "summary": {
    "windowKey": "fullHistory",
    "currentStatus": "ahead",
    "remainingBalanceDelta": 2450.25,
    "cumulativeInterestAvoided": 312.80,
    "monthsSaved": 4.2,
    "projectedPayoffDateDelta": -127.0,
    "firstMeaningfulDivergenceDate": "2024-02-15",
    "lastRecalculatedAt": "2025-01-01T00:00:00Z",
    "explanatoryStateMessage": "You are ahead of your baseline schedule."
  },
  "balanceSeries": [
    { "date": "2024-02-15", "actualRemainingBalance": 198450.0, "baselineRemainingBalance": 199345.0, "balanceDelta": 895.0, "interestDelta": 0.0, "containsExtraPrincipalEffect": true }
  ],
  "costSeries": [ ... ],
  "milestones": [
    { "type": "divergenceStart", "date": "2024-02-15", "title": "Balance Divergence Began", "description": "Your balance first fell below baseline.", "value": null }
  ]
}
```

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
