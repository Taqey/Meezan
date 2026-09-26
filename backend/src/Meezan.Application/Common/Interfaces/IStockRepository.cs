using Meezan.Application.Features.Sectors.Queries.GetSectorsList;
using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Interfaces;

public interface IStockRepository
{
    Task<Stock?> GetByTickerAsync(string ticker, CancellationToken cancellationToken = default);
    Task<Stock?> GetBySymbolCodeAsync(string symbolCode, CancellationToken cancellationToken = default);
    Task<List<Stock>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Loads all stocks eagerly with MarketData, FairValue (with Methods), SupportResistance, and Sector.</summary>
    Task<List<Stock>> GetAllWithMarketDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads one stock with Sector, IndexConstituents→Index, and ShariahCompliance eager-loaded.
    /// Used by the detailed market-data endpoint so sector names, index memberships,
    /// and compliance status are all available in a single query.
    /// </summary>
    Task<Stock?> GetByTickerWithFullDetailsAsync(string ticker, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the paginated stocks list query entirely in SQL via IQueryable projection.
    /// Returns the page of items plus the unfiltered-but-filtered total count.
    /// </summary>
    Task<(List<StockListProjection> Items, int TotalCount)> QueryPagedAsync(
        StockListFilter filter, CancellationToken cancellationToken = default);

    Task<Sector?> GetOrCreateSectorAsync(string? nameAr, string? nameEn, CancellationToken cancellationToken = default);
    Task AddAsync(Stock stock, CancellationToken cancellationToken = default);
    Task UpdateAsync(Stock stock, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deactivates a stock by setting IsActive = false.
    /// The row and ALL child data remain in the database; normal read queries
    /// filter to IsActive = true so the stock simply stops appearing in results.
    /// Use the review/reactivate endpoints to undo this if needed.
    /// </summary>
    Task DeactivateAsync(int stockId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a previously deactivated stock (IsActive = true).
    /// </summary>
    Task ReactivateAsync(int stockId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all stocks where IsActive = false, for operator review.
    /// </summary>
    Task<List<Stock>> GetDeactivatedStocksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the requested tickers WITH MarketData, bypassing the IsActive filter.
    /// Used by the operator-driven selective refresh so deactivated or
    /// flagged stocks can be explicitly re-scraped on demand.
    /// </summary>
    Task<List<Stock>> GetByTickersIncludingInactiveAsync(
        IEnumerable<string> tickers, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores DataStatus = Active for explicitly refreshed stocks.
    /// Single SQL statement — no change tracking of caller-held entities.
    /// </summary>
    Task SetActiveDataStatusAsync(
        IEnumerable<int> stockIds, CancellationToken cancellationToken = default);

    /// <summary>Flushes all pending EF changes to the database.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns all sectors that have at least one stock, sorted by stock count descending.</summary>
    Task<List<SectorSummaryDto>> GetSectorsWithCountAsync(CancellationToken cancellationToken = default);
}

// ── Projection types (no EF entity tracking — pure data carriers) ─────────────

/// <summary>
/// Flat projection returned from QueryPagedAsync.
/// Index codes are pre-joined as a comma-separated string and split by the handler.
/// </summary>
public record StockListProjection(
    string Ticker,
    string? NameAr,
    string? NameEn,
    string? IndexCodes,    // pipe-delimited e.g. "EGX30|Sectoral-Indices" — split by handler
    string? ShariahStatus,
    decimal? ClosingPrice,
    decimal? ChangePct,
    decimal? FairValue,
    string? PriceComparison,
    decimal? FairValueDiffPct,
    string? Currency,        // null means EGP (جنيه مصري)
    string? SectorNameAr = null
);

/// <summary>All optional filter/sort/page params for the stocks list query.</summary>
public record StockListFilter(
    int Page,
    int PageSize,
    string? SortBy,
    string? SortDir,
    string? Search,
    string? IndexCode,
    int? SectorId,
    string? ShariahStatus,
    string? PriceComparison,
    int? MinCompliantSources
);
