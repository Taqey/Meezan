using Meezan.Domain.Enums;

namespace Meezan.Domain.Entities;

public class StockFairValue
{
    public int Id { get; set; }
    public int StockId { get; set; }
    public decimal? FairValue { get; set; }

    /// <summary>
    /// Unavailable (not Fair) is the safe default: a fair-value row whose comparison was
    /// never computed must never claim a Cheap/Expensive/Fair verdict.
    /// </summary>
    public PriceComparison PriceComparison { get; set; } = PriceComparison.Unavailable;
    public decimal? FairValueDiff { get; set; }
    public decimal? FairValueDiffPct { get; set; }
    public int MethodsUsedCount { get; set; }
    public int MethodsExcludedCount { get; set; }
    public ValuationConfidence Confidence { get; set; } = ValuationConfidence.Low;
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;

    public Stock? Stock { get; set; }
    public ICollection<StockFairValueMethod> Methods { get; set; } = new List<StockFairValueMethod>();
}
