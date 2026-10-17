using MediatR;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Funds.Details.Dtos;
using Meezan.Application.Features.Funds.Details.Queries.GetFundById;

namespace Meezan.Application.Features.Funds.Details.Queries.GetFundBySlug;

public sealed class GetFundBySlugQueryHandler : IRequestHandler<GetFundBySlugQuery, Result<FundDetailDto>>
{
    private readonly IFundRepository _fundRepo;
    private readonly IMediator _mediator;

    public GetFundBySlugQueryHandler(IFundRepository fundRepo, IMediator mediator)
    {
        _fundRepo = fundRepo;
        _mediator = mediator;
    }

    public async Task<Result<FundDetailDto>> Handle(GetFundBySlugQuery request, CancellationToken cancellationToken)
    {
        var fund = await _fundRepo.GetBySlugAsync(request.Slug, cancellationToken);
        if (fund == null) return Error.NotFound("FundNotFound", "");

        return await _mediator.Send(new GetFundByIdQuery(fund.Id), cancellationToken);
    }
}
