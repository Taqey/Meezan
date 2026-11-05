# Investment Funds Feature

Path: `frontend/src/app/features/funds/`

## Components

- **`funds-list.component`**:
  - Catalog of all licensed Egyptian mutual and investment funds.
  - Multi-level classification filter using the interactive hierarchical tree (`ClassificationCascadeComponent`).
  - Filtering by asset manager, Islamic funds, fund type (Equity, Money Market, Fixed Income, Gold, Balanced, etc.), and AUM.
  - Performance period selection (1M, 3M, 6M, 1Y, 3Y, 5Y, YTD).

- **`fund-detail.component`**:
  - Detailed performance and analytical breakdown of a single fund.
  - Interactive NAV history chart powered by Chart.js.
  - Asset allocation breakdown with percentage bands and asset-class classification.
  - Fee schedule (management fees, subscription/redemption charges).
  - Similar funds comparison based on category and risk rating.

- **`fund-manager-detail.component`**:
  - Institutional profile for Egyptian asset managers (e.g., Beltone, CI Capital, Hermes, CIAM, Azimut).
  - Total AUM managed, aggregate fund count, and complete fund lineup breakdown.
