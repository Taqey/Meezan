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

### Stop 7 (Step 1.2 — AdminFundsComponent Split)
- **Changes**:
  - Split `AdminFundsComponent` (923 lines TS, 743 lines HTML) into focused, single-responsibility units:
    - **4 Domain Services** in `features/admin/services/`:
      - `AdminFundsSyncLogService`: Manages sync history list, page pagination, and per-run detail error drilldown.
      - `AdminFundCategoriesService`: Manages category CRUD, auto-derived keyword rules, category membership, and change log.
      - `AdminFundDetailsService`: Manages fund search, name/description draft editing, and missing prices fetching.
      - `AdminFundTypesService`: Manages editable fund types, merge operations, and type-funds modal.
    - **1 Feature Model** in `features/admin/models/`: `FundCategoryRow`.
    - **5 Focused Child Components** in `features/admin/components/`:
      - `AdminFundsSyncTabComponent`: Tab view for sync history and failed detail logs.
      - `AdminFundsCategoriesTabComponent`: Categories table, category members drilldown, and add-to-category search.
      - `AdminFundsTableTabComponent`: Funds listing, inline name/description editor, and category overrides table.
      - `AdminFundsChangeLogTabComponent`: Category change history log.
      - `AdminFundsModalsComponent`: Category editor modal, merge modal, and confirmation dialog.
    - `AdminFundsComponent` reduced to a coordinator managing tab navigation.
- **Verification**:
  - Production build: `npx ng build` passed with 0 errors. Bundle size decreased by ~10 kB (`admin-routes` lazy chunk from 391 kB to 380 kB).
  - Test suite: `npx ng test --watch=false --browsers=ChromeHeadless` passed with **585 SUCCESS, 7 FAILED** (only the exact 7 baseline test failures).
- **Affected Route**:
  - `/admin/funds`.

### Stop 8 (Step 1.2 — AdminClassificationComponent Split)
- **Changes**:
  - Split `AdminClassificationComponent` (546 lines TS, 423 lines HTML) into single-responsibility units:
    - **3 Domain Services** in `features/admin/services/`:
      - `AdminClassificationTreeService`: Tree data loading, hierarchical flattening, node CRUD/editor form, keyword rules, move/merge/delete previews, and reclassification trigger. Decoupled from funds via callback.
      - `AdminClassificationReviewService`: Review data loading, unclassified equity funds, conflicting signals, missing sub-classifications, and bulk selection.
      - `AdminClassificationFundsService`: Node funds list, manual/auto filtering, pagination, search, cascade assignment, bulk move, and reset to auto.
    - **4 Focused Child Components** in `features/admin/components/`:
      - `AdminClassificationTreeTabComponent`: Interactive hierarchy tree with expand/collapse, active fund counts, and action buttons.
      - `AdminClassificationReviewTabComponent`: Review workflow tabs (unclassified, conflicting, missing child) with quick assignment cascade.
      - `AdminClassificationFundsPanelComponent`: Drawer/panel for managing funds belonging to a selected tree node.
      - `AdminClassificationModalsComponent`: Dialogs for node editing, moving, merging, deleting, bulk moving, and generic confirmations.
    - `AdminClassificationComponent` reduced to a thin coordinator (< 40 lines).
- **Verification**:
  - Production build: `npx ng build` passed with 0 errors and baseline bundle warning.
  - Test suite: `npx ng test --watch=false --browsers=ChromeHeadless` passed with **589 SUCCESS, 7 FAILED** (only the exact 7 baseline test failures).
- **Affected Route**:
  - `/admin/classification`.

