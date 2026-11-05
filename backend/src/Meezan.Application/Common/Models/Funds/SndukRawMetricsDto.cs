using System.Text.Json.Serialization;

namespace Meezan.Application.Common.Models.Funds;

public sealed class SndukRawMetricsDto
{
    public decimal? Cagr { get; set; }
    public decimal? Alpha { get; set; }
    public decimal? Volatility { get; set; }
    public decimal? Consistency { get; set; }
    public decimal? MaxDrawdown { get; set; }
    public decimal? SharpeRatio { get; set; }
}
