using Meezan.Application.Common.Interfaces.Caching;
using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Funds.Classification.Dtos;

namespace Meezan.Application.Features.Funds.Classification.Commands.BulkAddClassificationFunds;

public sealed record BulkAddClassificationFundsCommand(int NodeId, List<int>? FundIds) : IRequest<Result<BulkAddFundsResultDto>>, ITransactionalCommand, IInvalidatesFundCatalog;
