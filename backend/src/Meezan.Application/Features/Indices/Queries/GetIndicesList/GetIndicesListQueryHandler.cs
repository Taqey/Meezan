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
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Indices.DTOs;

namespace Meezan.Application.Features.Indices.Queries.GetIndicesList;

public class GetIndicesListQueryHandler : IRequestHandler<GetIndicesListQuery, Result<List<IndexSummaryDto>>>
{
    private readonly IIndexRepository _indexRepo;

    public GetIndicesListQueryHandler(IIndexRepository indexRepo)
    {
        _indexRepo = indexRepo;
    }

    public async Task<Result<List<IndexSummaryDto>>> Handle(GetIndicesListQuery request, CancellationToken cancellationToken)
    {
        var summaries = await _indexRepo.GetAllSummariesAsync(cancellationToken);
        return summaries.Select(s => new IndexSummaryDto(
            s.Code,
            s.NameAr,
            s.NameEn,
            s.Description,
            s.ConstituentsCount,
            s.LastUpdated
        )).ToList();
    }
}
