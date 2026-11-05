using System.Text.Json.Serialization;

namespace Meezan.Application.Common.Models.Funds;

public sealed class SndukPerformanceResponse
{
    public SndukPricePointDto[] Data { get; set; } = [];
    public SndukPerformanceSummaryDto? Summary { get; set; }
    public SndukPerformanceResult[]? Result { get; set; }
}
