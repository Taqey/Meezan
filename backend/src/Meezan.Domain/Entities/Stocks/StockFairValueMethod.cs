namespace Meezan.Domain.Entities;

public class StockFairValueMethod
{
    public int Id { get; set; }
    public int StockFairValueId { get; set; }
    /// <summary>Method name, e.g. "EPS×PE", "BookValue×PB", "Graham"</summary>
    public string MethodName { get; set; } = string.Empty;
    public decimal? EstimatedValue { get; set; }
    /// <summary>Whether this method was excluded as an outlier</summary>
    public bool IsOutlier { get; set; }

    public StockFairValue? StockFairValue { get; set; }
}
