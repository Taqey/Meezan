using System.Text.Json.Serialization;

namespace Meezan.Application.Common.Models.Funds;

public sealed class SndukPerformanceJson
{
    public SndukPricePointDto[] Data { get; set; } = [];
    public SndukPerformanceSummaryDto? Summary { get; set; }
}
