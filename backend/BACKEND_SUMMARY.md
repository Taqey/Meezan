# Meezan Backend — Actual Implementation Specification & Architecture Audit

This document describes the actual state of the **Meezan** ASP.NET Core backend codebase as implemented in `d:/borsa/New folder (4)/backend`. All paths, types, signatures, schemas, and configurations reflect the live code.

---

## 1. High-level Overview

Meezan is an Egyptian Stock Exchange (EGX) tracking and analysis backend built with ASP.NET Core 10, Entity Framework Core, and SQL Server. It ingests EGX index constituent sheets via Excel uploads, seeds and syncs Shariah compliance ratings and financial purity metrics across 8 external sources, and runs an automated daily scraping engine (targeting Mubasher market data and support/resistance levels) to calculate multi-method fair values using sector-peer medians and IQR-based outlier detection.

---

## 2. Architecture — As Implemented

### Solution Projects and Namespaces
- **`Meezan.Domain`** (`backend/src/Meezan.Domain`): Contains core entity models, database configurations/enums (`ShariahStatus`, `PriceComparison`, `ValuationConfidence`, `ShariahSourceKey`), completely free of external dependencies.
- **`Meezan.Application`** (`backend/src/Meezan.Application`): Implements CQRS with MediatR, FluentValidation validators, common DTOs/pagination models, and domain interfaces (`IStockRepository`, `IIndexRepository`, `IShariahComplianceRepository`, `IStockScraperClient`, `IScrapeProgressTracker`, etc.).
- **`Meezan.Infrastructure`** (`backend/src/Meezan.Infrastructure`): Implements EF Core `ApplicationDbContext`, entity type configurations via Fluent API, repository implementations, Excel parsing via `ExcelDataReader`, external HTTP clients (`MubasherScraperClient`, `ShariahSourceClient`), batch committer, in-memory live progress tracker, and background services (`ScrapingBackgroundService`, `ShariahRefreshBackgroundService`).
- **`Meezan.WebApi`** (`backend/src/Meezan.WebApi`): ASP.NET Core Web API layer hosting controllers (`StocksController`, `IndicesController`, `ShariahController`, `ScrapingController`), global error handling middleware (`ExceptionHandlingMiddleware`), CORS policy, and Swagger / OpenAPI integration.

