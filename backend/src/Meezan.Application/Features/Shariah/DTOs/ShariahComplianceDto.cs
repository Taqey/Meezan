using Meezan.Domain.Enums;

namespace Meezan.Application.Features.Shariah.DTOs;

public class ShariahComplianceDto
{
    public int Id { get; set; }
    public int StockId { get; set; }
    public string SymbolCode { get; set; } = string.Empty;
    public string? StockNameAr { get; set; }
    public string? StockNameEn { get; set; }
    public ShariahStatus Status { get; set; }
    public string StatusText => Status.ToString();
    public decimal? Pct { get; set; }
    public string? Note { get; set; }
    public DateTime LastCheckedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
