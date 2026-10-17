using Meezan.Application.Common.Results;
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
using Meezan.Application.Features.RemovalCandidates.DTOs;

namespace Meezan.Application.Features.RemovalCandidates.Queries.GetRemovalCandidates;

public sealed class GetRemovalCandidatesQueryHandler : IRequestHandler<GetRemovalCandidatesQuery, Result<List<RemovalCandidateDto>>>
{
    private readonly IRemovalCandidatesReadQuery _readQuery;

    public GetRemovalCandidatesQueryHandler(IRemovalCandidatesReadQuery readQuery)
    {
        _readQuery = readQuery;
    }

    public async Task<Result<List<RemovalCandidateDto>>> Handle(GetRemovalCandidatesQuery request, CancellationToken cancellationToken)
    {
        return await _readQuery.GetRemovalCandidatesAsync(cancellationToken);
    }
}
