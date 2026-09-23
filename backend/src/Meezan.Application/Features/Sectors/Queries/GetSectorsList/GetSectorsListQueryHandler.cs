using MediatR;
using Meezan.Application.Common.Interfaces;

namespace Meezan.Application.Features.Sectors.Queries.GetSectorsList;

public class GetSectorsListQueryHandler
    : IRequestHandler<GetSectorsListQuery, List<SectorSummaryDto>>
{
    private readonly IStockRepository _stockRepo;

    public GetSectorsListQueryHandler(IStockRepository stockRepo)
    {
        _stockRepo = stockRepo;
    }

    public async Task<List<SectorSummaryDto>> Handle(
        GetSectorsListQuery request,
        CancellationToken cancellationToken)
    {
        return await _stockRepo.GetSectorsWithCountAsync(cancellationToken);
    }
}
