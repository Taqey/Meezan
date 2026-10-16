namespace Meezan.Application.Features.Funds.Queries.GetAdminFundById;

using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;

public class GetAdminFundByIdQueryHandler : IRequestHandler<GetAdminFundByIdQuery, Fund?>
{
    private readonly IFundRepository _fundRepo;

    public GetAdminFundByIdQueryHandler(IFundRepository fundRepo)
    {
        _fundRepo = fundRepo;
    }

    public async Task<Fund?> Handle(GetAdminFundByIdQuery request, CancellationToken cancellationToken)
    {
        return await _fundRepo.GetByIdAsync(request.Id, cancellationToken);
    }
}
