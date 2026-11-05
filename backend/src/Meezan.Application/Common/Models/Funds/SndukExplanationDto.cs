using System.Text.Json.Serialization;

namespace Meezan.Application.Common.Models.Funds;

public sealed class SndukExplanationDto
{
    public string[]? Ar { get; set; }
    public string[]? En { get; set; }
}
