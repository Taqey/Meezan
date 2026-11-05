# Refactor verification log

Branch: `refactor/structure`. Baseline: initial bundle 89.37 kB over the 1.00 MB budget; 7 failing tests (6 FundsList classification wiring, 1 StockDetail opinion cards).
Verification kind: build + full test suite + static checks (no browser run: starting the backend against the shared DB is not safe, so no screenshot comparison).

## Stop 1 - shared components/ (b479d3e)
- 16 components: inline template/styles moved to .html/.css (content byte-identical, extracted by evaluating the template literal), 14 specs added.
- Verified: build ok, 498 pass / 7 baseline fail.
- Human look: /stocks, /funds, /sectors, /indices cards, badges, treemap, toasts.

## Stop 2 - components/ui (d452021)
- 5 multi-declaration files split into 40 component folders, 22 models, 7 util functions, 5 constants; barrel regenerated; consumers import files directly (barrel caused a circular import).
- Verified: build ok, 535 pass / 7 baseline fail.
- Human look: listing pages and detail pages (mz-* components), spinner buttons.

## Stop 3 - pages, layouts, app.component inline templates
- 23 files: inline template/styles moved to .html/.css, missing specs added.
- Verified: build ok (bundle 89.19 kB over, was 89.37), 555 pass / 7 baseline fail.
- Human look: every route, public and admin, once at the end.

## Stop 4 - global CSS
- `styles.css` (4087 lines) and `ui-kit.css` (1332 lines) split into contiguous chunks: `src/styles/ui-kit/*` (11) and `src/styles/global/*` (24); `styles.css` is now only ordered @imports (ui-kit first, then global), same order as before. No selector renamed or merged.
- Verified: built bundle CSS is byte-identical to before (same content hash styles-S4TYUFXR.css, 109948 bytes). Build ok.
- Human look: none needed.

## Stop 5 - folder restructure (end of 1.1)
- 305 files moved with all relative imports rewritten by tools/move-files.mjs: core/{api,auth,layouts,services}, shared/{components,directives,models,utils}, features/{admin,favorites,funds,home,indices,portfolio,sectors,sharia,stocks}. Routes, URLs and lazy chunks unchanged.
- Verified: build ok (bundle 89.19 kB over, baseline 89.37), 555 pass / 7 baseline fail.
- Human look: none beyond final all-routes pass.

## Stop 5b - ensure 4 files per component (.html, .css, .ts, .spec.ts)
- Added 62 empty .css files and styleUrl bindings to components that had no inline styles previously.
- Verified: build ok (89.19 kB over budget).

### Stop 6 (Step 1.2 — AdminComponent Split)
- **Changes**:
  - Split the monolithic `AdminComponent` (over 2,000 lines TS, 2,017 lines HTML) into single-responsibility units:
    - **13 Domain Services** in `features/admin/services/`:
      - `AdminScrapingJobsService`, `AdminSectorDailyService`, `AdminFundsSyncService`, `AdminHistoricalSyncService`, `AdminFundCompareService`, `AdminStockLookupService`, `AdminMarketDataService`, `AdminShariahOverridesService`, `AdminIndexUploadService`, `AdminShariahSeedService`, `AdminSourcePdfService`, `AdminRemovalCandidatesService`, `AdminStocksManagementService`.
    - **1 Feature Model** in `features/admin/models/`: `EditableMarketForm`.
    - **2 Reusable Shared Components** in `shared/components/`:
      - `StockPickerComponent`: Stock search input, select dropdown, and matching chips.
      - `StockHeaderBannerComponent`: Stock details header banner with live updates, sector badge, and fair value / Shariah pills.
    - **15 Focused Child Components** in `features/admin/components/`:
      - `AdminPageHeadingComponent`, `JobCardComponent`, `FundsSyncCardComponent`, `ScrapeProgressComponent`, `SectorDailyPanelComponent`, `LastRunTableComponent`, `HistoricalSyncModalComponent`, `FundCompareChartModalComponent`, `MarketDataFormComponent`, `ShariahOverridesPanelComponent`, `BourseHalalStandardsTableComponent`, `SourcePdfStatusCardComponent`, `PdfUploadFormComponent`, `CreateStockFormComponent`, `StocksManagementTableComponent`.
    - **8 Routed Tab Pages** in `features/admin/pages/`:
      - `AdminScrapingPageComponent`, `AdminMarketDataPageComponent`, `AdminShariahEditPageComponent`, `AdminIndexUploadPageComponent`, `AdminShariahSeedPageComponent`, `AdminPdfReportsPageComponent`, `AdminReviewChecklistPageComponent`, `AdminStocksMgmtPageComponent`.
    - `AdminComponent` reduced to a coordinator (< 65 lines).
- **Verification**:
  - Production build: `npx ng build` passed with 0 errors and identical baseline bundle size warnings.
  - Test suite: `npx ng test --watch=false --browsers=ChromeHeadless` passed with **580 SUCCESS, 7 FAILED** (only the exact 7 baseline test failures). All newly generated component specs passed.
- **Affected Routes**:
  - `/admin/market-data`, `/admin/manual-edit`, `/admin/shariah-edit`, `/admin/index-files`, `/admin/shariah-seed`, `/admin/shariah-reports`, `/admin/review-queue`, `/admin/stocks`.
