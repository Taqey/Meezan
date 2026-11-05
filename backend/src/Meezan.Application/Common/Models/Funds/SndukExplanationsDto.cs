using System.Text.Json.Serialization;

namespace Meezan.Application.Common.Models.Funds;

public sealed class SndukExplanationsDto
{
    public SndukExplanationDto? Performance { get; set; }
    public SndukExplanationDto? Risk { get; set; }
    public SndukExplanationDto? Cost { get; set; }
    public SndukExplanationDto? Stability { get; set; }
    public SndukExplanationDto? Diversification { get; set; }
}
