using MediatR;
using Meezan.Application.Common.Interfaces.Storage;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Shariah.DTOs;

namespace Meezan.Application.Features.Shariah.Queries.GetSourcePdfsStatus;

public sealed class GetSourcePdfsStatusQueryHandler : IRequestHandler<GetSourcePdfsStatusQuery, Result<List<SourcePdfStatusDto>>>
{
    private readonly IShariahSourcePdfStorage _storage;

    public GetSourcePdfsStatusQueryHandler(IShariahSourcePdfStorage storage)
    {
        _storage = storage;
    }

    public async Task<Result<List<SourcePdfStatusDto>>> Handle(GetSourcePdfsStatusQuery request, CancellationToken cancellationToken)
    {
        return await _storage.GetSourcePdfsStatusAsync(cancellationToken);
    }
}
