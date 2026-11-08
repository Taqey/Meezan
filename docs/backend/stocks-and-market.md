# Stocks, Market Scraping & Valuation Models

The Meezan equities pipeline ingests live EGX trading data, computes quantitative valuation models (Benjamin Graham Number, Sector Median Multiples), and calculates support and resistance pivot levels.

---

## 1. Market Data Scraping Pipeline

Real-time and delayed quote data is ingested via `IStockScraperClient` using a multi-source architecture:

```
Scrape Trigger (Manual or Background Schedule)
    ──▶ Starta Scraper Client (Primary for official market quotes & daily percentage changes)
    ──▶ Mubasher Scraper Client (Dedicated for Technical Support & Resistance Ladder and specific fallbacks like NAPR)
    ──▶ Extract EPS, P/E, P/B, Book Value, Market Cap, High, Low, Volume
    ──▶ Flat-Trading Rule Guard: If price & change % are unchanged from previous day session, daily change is locked to 0% (avoiding stale OTC session artifacts)
    ──▶ Compute Sector Peer Medians (Excluding the target stock itself)
    ──▶ Run FairValueCalculator
    ──▶ Compute Technical Support & Resistance Ladder (Pivot, R1, R2, S1, S2 from Mubasher)
    ──▶ Synchronize Sector Daily Aggregations (RecalculateSectorsAsync)
    ──▶ IScrapeBatchCommitter (Atomic live-table swap in a single SQL transaction)
```

### Scraping Client Roles
- **`StartaScraperClient`**: Primary client for Egyptian Exchange live data table. Parses quotes, closing prices, and official percentage changes. Supports automated mapping of Arabic company names and tickers.
- **`MubasherScraperClient`**: Specialized scraper for technical analysis levels (Classic Floor Trader Pivot, Resistance R1/R2, Support S1/S2). Also acts as the designated source for equities not listed on secondary indices (e.g., `NAPR`).
- **Hybrid Support/Resistance Pipeline**: In `StartaScraperClient.ScrapeSupportResistanceAsync`, official price and change percentages are sourced from Starta, while technical pivot and support/resistance values are pulled directly from Mubasher.

### Atomic Commit Guarantee
To eliminate partial update glitches in active client UI sessions:
- Scraped data is accumulated in-memory into `StockScrapedSnapshot` records.
- `IScrapeBatchCommitter` applies the batch in an atomic database transaction. If any error occurs, the live database state remains unmodified.
- Error summaries in batch logs are safely truncated to fit column boundaries ($\le 4000$ chars) to prevent SQL exceptions.

---

## 2. Fair Value Calculation Heuristics

Valuation logic is encapsulated in the pure static service `FairValueCalculator`:

1. **Graham Number**:
   $$\text{Graham} = \sqrt{22.5 \times \text{EPS} \times \text{BookValuePerShare}}$$
   - Computed only when both EPS and Book Value are strictly positive.
2. **Sector Median P/E × EPS**:
   - Computes median P/E from peer stocks within the same sector (filtering out outliers and non-positive values).
   - Multiplies the peer median by the stock's current EPS.
3. **Sector Median P/B × Book Value**:
   - Computes median P/B across sector peers and multiplies by the stock's Book Value per share.
4. **Direct Book Value**:
   - Assesses the stock's tangible net asset value per share.

### Outlier Detection & Blended Fair Value
- Uses **Tukey fences** ($IQR \times 1.5$) to exclude outlier estimates.
- The surviving estimates are averaged to produce the final `FairValue`.
- Compares current market price with fair value ($2\%$ tolerance band) to assign `PriceComparison`:
  - **`Cheap`**: Price $< \text{FairValue} \times (1 - 0.02)$
  - **`Fair`**: Within $\pm 2\%$ of fair value
  - **`Expensive`**: Price $> \text{FairValue} \times (1 + 0.02)$

---

## 3. Sector Weights & Daily Aggregations

Sector metrics are calculated dynamically using `SectorWeightCalculator` and `SectorDailyService`:
- Aggregates market caps and volumes across all constituent equities in a sector.
- Evaluates Top Contributors (stocks driving daily sector index movement based on weight and percentage change).
- Supports dual weighting mechanisms: Free-Float Market Cap or Equal Weight.
