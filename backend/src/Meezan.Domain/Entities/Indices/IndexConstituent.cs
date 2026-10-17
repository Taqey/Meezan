namespace Meezan.Domain.Entities;

public class IndexConstituent
{
    public int Id { get; set; }
    public int IndexId { get; set; }
    public int StockId { get; set; }
    /// <summary>
    /// Null when the source file has no weight column at all (e.g. the weight-less
    /// .xls HTML-table exports) — never forced to 0 in that case.
    /// </summary>
    public decimal? Weight { get; set; }
    public DateTime EffectiveDate { get; set; }

    public Index? Index { get; set; }
    public Stock? Stock { get; set; }
}
