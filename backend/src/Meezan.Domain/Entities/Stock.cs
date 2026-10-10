using Meezan.Domain.Enums;

namespace Meezan.Domain.Entities;

public class Stock
{
    public int Id { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string? NameEn { get; set; }
    public int? SectorId { get; set; }
    public StockDataStatus DataStatus { get; set; } = StockDataStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Soft-deactivation — never issue a hard DELETE.
    // IsActive = false hides the stock from all normal queries without destroying data.
    // NOTE: Do NOT use HasDefaultValue() in EF config for this column (same reason as DataStatus
    // — it causes ValueGeneratedOnAdd() and spurious concurrency exceptions on UPDATE).
    // The C# initialiser below provides the correct default for new inserts; the DB-level
    // DEFAULT BIT 1 is set directly in the migration SQL.
    public bool IsActive { get; set; } = true;
    public DateTime? DeactivatedAt { get; set; }
    public string? DeactivationReason { get; set; }

    /// <summary>
    /// True when the sector assignment was made (or overridden) manually via the admin UI.
    /// When true, automated Excel uploads will NOT overwrite SectorId — the manual
    /// assignment takes precedence over file-derived data.
    /// NOTE: Do NOT use HasDefaultValue() in EF config for this column.
    /// DB-level DEFAULT (0) is set in the migration SQL; C# initializer provides false for new rows.
    /// </summary>
    public bool IsManual { get; set; } = false;

    public Sector? Sector { get; set; }
    public ICollection<IndexConstituent> IndexConstituents { get; set; } = new List<IndexConstituent>();

    public ShariahCompliance? ShariahCompliance { get; set; }
    public StockShariahMetrics? ShariahMetrics { get; set; }
    public ICollection<ShariahSourceOpinion> ShariahSourceOpinions { get; set; } = new List<ShariahSourceOpinion>();

    // Part 4 — market data & scraping
    public StockMarketData? MarketData { get; set; }
    public StockFairValue? FairValue { get; set; }
    public StockSupportResistance? SupportResistance { get; set; }
}
