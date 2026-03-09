# DebtDash — True Cost Loan Tracker

A web application for tracking loan payments, calculating real interest by actual elapsed days, detecting rate variance, and forecasting true payoff date and cost metrics.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (`dotnet --version` → 10.x)
- [Node.js 22+](https://nodejs.org/) and npm (if using nvm: `nvm use 22`)

## Getting Started

### 1. Install dependencies

```bash
# Backend (from repo root)
dotnet restore

# Frontend
cd src/DebtDash.Web/ClientApp
npm install
cd ../../..
```

### 2. Run the application

```bash
cd src/DebtDash.Web
dotnet run
```

This starts the ASP.NET Core backend on `http://localhost:5000` and the Vite dev server on `https://localhost:5173`. The backend proxies frontend requests to Vite during development.

Open your browser at the URL printed in the terminal.

> The SQLite database (`debtdash-dev.db`) is auto-created on first startup — no migration commands needed.

### 3. Build the frontend for production

```bash
cd src/DebtDash.Web/ClientApp
npm run build
```

## Running Tests

```bash
# All tests (unit + integration + e2e)
dotnet test

# Unit tests only
dotnet test tests/DebtDash.Web.UnitTests

# Integration tests only
dotnet test tests/DebtDash.Web.IntegrationTests

# Performance tests
dotnet test tests/DebtDash.Web.IntegrationTests --filter "Performance"

# Full lifecycle regression
dotnet test tests/DebtDash.Web.IntegrationTests --filter "Regression"
```

## Project Structure

```
src/DebtDash.Web/
├── Api/                        # Minimal API endpoints and DTOs
│   ├── Contracts/              # Request/response records
│   ├── Validators/             # FluentValidation rules
│   ├── LoanEndpoints.cs
│   ├── PaymentEndpoints.cs
│   ├── DashboardEndpoints.cs
│   └── ProjectionEndpoints.cs
├── Domain/
│   ├── Models/                 # EF Core entities
│   ├── Calculations/           # Stateless financial math
│   └── Services/               # Application services
├── Infrastructure/
│   └── Persistence/            # DbContext, configs, migrations
└── ClientApp/                  # React + TypeScript SPA (Vite)
    └── src/
        ├── pages/              # Route-level page components
        ├── components/         # Reusable UI components
        ├── charts/             # Recharts wrappers
        └── services/           # API client functions

tests/
├── DebtDash.Web.UnitTests/
├── DebtDash.Web.IntegrationTests/
└── DebtDash.Web.E2ETests/
```

## API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/loan` | Get loan profile |
| PUT | `/api/loan` | Create/update loan profile |
| GET | `/api/payments` | List payments (paginated) |
| POST | `/api/payments` | Add a payment |
| PUT | `/api/payments/{id}` | Update a payment |
| DELETE | `/api/payments/{id}` | Delete a payment |
| GET | `/api/dashboard` | Dashboard KPIs and chart data |
| GET | `/api/projections/true-end-date` | Payoff date projection |

## Tech Stack

- **Backend**: .NET 10, ASP.NET Core Minimal APIs, EF Core with SQLite
- **Frontend**: React 19, TypeScript, Vite, Recharts, React Router
- **Validation**: FluentValidation
- **Testing**: xUnit, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing
