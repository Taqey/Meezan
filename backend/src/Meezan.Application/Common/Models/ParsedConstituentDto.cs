namespace Meezan.Application.Common.Models;

public class ParsedConstituentDto
{
    public string Ticker { get; set; } = string.Empty;
    public string SymbolCode { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? ReutersCode { get; set; }
    public string? SectorAr { get; set; }
    public string? SectorEn { get; set; }
    public decimal Weight { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public int RowNumber { get; set; }
}
