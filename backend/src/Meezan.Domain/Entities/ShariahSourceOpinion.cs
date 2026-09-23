using Meezan.Domain.Enums;

namespace Meezan.Domain.Entities;

public class ShariahSourceOpinion
{
    public int Id { get; set; }
    public int StockId { get; set; }
    public ShariahSourceKey SourceKey { get; set; }
    public string? Status { get; set; }
    public decimal? Percentage { get; set; }
    public string? Note { get; set; }
    public string? PdfUrl { get; set; }
    public DateTime? SourceLastUpdated { get; set; }
    public DateTime FetchedAt { get; set; }

    /// <summary>
    /// JSON blob holding source-specific extra fields
    /// (grade/sector/industry/ranking for Musaffa;
    ///  purity/haram_percentage/sector/statement_date for Kashif;
    ///  haram_percentage/avg_market_cap/total_assets/deposits_percentage/loans_percentage/liquid_assets_percentage for Osoul).
    /// </summary>
    public string? ExtraData { get; set; }

    public Stock? Stock { get; set; }
}
