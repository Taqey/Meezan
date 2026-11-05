# Stocks, Market Scraping & Valuation Models

The Meezan equities pipeline ingests live EGX trading data, computes quantitative valuation models (Benjamin Graham Number, Sector Median Multiples), and calculates support and resistance pivot levels.

---

## 1. Market Data Scraping Pipeline

Real-time and delayed quote data is scraped from Mubasher via `IStockScraperClient`:

```
Scrape Trigger (Manual or Background Schedule)
    ──▶ Mubasher Scraper Client
    ──▶ Extract EPS, P/E, P/B, Book Value, Market Cap, High, Low, Volume
    ──▶ Compute Sector Peer Medians (Excluding the target stock itself)
    ──▶ Run FairValueCalculator
    ──▶ Compute Technical Support & Resistance Ladder
    ──▶ IScrapeBatchCommitter (Atomic live-table swap in a single SQL transaction)
```

### Atomic Commit Guarantee
To eliminate partial update glitches in active client UI sessions:
- Scraped data is accumulated in-memory into `StockScrapedSnapshot` records.
- `IScrapeBatchCommitter` applies the batch in an atomic database transaction. If any error occurs, the live database state remains unmodified.

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
