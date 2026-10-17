using MediatR;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Classification.Queries.GetClassificationChanges;

public sealed record GetClassificationChangesQuery(
    int? FundId = null,
    int? NodeId = null,
    ClassificationChangeAction? Action = null,
    int Page = 1,
    int PageSize = 50) : IRequest<ClassificationChangesResultDto>;