### Stop 9 (Step 1.2 — Domain Models Split)
- **Changes**:
  - Split monolithic `shared/models/api.models.ts` (1,222 lines) into 7 domain model files in `shared/models/`:
    - `common.model.ts`: PagedResult, ValuationConfidence, PriceComparison, ShariahStatus.
    - `stocks.model.ts`: StockListItemDto, FairValueMethodDto, MarketDataDto, SupportResistanceDto.
    - `sectors.model.ts`: SectorSummaryDto, SectorPickerItem.
    - `indices.model.ts`: IndexSummaryDto, MarketSnapshotDto, IndexInStockDto, ConstituentItemDto, IndexConstituentsPagedResultDto, INDEX_ARABIC_NAMES, IndexPickerItem.
    - `shariah.model.ts`: ShariahSourceKey, SHARIAH_BOARD_FROZEN_TICKERS, SHARIAH_SOURCE_NAMES, ShariahSourceOpinionDto, RatioCheckDto, CriterionResultDto, StoredVerdictDetailsDto, StockShariahMetricsDto, ShariahOverridesDto, SaveShariahOverridesRequest.
    - `funds.model.ts`: FundType, FundRiskLevel, FundSortBy, FundListItemDto, EffectiveClassificationDto, FundDetailDto, FundPricePointDto, FundAnalysisDto, FundSimilarDto, FundTypeOptionDto, FundClassificationTreeNode, AdminClassificationNode, FundCategory, FundSyncLog, etc.
    - `admin.model.ts`: RemovalCandidateDto, RefreshSelectedStocksResult, LiveProgressInfo, JobProgressInfo, LastRunSummary, ScrapeStatusResponse, RunCombinedScrapeResult, SeedShariahResultDto, SourcePdfStatusDto, UploadIndexFileResultDto, ManualMarketDataUpdateRequest, StockManagementItem, etc.
  - Converted `api.models.ts` into a clean facade barrel re-exporting all 7 domain model modules with zero changes to existing consumers.
- **Verification**:
  - Production build: `npx ng build` passed with 0 errors and baseline bundle warning.
  - Test suite: `npx ng test --watch=false --browsers=ChromeHeadless` passed with **589 SUCCESS, 7 FAILED** (exact 7 baseline failures).
- **Affected Routes**:
  - All routes (transparent facade).

### Stop 11 (Step 1.3 - 1.6 Frontend Completion & Diagnostics Cleanup)
- **Changes**:
  - Resolved all extended diagnostics and template markers across the frontend (`NG8102` nullish coalescing checks in `admin-classification-review-tab.component.html` and `stock-detail.component.html`).
  - Added dedicated component getters (`priceChangeText`, `priceChangeTone`) ensuring robust null safety and zero template warnings.
- **Verification**:
  - Angular production build: 0 errors, 1 baseline budget warning.
  - Angular test suite: 589 passed, 7 allowed baseline failures.

---

## Phase 2: Backend (.NET 10 WebApi) Verification Log

### Stop 12 (Phase 2.1 — Meezan.Domain Multi-Type Files Split)
- **Changes**:
  - Split multi-type files in `backend/src/Meezan.Domain/Entities/Funds/`:
    - Extracted 9 enums (`CategoryChangeAction`, `ClassificationChangeAction`, `ClassificationOverrideAction`, `FundRiskLevel`, `FundSyncDetailStatus`, `FundSyncStatus`, `FundType`, `FundTypeChangeAction`, `OverrideAction`).
    - Extracted entities (`FundPriceHistory`, `FundSimilar`, `FundCategoryOverride`, `FundCategoryChangeLog`, `FundClassificationOverride`, `FundClassificationChangeLog`, `FundSyncLogDetail`, `FundTypeOverride`, `FundTypeChangeLog`).
    - Every domain entity and enum is now in its own dedicated `.cs` file.
- **Verification**:
  - `dotnet build` compiled with 0 errors and baseline 20 warnings.

### Stop 13 (Phase 2.1 — Meezan.Infrastructure Repositories Split)
- **Changes**:
  - Monolithic `FundRepository.cs` (which contained 17 distinct repository implementations in one file) split into 17 individual repository files in `backend/src/Meezan.Infrastructure/Persistence/Repositories/Funds/`:
    - `FundAnalysisRepository`, `FundCategoryRepository`, `FundCategoryChangeLogRepository`, `FundCategoryOverrideRepository`, `FundClassificationBackupRepository`, `FundClassificationChangeLogRepository`, `FundClassificationNodeRepository`, `FundClassificationOverrideRepository`, `FundPriceHistoryRepository`, `FundRepository`, `FundSimilarRepository`, `FundSyncLogDetailRepository`, `FundSyncLogRepository`, `FundSyncSettingsRepository`, `FundTypeChangeLogRepository`, `FundTypeDefinitionRepository`, `FundTypeOverrideRepository`.
- **Verification**:
  - `dotnet build` compiled with 0 errors and 20 baseline warnings.

