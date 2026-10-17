using MediatR;
using Meezan.Application.Common.Interfaces.Repositories.Stocks;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Sectors.DTOs;

namespace Meezan.Application.Features.Sectors.Queries.GetSectorsList;

public class GetSectorsListQueryHandler
    : IRequestHandler<GetSectorsListQuery, Result<List<SectorSummaryDto>>>
{
    private readonly IStockRepository _stockRepo;

    public GetSectorsListQueryHandler(IStockRepository stockRepo)
    {
        _stockRepo = stockRepo;
    }

    public async Task<Result<List<SectorSummaryDto>>> Handle(
        GetSectorsListQuery request,
        CancellationToken cancellationToken)
    {
        return await _stockRepo.GetSectorsWithCountAsync(cancellationToken);
    }
}
