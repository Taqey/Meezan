using MediatR;
using Meezan.Application.Common.Interfaces.Caching;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.Funds.Categories.Commands.IncludeFundInCategory;

public sealed record IncludeFundInCategoryCommand(int FundId, int CategoryId) : IRequest<Result>, ITransactionalCommand, IInvalidatesFundCatalog;
