using Meezan.Domain.Enums;

namespace Meezan.Domain.Entities;

public class StockFairValue
{
    public int Id { get; set; }
    public int StockId { get; set; }
    public decimal? FairValue { get; set; }
    public PriceComparison PriceComparison { get; set; } = PriceComparison.Fair;
    public decimal? FairValueDiff { get; set; }
    public decimal? FairValueDiffPct { get; set; }
    public int MethodsUsedCount { get; set; }
    public int MethodsExcludedCount { get; set; }
    public ValuationConfidence Confidence { get; set; } = ValuationConfidence.Low;
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;

    public Stock? Stock { get; set; }
    public ICollection<StockFairValueMethod> Methods { get; set; } = new List<StockFairValueMethod>();
}
