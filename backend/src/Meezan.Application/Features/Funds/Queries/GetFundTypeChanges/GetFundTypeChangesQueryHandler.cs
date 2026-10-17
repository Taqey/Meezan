using MediatR;
using Meezan.Application.Common.Interfaces;

namespace Meezan.Application.Features.Funds.Queries.GetFundTypeChanges;

public sealed class GetFundTypeChangesQueryHandler : IRequestHandler<GetFundTypeChangesQuery, FundTypeChangesResultDto>
{
    private readonly IFundTypeChangeLogRepository _changeLogRepo;

    public GetFundTypeChangesQueryHandler(IFundTypeChangeLogRepository changeLogRepo)
    {
        _changeLogRepo = changeLogRepo;
    }

    public async Task<FundTypeChangesResultDto> Handle(GetFundTypeChangesQuery request, CancellationToken cancellationToken)
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
