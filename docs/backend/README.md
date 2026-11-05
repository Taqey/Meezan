# Backend Documentation (Meezan .NET 10 WebApi)

The Meezan backend is a high-performance, modular monolith built on **.NET 10** and **Entity Framework Core 10**, following **Clean Architecture** and **CQRS with MediatR**. It serves real-time market data, technical levels, quantitative Shariah compliance opinions, Graham fair valuations, and investment funds analysis for Egyptian Stock Exchange (EGX) securities.

---

## Architecture Overview

```
backend/
├── src/
│   ├── Meezan.Domain/         # Core enterprise entities, value objects & enums (Zero dependencies)
│   │   ├── Entities/          # Domain models (Funds, Stocks, Indices, Market, Shariah, System)
│   │   ├── Enums/             # ShariahStatus, PriceComparison, ValuationConfidence, etc.
│   │   └── Services/          # Domain evaluation heuristics (Pure domain logic)
│   │
│   ├── Meezan.Application/    # Use cases, CQRS commands/queries, business workflows & contracts
│   │   ├── Common/            # Cross-cutting interfaces, MediatR pipeline behaviors, results
│   │   │   ├── Interfaces/    # Repositories, integrations, storage contracts
│   │   │   ├── Models/        # DTO projections, external API schemas
│   │   │   └── Services/      # Business domain engines (Shariah, Market, FairValue)
│   │   └── Features/          # Vertical sliced CQRS commands and queries
│   │       ├── Funds/         # Categories, classification, details, listing, sync, types
│   │       ├── Indices/       # Index files upload, constituent management
│   │       ├── Market/        # Snapshot refresh, quotes
│   │       ├── Scraping/      # Web scraping handlers, valuation pipeline
│   │       ├── Sectors/       # Sector queries, daily performance recalculations
│   │       ├── Shariah/       # Opinion imports, verdict recomputation, map seeding
│   │       ├── Stocks/        # Stock listing, detail projections, market data
│   │       └── System/        # Job settings, background worker management
│   │
│   ├── Meezan.Infrastructure/ # Data access, EF Core, database migrations & external integrations
│   │   ├── Persistence/       # ApplicationDbContext, entity type configurations, repositories
│   │   │   ├── Configurations/# Fluent API mappings for all entities
│   │   │   └── Repositories/  # Domain repository implementations (Funds, Stocks, Indices, Shariah)
│   │   └── Services/          # Concrete external integrations (Mubasher scraper, Snduk client, LocalStorage)
│   │
│   └── Meezan.WebApi/         # REST API endpoints, ASP.NET Core controllers, middleware & hosting
│       ├── Controllers/       # Public (read-only) & Admin (JWT-protected) controllers
│       ├── Middleware/        # Exception handling, request timing, security headers
│       └── Models/Admin/      # Admin API request/response contracts
│
└── tests/
    └── Meezan.Infrastructure.Tests/ # Architecture boundary verification, unit & integration tests
```

---

## Architectural Principles

1. **Explicit Boundaries**:
   - `Meezan.Domain` has **zero dependencies** on other projects, EF Core, or external libraries.
   - `Meezan.Application` depends only on `Domain` and core abstractions (`MediatR`, `FluentValidation`). It has **zero references** to `Infrastructure`, `WebApi`, or `Microsoft.EntityFrameworkCore`.
   - `Meezan.Infrastructure` implements Application interfaces.
   - `Meezan.WebApi` serves as the entry point and composition root.
2. **CQRS & MediatR**:
   - All write operations use MediatR Commands returning `Result` or `Result<T>`.
   - All read operations use MediatR Queries returning structured DTOs.
3. **Single Responsibility**:
   - Every entity, enum, DTO, and repository interface lives in its own dedicated source file.
4. **Safety & Zero Disruption**:
   - Database schemas, table columns, migrations, and existing public HTTP routes remain strictly preserved.

---

## Guides & Deep Dives

1. [Architecture & CQRS](./architecture.md)
2. [Funds System & Sync Pipeline](./funds.md)
3. [Stocks, Scraping & Valuation Models](./stocks-and-market.md)
4. [Shariah Screening & Opinions Engine](./shariah.md)
5. [Admin API & Operations](./admin-api.md)
