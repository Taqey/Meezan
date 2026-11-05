# AGENTS.md - Multi-Agent Operating Manual for Meezan

This guide outlines architectural conventions, quality standards, and operating rules for automated agents working on the **Meezan** repository.

---

## 1. Operating Rules for AI Agents

1. **Autonomous Verification**:
   - Run tests and builds after every component or folder modification.
   - Never proceed to subsequent steps if new test failures or compile errors appear.
2. **Preserve Compatibility**:
   - Existing REST API contracts, JSON field casings, and database column names are immutable without explicit user authorization.
   - Do not perform database migrations or schema adjustments.
3. **Dual-Repo Sync**:
   - The root repository and `frontend/` sub-repository must remain synchronized.
   - Always invoke `powershell -ExecutionPolicy Bypass -File tools\commit-both.ps1 "<message>"` for version control checkpoints.

---

## 2. Fast Verification Commands

| Component | Command | Success Baseline |
| :--- | :--- | :--- |
| **Frontend Build** | `cd frontend; npx ng build` | 0 errors, 1 budget warning |
| **Frontend Tests** | `cd frontend; npx ng test --watch=false --browsers=ChromeHeadless` | 589 passed, 7 allowed baseline fails |
| **Backend Build** | `cd backend/src/Meezan.WebApi; dotnet build` | 0 errors, $\le 20$ baseline warnings |
| **Backend Tests** | `dotnet test backend/Meezan.slnx` | 443 passed, 4 allowed baseline fails |

---

## 3. Directory & Responsibility Map

```
Meezan/
├── frontend/src/app/
│   ├── core/                  # Core singletons, API facade, authentication
│   ├── shared/                # Design kit (mz-*), models, directives, utilities
│   └── features/              # Feature modules (admin, funds, stocks, etc.)
│
├── backend/src/
│   ├── Meezan.Domain/         # Domain entities, value objects, enums
│   ├── Meezan.Application/    # CQRS MediatR handlers, DTOs, interfaces
│   ├── Meezan.Infrastructure/ # EF Core DbContext, configurations, repositories
│   └── Meezan.WebApi/         # Controllers, filters, configuration
│
└── docs/                      # Subsystem architecture & refactor logs
```
