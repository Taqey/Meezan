using Meezan.Application.Features.Sectors.DTOs;
using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Interfaces.Repositories.Stocks;

public interface IStockRepository
{
    Task<Stock?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Stock?> GetByIdWithSectorAsync(int id, CancellationToken cancellationToken = default);
    Task<Stock?> GetByTickerAsync(string ticker, CancellationToken cancellationToken = default);

    /// <summary>
    /// Like GetByTickerAsync but also returns deactivated stocks.
    /// Use in the Excel-upload flow to avoid a unique-key violation when a
    /// previously-deactivated ticker is re-imported.
    /// </summary>
    Task<Stock?> GetByTickerIncludingInactiveAsync(string ticker, CancellationToken cancellationToken = default);

    Task<Stock?> GetBySymbolCodeAsync(string symbolCode, CancellationToken cancellationToken = default);
    Task<List<Stock>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Loads all stocks eagerly with MarketData, FairValue (with Methods), SupportResistance, and Sector.</summary>
    Task<List<Stock>> GetAllWithMarketDataAsync(CancellationToken cancellationToken = default);

    /// <summary>Loads all active stocks belonging to a sector with MarketData.</summary>
    Task<List<Stock>> GetStocksBySectorIdAsync(int sectorId, CancellationToken cancellationToken = default);

    Task<Stock?> GetByTickerWithMarketDataAsync(string ticker, CancellationToken cancellationToken = default);
    Task<List<Meezan.Application.Features.Scraping.Services.FairValueCalculator.SectorPeerData>> GetSectorPeerDataAsync(int sectorId, int excludeStockId, CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Like GetOrCreateSectorAsync but also reports whether a new Sector row was added.
    /// Used by the upload handler to count <see cref="UploadIndexFileResultDto.SectorsAdded"/>.
    /// </summary>
    Task<(Sector? Sector, bool IsNew)> GetOrCreateSectorWithFlagAsync(
        string? nameAr, string? nameEn, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns ALL stocks (active + inactive) with only Id, Ticker, and NameAr loaded.
    /// Used by the Excel-upload flow to build an in-memory lookup for Arabic-name matching
    /// without N+1 queries per row.
    /// </summary>
    Task<List<Stock>> GetAllIncludingInactiveAsync(CancellationToken cancellationToken = default);
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

    /// <summary>
    /// True when the stock is currently a constituent of the EGX33 Shariah index
    /// (any historical code spelling). Single cheap EXISTS query for overall-verdict
    /// recomputes that need the membership input without loading the full graph.
    /// </summary>
    Task<bool> IsShariahIndexMemberAsync(int stockId, CancellationToken cancellationToken = default);

    /// <summary>Returns all sectors that have at least one stock, sorted by stock count descending.</summary>
    Task<List<SectorSummaryDto>> GetSectorsWithCountAsync(CancellationToken cancellationToken = default);
}

