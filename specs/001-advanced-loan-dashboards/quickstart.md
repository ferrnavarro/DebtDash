# Quickstart: Advanced Loan Comparison Dashboards

## 1. Prerequisites

- .NET 10 SDK installed
- Node.js 22+ and npm installed
- Frontend dependencies installed in src/DebtDash.Web/ClientApp

## 2. Build the Solution

```bash
dotnet build
cd src/DebtDash.Web/ClientApp
npm install
npm run build
cd ../../..
```

## 3. Run the Application

```bash
cd src/DebtDash.Web
dotnet run
```

Open the application at the ASP.NET Core local URL and navigate to the dashboard.

## 4. Seed or Create Testable Comparison Data

1. Create a loan profile with enough remaining term to observe payoff acceleration.
2. Add a baseline sequence of scheduled payments with no extra principal.
3. Add or edit several later payments to include extra principal reductions.
4. Confirm the dashboard can show both overlap periods and divergence periods.

## 5. Validate Primary Behavior

1. Open the dashboard and verify actual versus baseline summary cards are visible.
2. Switch between full-history and recent windows using the window selector.
3. Confirm remaining-balance and cumulative-cost charts stay synchronized.
4. Edit a historical payment and confirm the summary, milestones, and graphs refresh.
5. Verify limited-data messaging when a loan has too little history or no divergence.

**API smoke test** (requires app running on localhost:5195):

```bash
# Default window (full-history)
curl -s http://localhost:5195/api/dashboard | jq '.state, .summary.currentStatus'

# Trailing 6-months window
curl -s "http://localhost:5195/api/dashboard?window=trailing-6-months" | jq '.activeWindow.key, .summary.monthsSaved'

# Year-to-date window
curl -s "http://localhost:5195/api/dashboard?window=year-to-date" | jq '.state, .milestones | length'
```

## 6. Run Verification

```bash
dotnet test tests/DebtDash.Web.UnitTests
dotnet test tests/DebtDash.Web.IntegrationTests --filter "Dashboard|Projection|Regression"
dotnet test tests/DebtDash.Web.E2ETests
cd src/DebtDash.Web/ClientApp
npm run lint
npm run build
cd ../../..
```

## 7. Performance Validation

1. Run the dashboard integration or performance scenarios with histories up to 5,000
   payments.
2. Confirm dashboard API comparison responses stay under the defined p95 budget.
3. Confirm UI refresh after payment edits remains under the 3 second target for
   routine user actions.

## 8. Expected Delivery Scope

- Enriched dashboard API response with comparison summary, milestones, and time-window
  series.
- Dashboard UI updates that compare actual and no-extra-principal paths.
- Test coverage across no-extra, steady-extra, retroactive-edit, and early-payoff
  scenarios.