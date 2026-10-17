namespace Meezan.Application.Features.Funds.Commands.BulkAddFundsToType;

public sealed record BulkAddFundsToTypeResultDto(int Added, List<int> Failed);
