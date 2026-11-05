using System.Text.Json.Serialization;

namespace Meezan.Application.Features.Shariah.Commands.ImportFaisalOsoulOpinions;

/// <summary>Root of shariah_opinions_faisal_osoul.json (the "_notes" block is ignored).</summary>
public class FaisalOsoulOpinionsFile
{
    public FaisalOsoulSourceBlock? FaisalBank { get; set; }
    [JsonPropertyName("ostoul")]
    public FaisalOsoulSourceBlock? Ostoul { get; set; }
}