### Actual Solution & Directory Tree
```
backend/
├── Meezan.slnx
└── src/
    ├── Meezan.Domain/
    │   ├── Entities/
    │   │   ├── Index.cs
    │   │   ├── IndexConstituent.cs
    │   │   ├── ScrapeRunLog.cs
    │   │   ├── Sector.cs
    │   │   ├── ShariahCompliance.cs
    │   │   ├── ShariahRefreshLog.cs
    │   │   ├── ShariahSourceOpinion.cs
    │   │   ├── Stock.cs
    │   │   ├── StockFairValue.cs
    │   │   ├── StockFairValueMethod.cs
    │   │   ├── StockMarketData.cs
    │   │   ├── StockShariahMetrics.cs
    │   │   ├── StockSupportResistance.cs
    │   │   └── UploadHistory.cs
    │   └── Enums/
    │       ├── PriceComparison.cs
    │       ├── ShariahSourceKey.cs
    │       ├── ShariahStatus.cs
    │       └── ValuationConfidence.cs
    ├── Meezan.Application/
    │   ├── Common/
    │   │   ├── Interfaces/
    │   │   │   ├── IExcelParserService.cs
    │   │   │   ├── IIndexRepository.cs
    │   │   │   ├── IScrapeBatchCommitter.cs
    │   │   │   ├── IScrapeProgressTracker.cs
    │   │   │   ├── IScrapeRunLogRepository.cs
    │   │   │   ├── IShariahComplianceRepository.cs
    │   │   │   ├── IShariahRefreshLogRepository.cs
    │   │   │   ├── IShariahSourceClient.cs
    │   │   │   ├── IShariahSourceOpinionRepository.cs
    │   │   │   ├── IStockFairValueRepository.cs
    │   │   │   ├── IStockMarketDataRepository.cs
    │   │   │   ├── IStockRepository.cs
    │   │   │   ├── IStockScraperClient.cs
    │   │   │   ├── IStockShariahMetricsRepository.cs
    │   │   │   ├── IStockSupportResistanceRepository.cs
    │   │   │   ├── IUnitOfWork.cs
    │   │   │   └── IUploadHistoryRepository.cs
    │   │   ├── Models/
    │   │   ├── PagedResult.cs
    │   │   └── PageSizeHelper.cs
    │   ├── Features/
    │   │   ├── Indices/
    │   │   │   ├── Commands/UploadIndexFile/
    │   │   │   └── Queries/
    │   │   │       ├── GetIndexConstituents/
    │   │   │       └── GetIndicesList/
    │   │   ├── Scraping/
    │   │   │   ├── Commands/RunCombinedScrape/
    │   │   │   ├── Queries/
    │   │   │   │   ├── GetMarketData/
    │   │   │   │   ├── GetStocksList/
    │   │   │   │   └── GetSupportResistance/
    │   │   │   └── Services/
    │   │   │       └── FairValueCalculator.cs
    │   │   └── Shariah/
    │   │       ├── Commands/
    │   │       │   ├── RefreshShariahData/
    │   │       │   └── SeedShariahMap/
    │   │       ├── DTOs/
    │   │       └── Queries/
    │   │           ├── GetAllShariahCompliance/
    │   │           ├── GetFullStockShariah/
    │   │           └── GetShariahComplianceBySymbol/
    │   └── DependencyInjection.cs
    ├── Meezan.Infrastructure/
    │   ├── Persistence/
    │   │   ├── ApplicationDbContext.cs
    │   │   ├── Configurations/  (13 IEntityTypeConfiguration classes)
    │   │   └── Repositories/     (11 repository classes)
    │   ├── Services/
    │   │   ├── ExcelParserService.cs
    │   │   ├── MubasherScraperClient.cs
    │   │   ├── ScrapeBatchCommitter.cs
    │   │   ├── ScrapeProgressTracker.cs
    │   │   ├── ScrapingBackgroundService.cs
    │   │   ├── ShariahRefreshBackgroundService.cs
    │   │   ├── ShariahSourceClient.cs
    │   │   └── UnitOfWork.cs
    │   └── DependencyInjection.cs
    └── Meezan.WebApi/
        ├── Controllers/
        │   ├── IndicesController.cs
        │   ├── ScrapingController.cs
        │   ├── ShariahController.cs
        │   └── StocksController.cs
        ├── Middleware/
        │   └── ExceptionHandlingMiddleware.cs
        ├── appsettings.json
        └── Program.cs
```

---

## 3. Full Data Model — As Implemented

Entity configurations are registered via EF Core Fluent API in `Meezan.Infrastructure/Persistence/Configurations/`:

### 1. `Stocks` (`StockConfiguration.cs`)
- `Id` (`int`, PK, Identity)
- `Ticker` (`nvarchar(50)`, Required, Unique Index)
- `NameAr` (`nvarchar(250)`, Nullable)
- `NameEn` (`nvarchar(250)`, Nullable)
- `SectorId` (`int`, Nullable, FK to `Sectors.Id`, `OnDelete: SetNull`)
- `CreatedAt` (`datetime2`, Required)
- `UpdatedAt` (`datetime2`, Required)

### 2. `Sectors` (`SectorConfiguration.cs`)
- `Id` (`int`, PK, Identity)
- `NameAr` (`nvarchar(200)`, Required)
- `NameEn` (`nvarchar(200)`, Required)

