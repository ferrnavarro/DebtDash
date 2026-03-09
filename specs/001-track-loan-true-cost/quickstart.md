# Quickstart: True Cost Loan Tracker

## 1. Prerequisites

- .NET 10 SDK installed (`dotnet --version` should show 10.x)
- Node.js 22+ and npm (via nvm: `nvm use 22`)

## 2. Build & Run

```bash
# From repository root
dotnet build

# Install frontend dependencies
cd src/DebtDash.Web/ClientApp
npm install
cd ../../..

# Run the application
cd src/DebtDash.Web
dotnet run
```

The backend serves on `http://localhost:5000` and proxies the Vite dev server on port 5173.

## 3. Run Tests

```bash
# All tests
dotnet test

# Unit tests only
dotnet test tests/DebtDash.Web.UnitTests

# Integration tests only
dotnet test tests/DebtDash.Web.IntegrationTests

# Performance tests
dotnet test tests/DebtDash.Web.IntegrationTests --filter "Performance"

# Regression test
dotnet test tests/DebtDash.Web.IntegrationTests --filter "Regression"
```

## 4. Frontend Build

```bash
cd src/DebtDash.Web/ClientApp
npm run build
```

## 5. Database

SQLite database is auto-created on first startup via `EnsureCreatedAsync()`. No migration commands needed for development.

- Development: `debtdash-dev.db`
- Production: `debtdash.db`

## 6. API Endpoints

- `GET/PUT /api/loan` - Loan profile
- `GET/POST/PUT/DELETE /api/payments` - Payment ledger
- `GET /api/dashboard` - Dashboard metrics
- `GET /api/projections/true-end-date` - Payoff projection

See [docs/api/true-cost-api.md](../../docs/api/true-cost-api.md) for full API documentation.
