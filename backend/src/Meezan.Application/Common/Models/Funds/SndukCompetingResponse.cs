using System.Text.Json.Serialization;

namespace Meezan.Application.Common.Models.Funds;

public sealed class SndukCompetingResponse
{
    public SndukFundDto[] Funds { get; set; } = [];
    public SndukCompetingResult[]? Result { get; set; }
}
