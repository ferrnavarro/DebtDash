# Loan Tracker Architecture

## Overview

DebtDash is a single-page web application for tracking loan payment true costs. It uses a .NET 10 ASP.NET Core backend with a React TypeScript frontend (Vite).

## Tech Stack

- **Backend**: .NET 10, ASP.NET Core Minimal APIs, EF Core 10 with SQLite
- **Frontend**: React 18 + TypeScript, Vite, Recharts, React Router DOM
- **Testing**: xUnit, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing, Playwright
- **Validation**: FluentValidation 12

## Architecture Layers

```
src/DebtDash.Web/
├── Api/                     # Minimal API endpoints and DTOs
│   ├── Contracts/           # Request/response record types
│   ├── Validators/          # FluentValidation validators
│   ├── LoanEndpoints.cs
│   ├── PaymentEndpoints.cs
│   ├── DashboardEndpoints.cs
│   └── ProjectionEndpoints.cs
├── Domain/
│   ├── Models/              # EF Core entities
│   ├── Calculations/        # Pure financial math (stateless)
│   └── Services/            # Application services (stateful, use DbContext)
├── Infrastructure/
│   └── Persistence/         # DbContext, configurations, migrations
└── ClientApp/               # React SPA
    └── src/
        ├── pages/           # Route-level page components
        ├── components/      # Reusable UI components
        ├── charts/          # Recharts wrapper components
        └── services/        # API client functions
```

## Data Flow

1. **Loan Setup**: User configures baseline loan → `PUT /api/loan` → LoanProfileService → SQLite
2. **Payment Logging**: User adds payment → `POST /api/payments` → PaymentLedgerService → recalculates all entries (days elapsed, real rate, variance) → SQLite
3. **Dashboard**: `GET /api/dashboard` → DashboardAggregationService → builds weighted average rate, trend/countdown series from payment history
4. **Projections**: `GET /api/projections/true-end-date` → ProjectionService → calculates principal velocity, remaining months, delta vs baseline

## Key Design Decisions

- **Single loan profile**: MVP supports one loan per deployment
- **Full recalculation on mutation**: Every payment CRUD triggers recalculation of all entries to maintain consistency
- **Day-count interest**: Interest calculations use actual elapsed days (ACT/365)
- **Rate variance**: 5 basis point threshold compared against stated/override rate
- **SQLite**: Single-file database for simplicity; adequate for single-user workload
