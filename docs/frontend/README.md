# Frontend Documentation (Meezan Angular 19 SPA)

The Meezan frontend is a modern, standalone-first Angular 19 single-page application tailored for Egyptian Stock Exchange (EGX) investors and Islamic funds research. It provides Shariah compliance screening (across multiple independent scholarly standards), valuation models (Graham formula), quantitative technical levels (support/resistance pivots), and investment fund performance analysis.

---

## Architecture Overview

```
frontend/src/app/
├── core/                  # Singletons, core routing, auth & base API
│   ├── api/               # ApiService (HTTP client facade for backend endpoints)
│   ├── auth/              # Admin authentication & route guards
│   ├── layouts/           # Shell layouts (Navbar, Footer, App Shell)
│   └── services/          # Cross-cutting singletons (Theme, Toast, Meta)
│
├── shared/                # Universal UI components, models, directives & utilities
│   ├── components/        # Reusable design system cards, badges, dialogs, charts
│   │   ├── classification-cascade/
│   │   ├── stock-picker/
│   │   ├── stock-header-banner/
│   │   └── ui/            # Granular atomic UI kit (mz-* components)
│   ├── directives/        # DOM directives (e.g. appReveal animation)
│   ├── models/            # Domain interfaces, types, enums & visual helpers
│   │   ├── api.models.ts  # Facade re-exporting all domain models
│   │   ├── common.model.ts
│   │   ├── stocks.model.ts
│   │   ├── sectors.model.ts
│   │   ├── indices.model.ts
│   │   ├── shariah.model.ts
│   │   ├── funds.model.ts
│   │   ├── admin.model.ts
│   │   ├── fund-display.ts
│   │   └── shariah-standards.ts
│   └── utils/             # Helper utilities (navigation, math, formatting)
│
└── features/              # Routed feature domains
    ├── home/              # Market overview, trending stocks & top funds
    ├── stocks/            # Equities listing & in-depth multi-tab stock analysis
    ├── funds/             # Investment funds directory, manager profiles & detail
    ├── sectors/           # Market sectors directory & sector constituents
    ├── indices/           # EGX benchmark indices & constituent listings
    ├── sharia/            # Purification calculator & Shariah methodology
    ├── portfolio/         # Client-side investment portfolio tracking
    ├── favorites/         # LocalStorage watchlists
    └── admin/             # Admin control panel, scraping, sync & classification
```

---

## Feature Guides

1. [Architecture & Design Rules](./architecture.md)
2. [Stocks Feature](./stocks.md)
3. [Investment Funds Feature](./funds.md)
4. [Sectors & Indices Features](./sectors-and-indices.md)
5. [Shariah & Purification Feature](./sharia.md)
6. [Portfolio & Favorites Features](./portfolio-and-favorites.md)
7. [Admin Dashboard & Automation](./admin.md)
8. [Shared Design System & Components](./shared-components.md)

