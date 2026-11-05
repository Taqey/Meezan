# Admin API, Controls & Operations

The Meezan administration API powers the management dashboard, automated scraping routines, manual market data overrides, taxonomy curation, and audit logging.

---

## 1. Security & Authentication

All admin endpoints reside under the `/api/admin/` route prefix and require JWT Bearer authorization (`[Authorize]` attribute):
- Authentication tokens are issued via `AuthController` validating administrator credentials.
- Authorization headers must provide `Bearer <token>`.

---

## 2. Admin Controller Endpoints

### Market Data Management (`AdminMarketDataController`)
- `POST /api/admin/market-data/manual-update`: Allows manual correction of EPS, P/E, P/B, Book Value, or Support/Resistance levels for specific tickers.
- `POST /api/admin/market-data/refresh-selected`: Triggers real-time re-scraping and fair value recomputation for specified stock symbols.
- `POST /api/admin/market-data/update-data-status`: Toggles active status and tracking for individual equities.
- `POST /api/admin/market-data/save-shariah-overrides`: Saves administrator overrides for Shariah compliance metrics and core activity flags.

### Sector Daily Operations (`AdminSectorDailyController`)
- `GET /api/admin/sector-daily/status`: Returns current calculation status, USD/EGP exchange rate, and weighting methodology.
- `POST /api/admin/sector-daily/config`: Configures USD exchange rate, weighting mechanism (Market Cap vs Equal), and automated pull flags.
- `POST /api/admin/sector-daily/recalculate`: Triggers immediate recomputation of daily sector index movements and top contributors.
- `POST /api/admin/sector-daily/recompute-range`: Backfills sector performance across a specified historical date range.

### Mutual Funds Admin (`AdminFundsController`, `FundClassificationController`, `FundTypesController`)
- `POST /api/admin/funds/sync/run`: Triggers immediate mutual funds synchronization with SNDUK.
- `POST /api/admin/funds/sync/backfill`: Executes historical NAV price backfill.
- `POST /api/admin/funds/classification/nodes`: Creates or updates taxonomy nodes in the hierarchical classification tree.
- `POST /api/admin/funds/classification/override`: Sets manual include/exclude overrides on funds taxonomy.
- `GET /api/admin/funds/classification/review`: Audits conflicting or unclassified mutual funds.

---

## 3. Background Services & Progress Tracking

Long-running jobs (scraping runs, price backfills) run asynchronously in the background:
- **`IScrapeProgressTracker`**: Thread-safe in-memory singleton reporting current status, progress percentage, errors, and elapsed time for equities scrapers.
- **`IFundSyncProgressTracker`**: Progress monitor for mutual fund synchronization batches.
- Progress snapshots are polled by the admin frontend via status endpoints (`/api/admin/scrape/progress`, `/api/admin/funds/sync/progress`).
