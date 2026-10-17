using MediatR;
using Meezan.Application.Common.Interfaces;
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
