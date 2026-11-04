namespace Meezan.Domain.Entities;

/// <summary>
/// Daily sector performance calculation results keyed by (SectorId, TradingDate).
/// </summary>
public class SectorDaily
{
    public int Id { get; set; }
    public int SectorId { get; set; }
    public DateOnly TradingDate { get; set; }

    public decimal? ChangePct { get; set; }
    public decimal TotalMarketCap { get; set; } // EGP
    public int Advancers { get; set; }
    public int Decliners { get; set; }
    public int Unchanged { get; set; }
    public int StocksUsed { get; set; }
    public int StocksTotal { get; set; }
    public decimal CoveragePct { get; set; } // e.g. 85.5%
    public bool Approximate { get; set; }
    public decimal ShareOfWeightRose { get; set; } // e.g. % of covered weight that rose

    /// <summary>
    /// Top 3 contributors JSON: [{ stock: "COMI", weight: 35.0, change: 1.5, contribution: 0.525 }, ...]
    /// </summary>
    public string TopContributorsJson { get; set; } = "[]";

    public string MethodUsed { get; set; } = "marketCap"; // marketCap | displayedWeight
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;

    public Sector? Sector { get; set; }
}