### Stop 14 (Phase 2.1 — Meezan.Application DTOs, Queries & Contracts Split)
- **Changes**:
  - `HistoricalAndNewFundDtos.cs` split into 8 separate DTO files.
  - `FundDetailDto.cs` split into 5 separate files (`FundDetailDto`, `FundPricePointDto`, `FundAnalysisDto`, `FundSimilarDto`, `FundCategoryMembershipDto`).
  - `ImportFaisalOsoulOpinionsCommand.cs` standalone models extracted into 4 files (`FaisalOsoulSourceBlock`, `FaisalOsoulOpinionsFile`, `ImportSourceOpinionsResult`, `ImportFaisalOsoulOpinionsResult`).
  - `ShariahResponseDtos.cs` split into 4 files (`ShariahComplianceDto`, `ShariahSourceOpinionDto`, `StockShariahMetricsDto`, `FullStockShariahDto`).
  - `SndukModels.cs` split into 24 individual model files and `SndukJsonConverters.cs`.
  - `GetCategoryFundsQuery.cs` split into 3 files.
  - `FundPriceHistoryBackfillResultDto.cs` split into 3 files.
  - `TypeFundsResultDto.cs` split into 3 files.
  - `IScrapeProgressTracker.cs` and `IStockScraperClient.cs` split into dedicated interface and record files.
  - `IIndexRepository.cs` and `IStockRepository.cs` projections/filters extracted to dedicated files.
  - `SectorWeightCalculator.cs` calculation result records extracted to dedicated files.
  - `IMubasherMarketSnapshotClient.cs`, `IScrapeBatchCommitter.cs`, `IFundSyncProgressTracker.cs`, `ISectorDailyService.cs`, `IShariahComplianceRepository.cs`, `ExternalStockMergedDto.cs`, `GetCategoryChangesQuery.cs`, `FundCategoryStatusDto.cs`, `GetAdminFundsQuery.cs`, `GetSyncHistoryQuery.cs`, and `UploadIndexFileResultDto.cs` split into single-type files.
- **Verification**:
  - `dotnet build` compiled with 0 errors and 20 baseline warnings.

### Stop 15 (Phase 2.1 — Meezan.Infrastructure EF Configurations Split)
- **Changes**:
  - Multi-configuration classes split into single-type files:
    - `FundAnalysisConfiguration.cs`, `FundPriceHistoryConfiguration.cs`, `FundSimilarConfiguration.cs`
    - `FundCategoryConfiguration.cs`, `FundCategoryOverrideConfiguration.cs`
    - `StockFairValueConfiguration.cs`, `StockFairValueMethodConfiguration.cs`
    - `FundClassificationConfiguration.cs`, `FundClassificationOverrideConfiguration.cs`, `FundClassificationBackupConfiguration.cs`, `FundClassificationChangeLogConfiguration.cs`
    - `FundSyncConfiguration.cs`, `FundSyncLogDetailConfiguration.cs`, `FundSyncSettingsConfiguration.cs`, `FundCategoryChangeLogConfiguration.cs`
    - `FundTypeConfiguration.cs`, `FundTypeOverrideConfiguration.cs`, `FundTypeChangeLogConfiguration.cs`
- **Verification**:
  - `dotnet build` compiled with 0 errors and 20 baseline warnings.
  - EF Core assembly scan continues to discover 100% of configurations seamlessly.

### Stop 16 (Phase 2.1 — Meezan.WebApi Admin Request DTOs Split)
- **Changes**:
  - Extracted nested request/response records from `AdminMarketDataController.cs` into `backend/src/Meezan.WebApi/Models/Admin/`:
    - `ManualMarketDataUpdateRequest`, `ManualMarketDataUpdateResponse`, `AdminStockLookupItem`, `StockDataStatusItem`, `UpdateDataStatusRequest`, `ShariahOverridesDto`, `SaveShariahOverridesRequest`.
  - Extracted nested request records from `AdminSectorDailyController.cs` into `backend/src/Meezan.WebApi/Models/Admin/`:
    - `UpdateSectorDailyConfigRequest`, `RecomputeRangeRequest`.
- **Verification**:
  - `dotnet build` compiled with 0 errors and 20 baseline warnings.

### Stop 17 (Phase 2.2 & 2.4 — Verification & Test Suite Parity)
- **Changes**:
  - Verified full test suite against solution file `backend/Meezan.slnx`.
  - Exactly 443 passed, 4 allowed baseline failures (snapshot schema drift & count tests). Zero regression.
  - Verified `dotnet build` (0 errors, 20 warnings baseline).
  - Verified `npx ng build` (0 errors, 1 baseline budget warning).
  - Verified `npx ng test` (589 passed, 7 allowed baseline failures).
- **Architecture & Boundaries**:
  - Clean Architecture layer boundaries confirmed intact.
  - No database schema, migration, or public API route altered.




