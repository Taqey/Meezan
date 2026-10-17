using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Queries.GetFundTypeById;

public sealed class GetFundTypeByIdQueryHandler : IRequestHandler<GetFundTypeByIdQuery, FundTypeDefinition?>
{
    private readonly IFundTypeDefinitionRepository _typeDefRepo;

    public GetFundTypeByIdQueryHandler(IFundTypeDefinitionRepository typeDefRepo)
    {
        _typeDefRepo = typeDefRepo;
    }

    public Task<FundTypeDefinition?> Handle(GetFundTypeByIdQuery request, CancellationToken cancellationToken)
    {
        return _typeDefRepo.GetByIdAsync(request.Id, cancellationToken);
    }
}
