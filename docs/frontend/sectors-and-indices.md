# Sectors & Indices Features

Paths:
- `frontend/src/app/features/sectors/`
- `frontend/src/app/features/indices/`

## Sectors Feature

- **`sectors-list.component`**:
  - Overview of all EGX official industry sectors (Banking, Real Estate, Industrial Goods, Basic Resources, Non-Bank Financial Services, Healthcare, etc.).
  - Daily sector index return, trading volume, and company count.
- **`sector-detail.component`**:
  - Deep-dive into a selected sector.
  - Sector summary metrics (total market cap, average P/E, sector return).
  - Complete list of constituent companies with Shariah compliance status badges.

## Indices Feature

- **`indices-list.component`**:
  - Tracking of core Egyptian benchmark indices:
    - **EGX30**: Benchmark market-cap weighted index.
    - **EGX70 EWI**: Equal-weighted small & medium cap index.
    - **EGX100 EWI**: Broad market index.
    - **EGX33 Shariah**: Official EGX Islamic compliant index.
- **`index-detail.component`**:
  - Index daily return, trading value, and high/low ranges.
  - Complete list of index constituent equities with weightings and direct stock links.

