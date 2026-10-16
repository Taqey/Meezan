using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Features.Funds.Common;
using Meezan.Application.Features.Funds.Queries.GetFundById;

namespace Meezan.Application.Features.Funds.Queries.GetFundBySlug;

public sealed class GetFundBySlugQueryHandler : IRequestHandler<GetFundBySlugQuery, FundDetailDto?>
{
    private readonly IFundRepository _fundRepo;
    private readonly IMediator _mediator;

    public GetFundBySlugQueryHandler(IFundRepository fundRepo, IMediator mediator)
    {
        _fundRepo = fundRepo;
        _mediator = mediator;
    }

    public async Task<FundDetailDto?> Handle(GetFundBySlugQuery request, CancellationToken cancellationToken)
    {
        var fund = await _fundRepo.GetBySlugAsync(request.Slug, cancellationToken);
        if (fund == null) return null;

        return await _mediator.Send(new GetFundByIdQuery(fund.Id), cancellationToken);
    }
}
