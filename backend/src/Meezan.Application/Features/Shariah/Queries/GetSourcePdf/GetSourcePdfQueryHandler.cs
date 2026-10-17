using MediatR;
using Meezan.Application.Common.Interfaces.Storage;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Shariah.DTOs;

namespace Meezan.Application.Features.Shariah.Queries.GetSourcePdf;

public sealed class GetSourcePdfQueryHandler : IRequestHandler<GetSourcePdfQuery, Result<SourcePdfFileResultDto>>
{
    private readonly IShariahSourcePdfStorage _storage;

    public GetSourcePdfQueryHandler(IShariahSourcePdfStorage storage)
    {
        _storage = storage;
    }

    public async Task<Result<SourcePdfFileResultDto>> Handle(GetSourcePdfQuery request, CancellationToken cancellationToken)
    {
        var res = await _storage.GetSourcePdfStreamAsync(request.SourceKey, cancellationToken);
        if (res == null) return Error.NotFound("SourcePdfNotFound", "");
        return res;
    }
}