### 3. `Indices` (`IndexConfiguration.cs`)
- `Id` (`int`, PK, Identity)
- `Code` (`nvarchar(50)`, Required, Unique Index)
- `NameAr` (`nvarchar(250)`, Required)
- `NameEn` (`nvarchar(250)`, Required)
- `Description` (`nvarchar(500)`, Nullable)
- `LastUpdated` (`datetime2`, Nullable)
- *Pre-seeded with 8 official indices:* `EGX30`, `EGX30TR`, `EGX70`, `EGX100`, `EGX35-LV`, `Shariah`, `Sectoral-Indices`, `TAMAYUZ`.

### 4. `IndexConstituents` (`IndexConstituentConfiguration.cs`)
- `Id` (`int`, PK, Identity)
- `IndexId` (`int`, Required, FK to `Indices.Id`, `OnDelete: Cascade`)
- `StockId` (`int`, Required, FK to `Stocks.Id`, `OnDelete: Cascade`)
- `Weight` (`decimal(18,8)`, Required)
- `EffectiveDate` (`datetime2`, Required)
- *Constraint: Unique index on `(IndexId, StockId)`.*

### 5. `UploadHistories` (`UploadHistoryConfiguration.cs`)
- `Id` (`int`, PK, Identity)
- `IndexId` (`int`, Required, FK to `Indices.Id`, `OnDelete: Cascade`)
- `FileName` (`nvarchar(260)`, Required)
- `UploadedAt` (`datetime2`, Required)
- `UploadedBy` (`nvarchar(100)`, Nullable)
- `RowsProcessed` (`int`, Required)
- `RowsInserted` (`int`, Required)
- `RowsUpdated` (`int`, Required)
- `RowsSkipped` (`int`, Required)
- `Status` (`nvarchar(50)`, Required)
- `ErrorMessage` (`nvarchar(max)`, Nullable)

### 6. `ShariahCompliances` (`ShariahComplianceConfiguration.cs`)
- `Id` (`int`, PK, Identity)
- `StockId` (`int`, Required, FK to `Stocks.Id`, `OnDelete: Cascade`, Unique Index on `StockId`)
- `Status` (`int` mapped from `ShariahStatus` enum, Required)
- `Pct` (`decimal(18,4)`, Nullable)
- `Note` (`nvarchar(500)`, Nullable)
- `LastCheckedAt` (`datetime2`, Required)
- `UpdatedAt` (`datetime2`, Required)

### 7. `StockShariahMetrics` (`StockShariahMetricsConfiguration.cs`)
- `Id` (`int`, PK, Identity)
- `StockId` (`int`, Required, FK to `Stocks.Id`, `OnDelete: Cascade`, Unique Index on `StockId`)
- `Zakat`, `SpHaramEarningPercentage`, `AaoifiHaramEarningPerShare`, `HaramEarningsPercentage`, `LoansPercentage`, `FairValueValuation`, `BookValue`, `Profit`, `Dividend` (`decimal(18,4)`, Nullable)
- `DividendType` (`nvarchar(50)`, Nullable)
- `CoreActivityCompliant`, `CashLiquidityCompliant`, `HaramInvestmentsCompliant` (`bit`, Nullable)
- `CategoryEn`, `CategoryAr` (`nvarchar(100)`, Nullable)
- `SourceUpdatedAt` (`datetime2`, Nullable)
- `FetchedAt` (`datetime2`, Required)

### 8. `ShariahSourceOpinions` (`ShariahSourceOpinionConfiguration.cs`)
- `Id` (`int`, PK, Identity)
- `StockId` (`int`, Required, FK to `Stocks.Id`, `OnDelete: Cascade`)
- `SourceKey` (`int` mapped from `ShariahSourceKey` enum, Required)
- `Status` (`nvarchar(50)`, Nullable)
- `Percentage` (`decimal(18,4)`, Nullable)
- `Note` (`nvarchar(1000)`, Nullable)
- `PdfUrl` (`nvarchar(500)`, Nullable)
- `SourceLastUpdated` (`datetime2`, Nullable)
- `FetchedAt` (`datetime2`, Required)
- `ExtraData` (`nvarchar(max)`, Nullable)
- *Constraint: Unique index on `(StockId, SourceKey)`.*

