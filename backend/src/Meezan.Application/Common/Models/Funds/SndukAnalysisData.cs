using System.Text.Json.Serialization;

namespace Meezan.Application.Common.Models.Funds;

public sealed class SndukAnalysisData
{
    public SndukAnalysisJson Json { get; set; } = new();
}
