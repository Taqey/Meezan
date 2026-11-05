# Shared Design System & Components

Path: `frontend/src/app/shared/components/`

## Core Shared Components

- **`StockPickerComponent`**:
  - Reusable stock search input with keyboard navigation and matching chips.
  - Used in Admin and Portfolio creation workflows.

- **`StockHeaderBannerComponent`**:
  - Standardized live stock quote banner with ticker, name, price, change, sector, and Shariah status pill.

- **`ClassificationCascadeComponent`**:
  - Multi-tier cascading dropdown for navigating and assigning hierarchical fund classifications (Category -> Sub-category -> Strategy).

- **`EntityTableComponent` & `EntityCardComponent`**:
  - Unified display cards and table grids for stock and fund listings with responsive mobile support.

- **`FavoriteToggleComponent`**:
  - Heart icon button tied to the LocalStorage favorites service.

- **`LoadingSkeletonsComponent`**:
  - Accessible pulse placeholders for loading cards, tables, and detail screens.

- **`TreemapComponent`**:
  - Dynamic visual market-map representation sizing companies by market cap and colored by percentage change.

---

## Atomic UI Kit (`shared/components/ui/`)

A comprehensive suite of prefix-namespaced components (`mz-*`) providing the visual design language of Meezan:
- **Navigation & Layout**: `mz-details-page`, `mz-listing-page`, `mz-page-header`, `mz-details-section`, `mz-back-link`, `mz-pagination`
- **Data Display**: `mz-listing-card`, `mz-details-header-card`, `mz-stat-card`, `mz-fair-value-card`, `mz-support-resistance-card`, `mz-sharia-authorities-section`, `mz-market-indicators-section`, `mz-allocation-breakdown`
- **Interactive Controls**: `mz-button`, `mz-select`, `mz-search-input`, `mz-filter-dropdown`, `mz-filters-bar`, `mz-quick-filter-tabs`, `mz-favorite-button`
- **Badges & Tags**: `mz-badge`, `mz-chip`, `mz-status-tag`
