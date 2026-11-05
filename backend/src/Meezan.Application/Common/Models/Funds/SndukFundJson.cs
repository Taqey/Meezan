using System.Text.Json.Serialization;

namespace Meezan.Application.Common.Models.Funds;

public sealed class SndukFundJson
{
    public SndukFundDto[] Funds { get; set; } = [];
}
