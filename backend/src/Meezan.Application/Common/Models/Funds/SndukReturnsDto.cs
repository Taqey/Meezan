using System.Text.Json.Serialization;

namespace Meezan.Application.Common.Models.Funds;

public sealed class SndukReturnsDto
{
    [JsonPropertyName("1M")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? M1 { get; set; }
    [JsonPropertyName("3M")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? M3 { get; set; }
    [JsonPropertyName("6M")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? M6 { get; set; }
    [JsonPropertyName("1Y")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Y1 { get; set; }
    [JsonPropertyName("3Y")]
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Y3 { get; set; }
}
