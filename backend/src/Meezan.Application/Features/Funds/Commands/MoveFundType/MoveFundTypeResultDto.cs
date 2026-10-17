using System.Text.Json.Serialization;

namespace Meezan.Application.Features.Funds.Commands.MoveFundType;

public sealed record MoveFundTypeResultDto(
    string Message,
    int FundId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? TargetTypeId = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? Cleared = null);

