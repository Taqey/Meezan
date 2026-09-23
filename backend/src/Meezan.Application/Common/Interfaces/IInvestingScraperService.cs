namespace Meezan.Application.Common.Interfaces;

/// <summary>
/// Fallback scraper for investing.com — used ONLY to fill in individual fields
/// that Mubasher returned null for. Never called when Mubasher data is complete.
/// </summary>
public interface IInvestingScraperService
{
    /// <summary>
    /// Attempts to fill any null fields in <paramref name="primary"/> by fetching
    /// the stock page on investing.com. Returns a new record with missing fields
    /// populated; fields that Mubasher already returned are left unchanged.
    /// Returns <paramref name="primary"/> unchanged (not null) if investing.com
    /// either wasn't needed or failed to parse a field.
    /// </summary>
    Task<ScrapedMarketData> FillMissingFieldsAsync(
        ScrapedMarketData primary,
        CancellationToken ct = default);
}
