namespace Meezan.Application.Features.Funds.Details.Queries.GetAdminFundById;

using MediatR;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

public class GetAdminFundByIdQueryHandler : IRequestHandler<GetAdminFundByIdQuery, Result<Fund?>>
{
    private readonly IFundRepository _fundRepo;

    public GetAdminFundByIdQueryHandler(IFundRepository fundRepo)
    {
        _fundRepo = fundRepo;
    }

    public async Task<Result<Fund?>> Handle(GetAdminFundByIdQuery request, CancellationToken cancellationToken)
    {
        return await _fundRepo.GetByIdAsync(request.Id, cancellationToken);
    }
}
