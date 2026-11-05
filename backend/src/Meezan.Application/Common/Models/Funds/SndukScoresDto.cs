using System.Text.Json.Serialization;

namespace Meezan.Application.Common.Models.Funds;

public sealed class SndukScoresDto
{
    public decimal? Performance { get; set; }
    public decimal? Risk { get; set; }
    public decimal? Cost { get; set; }
    public decimal? Stability { get; set; }
    public decimal? Diversification { get; set; }
    public decimal? Total { get; set; }
}
