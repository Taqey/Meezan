using MediatR;
using Meezan.Application.Common.Interfaces.Repositories.Indices;
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
