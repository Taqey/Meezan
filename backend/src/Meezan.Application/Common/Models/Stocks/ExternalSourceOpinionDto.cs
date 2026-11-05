namespace Meezan.Application.Common.Models.Stocks;

public class ExternalSourceOpinionDto
{
    public string? Status { get; set; }
    public decimal? Percentage { get; set; }
    public string? Note { get; set; }
    public string? PdfUrl { get; set; }
    public DateTime? LastUpdated { get; set; }
    public string? ExtraDataJson { get; set; }
}
