# Equities (Stocks) Feature

Path: `frontend/src/app/features/stocks/`

## Components

- **`stocks-list.component`**:
  - Displays all actively traded EGX companies with sorting, filtering, and instant search.
  - Multi-criteria filter drawer: sector picker, Shariah compliance status filter, valuation signals (`Cheap`, `Fair`, `Expensive`).
  - View switcher: cards view (`StockCardComponent`) and tabular view (`EntityTableComponent`).

- **`stock-detail.component`**:
  - Comprehensive analytical profile for any EGX stock (e.g. `COMI`, `ETEL`, `SWDY`).
  - Sub-sections:
    1. **`mz-details-header-card`**: Live trading quote, day's high/low, volume, 52-week price range, and sector tags. Respects stock currency (e.g., USD for dollar-denominated stocks like `FAITA` and `SAIB`, EGP for local stocks).
    2. **Side-by-side Dual Analytic Grid (`mz-details-two`)**:
       - Rendered side-by-side on tablet and desktop screens (`min-width: 768px: repeat(2, 1fr)`).
       - **`mz-support-resistance-card`**: Classic floor-trader pivot levels (R1-R2, Pivot, S1-S2) scraped from Mubasher with dynamic visual position indicator.
       - **`mz-fair-value-card`**: Graham formula valuation model comparison (`sqrt(22.5 * EPS * BookValue)`), upside/downside percentage, and confidence rating.
    3. **`mz-sharia-authorities-section`**: Multi-board Islamic compliance breakdown (Bourse Halal, Musaffa, Kashif, Halal Invest, Faisal Islamic Bank, Ostoul, Thndr).
    4. **`mz-market-indicators-section`**: P/E ratio, P/B ratio, dividend yield, market cap, and trading aggregates.
    5. **Quantitative Standards Modal**: Interactive modal displaying detailed debt, interest-bearing securities, and non-permissible income ratios according to AAOIFI and S&P standards.

