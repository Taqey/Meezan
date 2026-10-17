namespace Meezan.Application.Features.Funds.Types.Commands.BulkAddFundsToType;

public sealed record BulkAddFundsToTypeResultDto(int Added, List<int> Failed);
