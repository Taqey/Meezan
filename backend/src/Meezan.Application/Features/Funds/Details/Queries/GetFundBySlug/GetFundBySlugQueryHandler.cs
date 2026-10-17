using MediatR;
using Meezan.Application.Common.Interfaces.Integrations;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Queries.Funds;
using Meezan.Application.Common.Interfaces.Queries.RemovalCandidates;
using Meezan.Application.Common.Interfaces.Queries.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Interfaces.Repositories.Indices;
using Meezan.Application.Common.Interfaces.Repositories.Market;
using Meezan.Application.Common.Interfaces.Repositories.Scraping;
using Meezan.Application.Common.Interfaces.Repositories.Sectors;
using Meezan.Application.Common.Interfaces.Repositories.Shariah;
using Meezan.Application.Common.Interfaces.Repositories.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.System;
using Meezan.Application.Common.Interfaces.Security;
using Meezan.Application.Common.Interfaces.Storage;
using Meezan.Application.Features.Funds.Categories.Dtos;
using Meezan.Application.Features.Funds.Classification.Dtos;
using Meezan.Application.Features.Funds.Details;
using Meezan.Application.Features.Funds.Details.Dtos;
using Meezan.Application.Features.Funds.Listing.Dtos;
using Meezan.Application.Features.Funds.Types.Dtos;
using Meezan.Application.Features.Funds.Details.Queries.GetFundById;

namespace Meezan.Application.Features.Funds.Details.Queries.GetFundBySlug;

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