### 9. `ShariahRefreshLogs` (`ShariahRefreshLogConfiguration.cs`)
- `Id` (`int`, PK, Identity)
- `RunAt` (`datetime2`, Required)
- `TriggeredBy` (`nvarchar(50)`, Required)
- `PctUpdatedCount` (`int`, Required)
- `SkippedNoValueCount` (`int`, Required)
- `SkippedNotFoundCount` (`int`, Required)
- `StocksFullyRefreshedCount` (`int`, Required)

### 10. `StockMarketData` (`StockMarketDataConfiguration.cs`)
- `Id` (`int`, PK, Identity)
- `StockId` (`int`, Required, FK to `Stocks.Id`, `OnDelete: Cascade`, Unique Index on `StockId`)
- `NominalValue`, `MarketValue`, `BookValue`, `PbRatio`, `Eps`, `PeRatio`, `High`, `Low`, `Open`, `ClosingPrice` (`decimal(18,4)`, Nullable)
- `Currency` (`nvarchar(20)`, Nullable)
- `SourceLastUpdateText` (`nvarchar(100)`, Nullable)
- `FetchedAt` (`datetime2`, Required)

### 11. `StockFairValues` (`StockFairValueConfiguration.cs`)
- `Id` (`int`, PK, Identity)
- `StockId` (`int`, Required, FK to `Stocks.Id`, `OnDelete: Cascade`, Unique Index on `StockId`)
- `FairValue`, `FairValueDiff`, `FairValueDiffPct` (`decimal(18,4)`, Nullable)
- `PriceComparison` (`nvarchar(20)` mapped from enum, Nullable)
- `MethodsUsedCount`, `MethodsExcludedCount` (`int`, Nullable)
- `Confidence` (`nvarchar(20)` mapped from `ValuationConfidence` enum, Nullable)
- `CalculatedAt` (`datetime2`, Required)

### 12. `StockFairValueMethods` (`StockFairValueConfiguration.cs`)
- `Id` (`int`, PK, Identity)
- `FairValueId` (`int`, Required, FK to `StockFairValues.Id`, `OnDelete: Cascade`)
- `MethodName` (`nvarchar(50)`, Required)
- `EstimatedValue` (`decimal(18,4)`, Nullable)
- `IsOutlier` (`bit`, Required)

### 13. `StockSupportResistance` (`StockSupportResistanceConfiguration.cs`)
- `Id` (`int`, PK, Identity)
- `StockId` (`int`, Required, FK to `Stocks.Id`, `OnDelete: Cascade`, Unique Index on `StockId`)
- `LastPrice`, `ChangePct`, `Pivot`, `R1`, `R2`, `S1`, `S2` (`decimal(18,4)`, Nullable)
- `FetchedAt` (`datetime2`, Required)

### 14. `ScrapeRunLogs` (`ScrapeRunLogConfiguration.cs`)
- `Id` (`int`, PK, Identity)
- `StartedAt` (`datetime2`, Required)
- `FinishedAt` (`datetime2`, Nullable)
- `TotalStocks`, `SucceededStocks`, `FailedStocks` (`int`, Required)
- `TriggeredBy` (`nvarchar(50)`, Required)
- `ErrorSummary` (`nvarchar(max)`, Nullable)

### Schema Deviations from Preliminary Specs:
- **`SymbolCode` vs `Ticker`**: In the domain model and SQL schema, the stock identifier is represented uniformly as `Ticker` (`Stock.Ticker`) with a unique index. Methods accepting `SymbolCode` route directly to `Ticker`.
- **Enum SQL Storage**: `PriceComparison` and `ValuationConfidence` are stored as strings (`nvarchar(20)`) via EF value conversions, providing human-readable representations in SQL queries.

---

## 4. The Data Flows — As Implemented

