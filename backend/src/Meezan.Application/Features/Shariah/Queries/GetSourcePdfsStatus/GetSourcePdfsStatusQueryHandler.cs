using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Features.Shariah.DTOs;

namespace Meezan.Application.Features.Shariah.Queries.GetSourcePdfsStatus;

public sealed class GetSourcePdfsStatusQueryHandler : IRequestHandler<GetSourcePdfsStatusQuery, List<SourcePdfStatusDto>>
{
    private readonly IShariahSourcePdfStorage _storage;

    public GetSourcePdfsStatusQueryHandler(IShariahSourcePdfStorage storage)
    {
        _storage = storage;
    }

    public async Task<List<SourcePdfStatusDto>> Handle(GetSourcePdfsStatusQuery request, CancellationToken cancellationToken)
    {
        return await _storage.GetSourcePdfsStatusAsync(cancellationToken);
    }
}
