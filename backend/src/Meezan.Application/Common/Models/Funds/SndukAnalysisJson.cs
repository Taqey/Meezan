using System.Text.Json;
using System.Text.Json.Serialization;

namespace Meezan.Application.Common.Models.Funds;

public sealed class SndukAnalysisJson
{
    public SndukScoresDto? Scores { get; set; }
    public SndukRawMetricsDto? RawMetrics { get; set; }
    public JsonElement? Weights { get; set; }
    public SndukExplanationsDto? Explanations { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? CalculatedAt { get; set; }
}
