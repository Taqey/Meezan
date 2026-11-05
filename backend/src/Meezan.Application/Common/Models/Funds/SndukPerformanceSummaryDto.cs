using System.Text.Json.Serialization;

namespace Meezan.Application.Common.Models.Funds;

public sealed class SndukPerformanceSummaryDto
{
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? TotalReturn { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Volatility { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? MaxDrawdown { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? SharpeRatio { get; set; }
}
