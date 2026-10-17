using MediatR;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.Funds.Types.Queries.GetFundTypeChanges;

public sealed class GetFundTypeChangesQueryHandler : IRequestHandler<GetFundTypeChangesQuery, Result<FundTypeChangesResultDto>>
{
    private readonly IFundTypeChangeLogRepository _changeLogRepo;

    public GetFundTypeChangesQueryHandler(IFundTypeChangeLogRepository changeLogRepo)
    {
        _changeLogRepo = changeLogRepo;
    }

    public async Task<Result<FundTypeChangesResultDto>> Handle(GetFundTypeChangesQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _changeLogRepo.QueryPagedAsync(request.Page, request.PageSize, cancellationToken);
        return new FundTypeChangesResultDto(
            items,
            total,
            request.Page,
            request.PageSize,
            (int)Math.Ceiling(total / (double)request.PageSize)
        );
    }
}