### Flow 1: Index Excel Upload
1. **HTTP Action**: `POST /api/indices/{indexCode}/upload` (`IndicesController.UploadIndexFile`).
2. **MediatR**: Dispatches `UploadIndexFileCommand` to `UploadIndexFileCommandHandler`.
3. **Execution Steps**:
   - Validates command via `FluentValidation` and verifies index existence via `IIndexRepository.GetByCodeAsync`.
   - Parses stream with `IExcelParserService.ParseIndexConstituentsAsync` using `ExcelDataReader`.
   - Begins a database transaction (`IUnitOfWork.BeginTransactionAsync`).
   - For each constituent row, gets or creates `Sector` if present (`_stockRepository.GetOrCreateSectorAsync`), checks or creates `Stock` by ticker.
   - Replaces constituents: deletes existing `IndexConstituents` where `IndexId == index.Id` and bulk adds new `IndexConstituent` entities (`_indexRepository.ReplaceConstituentsAsync`).
   - Updates `Index.LastUpdated = DateTime.UtcNow`.
   - Appends an `UploadHistory` log entry.
   - Calls `_unitOfWork.SaveChangesAsync` and commits transaction (`_unitOfWork.CommitTransactionAsync`).

### Flow 2: Shariah Seed and Refresh
1. **Seed (`POST /api/shariah/seed`)**:
   - Handled by `SeedShariahMapCommandHandler`.
   - Reads dictionary payload or bundled local/resource `shariah_map.json`.
   - Iterates through symbols: creates `Stock` if missing (sets `Ticker`, leaves `NameAr/En` null for index upload to provide).
   - Upserts `ShariahCompliance` rows (`Status`, `Pct`, `Note`, `LastCheckedAt`, `UpdatedAt`).
   - Single `SaveChangesAsync` call at the end.
2. **Refresh (`POST /api/shariah/refresh` or background service)**:
   - Handled by `RefreshShariahDataCommandHandler`.
   - Fetches external array from `IShariahSourceClient.FetchMergedStocksAsync` (reading `stocks_merged.json`).
   - For each entry:
     - Finds or creates `Stock` (backfilling `NameAr/NameEn` if null or equal to ticker placeholder).
     - **Always overwrites `StockShariahMetrics`**: updates all 15 financial metrics or adds a new row.
     - **Always overwrites `ShariahSourceOpinion`**: for all 8 sources (`HalalBourse`, `Musaffa`, `Kashif`, `HalalInvest`, `FaisalBank`, `Osoul`, `Thndr`, `MohamedRamadanSharia`), updates existing or adds new opinion.
     - **Fill-only rule for `ShariahCompliance.Pct`**: strictly checks:
       `if (compliance.Status == ShariahStatus.Compliant && compliance.Pct == null && item.SpHaramEarningPercentage.HasValue)` -> sets `compliance.Pct`. If `compliance.Pct` was already populated, it is **never overwritten**.
     - Logs execution in `ShariahRefreshLog` and calls `_unitOfWork.SaveChangesAsync()`.

