using System.Text.Json.Serialization;

namespace Meezan.Application.Common.Models.Funds;

public sealed class SndukPricePointDto
{
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Date { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Price { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Change { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Performance { get; set; }
}
