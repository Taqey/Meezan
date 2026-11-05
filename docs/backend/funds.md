# Investment Funds Subsystem & Sync Pipeline

The Meezan funds subsystem tracks mutual funds operating in the Egyptian market, aggregating historical NAV prices, manager details, risk ratings, and multi-level classification taxonomy.

---

## 1. Domain Entities & Taxonomy

```
                       ┌──────────────────────┐
                       │         Fund         │
                       └──────────┬───────────┘
                                  │
         ┌────────────────────────┼────────────────────────┐
         │ 1:1                    │ 1:N                    │ 1:N
┌────────▼────────┐      ┌────────▼─────────┐     ┌────────▼─────────┐
│  FundAnalysis   │      │ FundPriceHistory │     │   FundSimilar    │
└─────────────────┘      └──────────────────┘     └──────────────────┘
```

- **`Fund`**: Core mutual fund entity storing fund code, name (Arabic/English), inception date, currency, asset manager, and current NAV price.
- **`FundAnalysis`**: Precalculated analytical scores (Performance, Risk, Cost, Stability, Diversification), CAGR, Sharpe Ratio, Alpha, Volatility, and Max Drawdown.
- **`FundPriceHistory`**: Daily historical NAV price points.
- **`FundClassificationNode`**: Hierarchical category tree (e.g. Asset Class -> Sector -> Investment Strategy).
- **`FundTypeDefinition`**: Semantic fund types (Equity, Debt, Money Market, Islamic, etc.).

---

## 2. External Integration: SNDUK Pipeline

The system synchronizes with **SNDUK** (the official Egyptian mutual funds portal) via `SndukFundClient`:

```
SndukFundClient 
    ──▶ Fetch Fund List / Metadata
    ──▶ Fetch Historical NAV Points
    ──▶ Parse JSON responses into Strongly-typed Snduk Models
    ──▶ Map into Domain Entities (Fund, FundPriceHistory, FundAnalysis)
    ──▶ Evaluate Auto-Classification Heuristics
    ──▶ Atomic Transaction Commit
```

### Sync Execution Modes
1. **Full Sync**: Iterates over all active funds, updates fund metadata, backfills missing price history, and updates technical performance statistics.
2. **Price-Only Sync**: Light, high-frequency synchronization that touches only the latest day's NAV price without modifying static fund parameters.
3. **Historical Backfill**: Fetches up to 5-10 years of price history points for selected funds.

---

## 3. Override & Audit System

When automated categorization needs human review:
- **`FundCategoryOverride` & `FundClassificationOverride`**: Allows administrators to override the automatic taxonomy assignment (Manual Include / Manual Exclude).
- **`FundCategoryChangeLog` & `FundClassificationChangeLog`**: Records every override modification, timestamp, reason, and administrator identity.
- **`FundClassificationBackup`**: Preserves previous state snapshots prior to bulk tree migration operations.
