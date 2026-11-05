using System.Collections.Generic;

namespace Meezan.Application.Features.Shariah.DTOs;

public class FullStockShariahDto
{
    public ShariahComplianceDto? Compliance { get; set; }
    public StockShariahMetricsDto? Metrics { get; set; }
    public List<ShariahSourceOpinionDto> Opinions { get; set; } = new();
}
