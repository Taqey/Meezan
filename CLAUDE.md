# CLAUDE.md - AI Coding Assistant Guidelines for Meezan

This repository contains the **Meezan** application: an Egyptian Stock Exchange (EGX) intelligence platform offering multi-standard Shariah screening, Graham valuation models, technical support/resistance ladders, and mutual fund analytics.

---

## Architecture Stack

- **Frontend**: Angular 19 (Standalone Components, SCSS/CSS, RxJS, TypeScript). Root: `frontend/`
- **Backend**: .NET 10 WebApi (C# 13 / .NET 10, Clean Architecture, CQRS with MediatR, EF Core 10). Root: `backend/`
- **Database**: SQL Server (Shared / Remote).

---

## Critical Safety Invariants

> [!CAUTION]
> **NEVER touch the database directly, run destructive migrations, alter existing schema tables/columns, or change public API route paths and DTO field names.**
> Local environments point to a real shared database.
> The mandate across this repository is **ZERO behavior change, ZERO visual change, and ZERO regression**.

---

## Git Workflow (Dual-Repo Synchronization)

The repository root and `frontend/` have synchronized git contexts. Always commit changes using the dual-commit script:
```powershell
powershell -ExecutionPolicy Bypass -File tools\commit-both.ps1 "<commit message>"
```

---

## Build & Test Commands

### Frontend
- **Production Build**:
  ```powershell
  cd frontend
  npm run build
  ```
  *Baseline budget check*: The build succeeds with 0 errors and a single budget warning (~89 kB over 1.00 MB initial bundle).
- **Run Unit Tests**:
  ```powershell
  cd frontend
  npx ng test --watch=false --browsers=ChromeHeadless
  ```
  *Baseline test results*: 589 passed, exactly 7 known baseline failures (`FundsListComponent` classification wiring & `StockDetailComponent` opinion cards).

### Backend
- **Solution File**: `backend/Meezan.slnx`
- **Build Backend**:
  ```powershell
  cd backend/src/Meezan.WebApi
  dotnet build
  ```
  *Baseline check*: Compiles with 0 errors and $\le 20$ baseline warnings.
- **Run Backend Tests**:
  ```powershell
  dotnet test backend/Meezan.slnx
  ```
  *Baseline check*: 443 passed, exactly 4 known baseline failures (snapshot schema drift & SQL count checks).

---

## Code Structure Conventions

### Frontend (`frontend/src/app/`)
- `core/`: Application singletons, API facade (`ApiService`), auth guards, shell layouts.
- `shared/`: Reusable design system (`mz-*`), dialogs, directives, models (`shared/models/`), and utility functions.
- `features/`: Routed feature modules (`admin`, `favorites`, `funds`, `home`, `indices`, `portfolio`, `sectors`, `sharia`, `stocks`).
- **Component File Rule**: Every Angular component has 4 companion files (`.ts`, `.html`, `.css`, `.spec.ts`).
- **Zero Inline Styles**: Never use inline `style="..."` attributes in HTML templates. Place all styles in the scoped component `.css`.

### Backend (`backend/src/`)
- `Meezan.Domain`: Pure domain entities and enums. Zero dependencies on EF Core or other layers.
- `Meezan.Application`: CQRS Commands and Queries using MediatR. Handlers return railway-oriented `Result` or `Result<T>`. Never throw unhandled exceptions.
- `Meezan.Infrastructure`: Entity Framework Core configurations (`IEntityTypeConfiguration<T>`), repository implementations, and external clients (Mubasher scraper, Snduk client).
- `Meezan.WebApi`: ASP.NET Core controllers and hosting configuration.
- **Single-Type Files**: Every class, enum, record, interface, and DTO must reside in its own dedicated source file.
- **Folder Size Limit**: Directories under active architecture rules must not exceed 15 files per directory (group into logical subfolders when needed).

---

## Documentation Links

- Frontend Documentation: `docs/frontend/README.md`
- Backend Documentation: `docs/backend/README.md`
- Refactor Verification Log: `docs/refactor-verification.md`
