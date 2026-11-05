using System.Text.Json.Serialization;

namespace Meezan.Application.Common.Models.Funds;

public sealed class SndukFundListResponse
{
    public SndukFundDto[] Funds { get; set; } = [];
    public SndukFundResult[]? Result { get; set; }
}
