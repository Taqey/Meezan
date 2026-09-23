namespace Meezan.Domain.Entities;

public class IndexConstituent
{
    public int Id { get; set; }
    public int IndexId { get; set; }
    public int StockId { get; set; }
    public decimal Weight { get; set; }
    public DateTime EffectiveDate { get; set; }

    public Index? Index { get; set; }
    public Stock? Stock { get; set; }
}