### Flow 3: Market-Data Scrape Run
1. **Trigger**: `POST /api/scraping/run` or `ScrapingBackgroundService`.
2. **MediatR**: Dispatches `RunCombinedScrapeCommand` to `RunCombinedScrapeCommandHandler`.
3. **Execution Steps**:
   - Creates a pending `ScrapeRunLog` in database.
   - Initializes in-memory `IScrapeProgressTracker` with `StartRun(TriggeredBy, total)`.
   - **Phase 1 (In-Memory Scraping)**: Iterates over all stocks. Fetches HTML/JSON via `MubasherScraperClient` for both market data and support/resistance without touching live database tables. Records per-stock success/fail in progress tracker.
   - **Phase 2 (Sector Peer Medians)**: Pools PE and PB values across all scraped stocks grouped by `SectorId`. For every stock, calculates the median PE and PB of its sector peers (excluding the stock's own PE/PB).
   - **Phase 3 (Multi-Method Fair Value & IQR)**: Runs `FairValueCalculator.BuildMethodEstimates`:
     - Method 1: Graham Number `√(22.5 × EPS × BookValue)`.
     - Method 2: Sector-Median PE × EPS.
     - Method 3: Sector-Median PB × BookValue.
     - Method 4: Direct BookValue.
     - Evaluates estimates via Tukey IQR fences ($Q_1 - 1.5 \times IQR$ to $Q_3 + 1.5 \times IQR$) with linear rank interpolation. Averages non-outliers to yield `FairValue`, determines `PriceComparison` (±5% tolerance: Cheap/Fair/Expensive), and assigns `ValuationConfidence`.
   - **Phase 4 (Staging & Atomic Commit)**:
     - Calls `IScrapeBatchCommitter.CommitAsync` (`ScrapeBatchCommitter.cs`).
     - Opens a database transaction (`BeginTransactionAsync`).
     - Deletes old data via bulk delete statements:
       `StockFairValueMethods.ExecuteDeleteAsync()`, `StockFairValues.ExecuteDeleteAsync()`, `StockMarketData.ExecuteDeleteAsync()`, `StockSupportResistance.ExecuteDeleteAsync()`.
     - Inserts the entire new batch of `StockMarketData`, `StockFairValues`, `StockFairValueMethods`, and `StockSupportResistance`.
     - Calls `SaveChangesAsync()` and `CommitAsync()`.
     - **Verification**: If scraping fails or an exception occurs during the batch commit, the transaction rolls back, leaving old live data 100% untouched.

---

## 5. Full API Surface — As Implemented

| Route | Method | Action Signature & Handler | Description |
|---|---|---|---|
| `/api/stocks` | `GET` | `StocksController.GetAll(...)` -> `GetStocksListQueryHandler` | Returns SQL-paginated list of stock summaries (`ticker, nameAr, nameEn, indices, shariahStatus, closingPrice, changePct, fairValue, priceComparison, fairValueDiffPct`). Supports `page`, `pageSize`, `sortBy`, `sortDir`, `search`, `indexCode`, `sectorId`, `shariahStatus`, `priceComparison`. |
| `/api/stocks/{ticker}/market-data` | `GET` | `StocksController.GetMarketData(...)` -> `GetMarketDataQueryHandler` | Returns full market data for a stock, including `sectorNameAr/En`, constituent index memberships with weights, Shariah status & %, full valuation breakdown and individual method outlier flags. |
| `/api/stocks/{ticker}/support-resistance` | `GET` | `StocksController.GetSupportResistance(...)` -> `GetSupportResistanceQueryHandler` | Returns latest support and resistance technical levels (`lastPrice, changePct, pivot, r1, r2, s1, s2, fetchedAt`). |
| `/api/indices` | `GET` | `IndicesController.GetAll(...)` -> `GetIndicesListQueryHandler` | Returns all 8 EGX indices with summary constituent counts and `lastUpdated` timestamps. |
| `/api/indices/{indexCode}/constituents` | `GET` | `IndicesController.GetConstituents(...)` -> `GetIndexConstituentsQueryHandler` | Returns SQL-paginated constituents of an index with index metadata, weight, market data, and fair value. Default sorted by `weight DESC`. |
| `/api/indices/{indexCode}/upload` | `POST` | `IndicesController.UploadIndexFile(...)` -> `UploadIndexFileCommandHandler` | Accepts `multipart/form-data` Excel spreadsheet to upsert constituents, stocks, sectors, and log upload history. |
| `/api/shariah` | `GET` | `ShariahController.GetAll(...)` -> `GetAllShariahComplianceQueryHandler` | Returns SQL-paginated Shariah compliance overview list with `page`, `pageSize`, `sortBy`, `sortDir`, `search`, and `status` filters. |
| `/api/shariah/{symbolCode}` | `GET` | `ShariahController.GetBySymbol(...)` -> `GetShariahComplianceBySymbolQueryHandler` | Returns the single `ShariahCompliance` record for a specific ticker symbol. |
| `/api/shariah/{symbolCode}/full` | `GET` | `ShariahController.GetFull(...)` -> `GetFullStockShariahQueryHandler` | Returns full Shariah profile: `Compliance`, `Metrics` (15 fields), and opinions from all 8 external sources. |
| `/api/shariah/seed` | `POST` | `ShariahController.Seed(...)` -> `SeedShariahMapCommandHandler` | Seeds initial Shariah ratings and missing stocks from payload or local bundled `shariah_map.json`. |
| `/api/shariah/refresh` | `POST` | `ShariahController.Refresh(...)` -> `RefreshShariahDataCommandHandler` | Manually triggers synchronization with external `stocks_merged.json` source. |
| `/api/scraping/run` | `POST` | `ScrapingController.RunScrape(...)` -> `RunCombinedScrapeCommandHandler` | Manually triggers combined scraping engine run. |
| `/api/scraping/status` | `GET` | `ScrapingController.GetStatus(...)` | Returns live scraping run progress if running, or last completed scrape summary from `ScrapeRunLog`. |

---

## 6. Scheduling — As Implemented

### Background Services & Registration
Registered in `Meezan.Infrastructure/DependencyInjection.cs`:
1. `services.AddHostedService<ScrapingBackgroundService>()`
2. `services.AddHostedService<ShariahRefreshBackgroundService>()`

### Scraping Timer & Timezone Logic (`ScrapingBackgroundService.cs`)
- Targets **16:00 (4:00 PM) Cairo time** every day.
- Uses `TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time")`.
- Converts current UTC to Cairo local time, determines if 16:00 today has passed, computes target next run date (today or tomorrow), converts back to UTC, and performs `Task.Delay(delay, stoppingToken)`.
- **Underlying Call**: Inside a scoped `IServiceProvider`, it dispatches `new RunCombinedScrapeCommand(TriggeredBy: "Scheduled")` via MediatR `ISender`.
- **Divergence check**: The scheduled service and `POST /api/scraping/run` **execute the exact same MediatR handler** (`RunCombinedScrapeCommandHandler`). The only difference is the `TriggeredBy` string flag (`"Scheduled"` vs `"Manual"`).

### Shariah Refresh Background Service (`ShariahRefreshBackgroundService.cs`)
- Runs on a **90-day periodic interval** (`TimeSpan.FromDays(90)`).
- Waits in 1-hour delay chunks to prevent 32-bit integer millisecond overflow.
- Calls `new RefreshShariahDataCommand(TriggeredBy: "Scheduled")` through MediatR `ISender` (identical to manual `POST /api/shariah/refresh`).

---

## 7. Gaps and Deviations

1. **No EF Core Migrations Directory**:
   - The database schema is configured via Fluent API configurations in `Meezan.Infrastructure.Persistence.Configurations`, but there is no `Migrations/` folder in the repository. The cloud database was initialized and managed via direct DDL scripts or `EnsureCreated`, rather than tracked EF Core migration history.
2. **External Shariah Data Source Client**:
   - `ShariahSourceClient.FetchMergedStocksAsync` currently looks for `stocks_merged.json` on local/relative paths or through an HTTP client URL configured in settings. In standard local development, it relies on the bundled JSON resource rather than an automated live authenticated scraper targeting the external portal.
3. **Scrape Execution Blocking**:
   - `MubasherScraperClient` runs sequentially with rate-limiting pauses over 270+ stocks, taking several minutes. While `GET /api/scraping/status` provides live polling, calling `POST /api/scraping/run` holds the HTTP request open until completion rather than immediately returning an `Accepted (202)` background task ID.
4. **`PriceComparison` Enum Inconsistency in Python vs DB**:
   - In Python scripts, price comparison tags were Arabic strings (`أكبر`, `أصغر`, `تقريبًا قدها`). In the backend, these are normalized to clean English domain enums (`Cheap`, `Expensive`, `Fair`) and exposed as such in API DTOs.
