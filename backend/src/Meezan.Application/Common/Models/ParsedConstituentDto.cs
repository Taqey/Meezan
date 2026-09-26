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
    /// <summary>
    /// Null when the file has no weight column (weight-column detection returned
    /// nothing). Never defaulted to 0 — a missing weight column stays missing.
    /// </summary>
    public decimal? Weight { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public int RowNumber { get; set; }
}
