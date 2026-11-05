using System.Text.Json.Serialization;

namespace Meezan.Application.Common.Models.Funds;

public sealed class SndukCompetingData
{
    public SndukCompetingJson Json { get; set; } = new();
}
