using System.Text.Json.Serialization;

namespace Meezan.Application.Common.Models.Funds;

public sealed class SndukCompetingJson
{
    public SndukFundDto[] Funds { get; set; } = [];
}
