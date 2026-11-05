using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Meezan.Application.Features.Shariah.Commands.ImportFaisalOsoulOpinions;

public class FaisalOsoulSourceBlock
{
    [JsonPropertyName("report_date")]
    public string? ReportDate { get; set; }

    [JsonPropertyName("compliant")]
    public List<string> Compliant { get; set; } = new();

    [JsonPropertyName("non_compliant")]
    public List<string> NonCompliant { get; set; } = new();

    [JsonPropertyName("has_shariah_board")]
    public List<string> HasShariahBoard { get; set; } = new();
}
