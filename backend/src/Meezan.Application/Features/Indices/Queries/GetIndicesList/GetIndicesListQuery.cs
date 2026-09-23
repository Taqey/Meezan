using MediatR;
using Meezan.Application.Common.Interfaces;

namespace Meezan.Application.Features.Indices.Queries.GetIndicesList;

public record GetIndicesListQuery : IRequest<List<IndexSummaryDto>>;

public record IndexSummaryDto(
    string Code,
    string NameAr,
    string NameEn,
    string? Description,
    int ConstituentsCount,
    DateTime? LastUpdated
);

public class GetIndicesListQueryHandler : IRequestHandler<GetIndicesListQuery, List<IndexSummaryDto>>
{
    private readonly IIndexRepository _indexRepo;

    public GetIndicesListQueryHandler(IIndexRepository indexRepo)
    {
        _indexRepo = indexRepo;
    }

    public async Task<List<IndexSummaryDto>> Handle(GetIndicesListQuery request, CancellationToken cancellationToken)
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
