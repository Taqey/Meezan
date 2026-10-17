using MediatR;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Types.Queries.GetFundTypeById;

public sealed class GetFundTypeByIdQueryHandler : IRequestHandler<GetFundTypeByIdQuery, Result<FundTypeDefinition>>
{
    private readonly IFundTypeDefinitionRepository _typeDefRepo;

    public GetFundTypeByIdQueryHandler(IFundTypeDefinitionRepository typeDefRepo)
    {
        _typeDefRepo = typeDefRepo;
    }

    public async Task<Result<FundTypeDefinition>> Handle(GetFundTypeByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _typeDefRepo.GetByIdAsync(request.Id, cancellationToken);
        return entity ?? (Result<FundTypeDefinition>)Error.NotFound("FundTypeDefinitionNotFound", "");
    }
}
