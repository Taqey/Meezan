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
using Meezan.Application.Features.Shariah.DTOs;

namespace Meezan.Application.Features.Shariah.Queries.GetSourcePdf;

public sealed class GetSourcePdfQueryHandler : IRequestHandler<GetSourcePdfQuery, SourcePdfFileResultDto?>
{
    private readonly IShariahSourcePdfStorage _storage;

    public GetSourcePdfQueryHandler(IShariahSourcePdfStorage storage)
    {
        _storage = storage;
    }

    public async Task<SourcePdfFileResultDto?> Handle(GetSourcePdfQuery request, CancellationToken cancellationToken)
    {
        return await _storage.GetSourcePdfStreamAsync(request.SourceKey, cancellationToken);
    }
}
