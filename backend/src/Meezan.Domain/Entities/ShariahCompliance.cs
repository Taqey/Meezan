using Meezan.Domain.Enums;

namespace Meezan.Domain.Entities;

public class ShariahCompliance
{
    public int Id { get; set; }
    public int StockId { get; set; }
    public ShariahStatus Status { get; set; }
    public decimal? Pct { get; set; }
    public string? Note { get; set; }
    public DateTime LastCheckedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Stock? Stock { get; set; }
}
