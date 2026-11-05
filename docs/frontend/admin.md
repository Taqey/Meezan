# Admin Control Panel & Automation

Path: `frontend/src/app/features/admin/`

## Modular Architecture

The administration dashboard is split into focused domain services, routed tab pages, and child components:

### 1. Routed Pages (`features/admin/pages/`)
- `AdminScrapingPageComponent`: Controls real-time scrapers, market data ingestion, and background job triggers.
- `AdminMarketDataPageComponent`: Manual market data overrides, EPS/BookValue corrections, and live stock price lookup.
- `AdminShariahEditPageComponent`: Manual Shariah classification overrides and board audit logging.
- `AdminIndexUploadPageComponent`: Ingestion of index constituents and EGX weightings from Excel/CSV.
- `AdminShariahSeedPageComponent`: Seeding Shariah audit databases from official PDF lists.
- `AdminPdfReportsPageComponent`: Archival and lookup of uploaded source compliance reports.
- `AdminReviewChecklistPageComponent`: Review queue for unclassified stocks or conflicting board verdicts.
- `AdminStocksMgmtPageComponent`: Add new stocks, assign sectors, and map index constituents.

### 2. Specialized Tab Hubs
- **`AdminFundsComponent` (`features/admin/admin-funds/`)**:
  - `AdminFundsSyncTabComponent`: Ingestion log history, sync errors, and retry controls.
  - `AdminFundsCategoriesTabComponent`: Fund category definitions, auto-derived keyword rules, and manual fund inclusions/exclusions.
  - `AdminFundsTableTabComponent`: Comprehensive fund master list with inline name and description editing.
  - `AdminFundsChangeLogTabComponent`: Category override history.
- **`AdminClassificationComponent` (`features/admin/admin-classification/`)**:
  - `AdminClassificationTreeTabComponent`: Full hierarchical taxonomy tree with drag-free reordering, moving, merging, and deleting.
  - `AdminClassificationReviewTabComponent`: Unclassified equity funds review, conflicting signals resolution, and rapid cascade assignment.
  - `AdminClassificationFundsPanelComponent`: Drawer for managing fund members inside any classification node.

### 3. Domain Services (`features/admin/services/`)
- Total of 20 decoupled domain services ensuring zero monolithic component bloat and clean dependency injection.

