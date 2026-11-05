using System.Text.Json.Serialization;

namespace Meezan.Application.Common.Models.Funds;

public sealed class SndukPerformanceData
{
    public SndukPerformanceJson Json { get; set; } = new();
}
