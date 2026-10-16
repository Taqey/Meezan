using MediatR;
using Meezan.Application.Common.Interfaces;

namespace Meezan.Application.Features.Funds.Queries.GetClassificationChanges;

public sealed class GetClassificationChangesQueryHandler : IRequestHandler<GetClassificationChangesQuery, ClassificationChangesResultDto>
{
    private readonly IFundClassificationChangeLogRepository _repo;

    public GetClassificationChangesQueryHandler(IFundClassificationChangeLogRepository repo)
    {
        _repo = repo;
    }

    public async Task<ClassificationChangesResultDto> Handle(GetClassificationChangesQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repo.QueryPagedAsync(
            request.FundId,
            request.NodeId,
            request.Action,
            request.Page,
            request.PageSize,
            cancellationToken);

        return new ClassificationChangesResultDto
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
        };
    }
}
