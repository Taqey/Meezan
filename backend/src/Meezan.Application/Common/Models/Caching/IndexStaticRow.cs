namespace Meezan.Application.Common.Models.Caching;

/// <summary>Static (rarely changing) data of one index constituent. No price or verdict fields.</summary>
public sealed class IndexStaticRow
{
    public int StockId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string? NameEn { get; set; }
    public string? SectorNameAr { get; set; }
    public decimal? Weight { get; set; }
    public List<string> IndexCodes { get; set; } = new();
}
