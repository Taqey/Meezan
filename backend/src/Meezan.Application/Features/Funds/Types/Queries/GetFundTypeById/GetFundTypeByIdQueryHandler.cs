using MediatR;
using Meezan.Application.Common.Interfaces.Integrations;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Queries.Funds;
using Meezan.Application.Common.Interfaces.Queries.RemovalCandidates;
using Meezan.Application.Common.Interfaces.Queries.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Interfaces.Repositories.Indices;
using Meezan.Application.Common.Interfaces.Repositories.Market;
using Meezan.Application.Common.Interfaces.Repositories.Scraping;
using Meezan.Application.Common.Interfaces.Repositories.Sectors;
using Meezan.Application.Common.Interfaces.Repositories.Shariah;
using Meezan.Application.Common.Interfaces.Repositories.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.System;
using Meezan.Application.Common.Interfaces.Security;
using Meezan.Application.Common.Interfaces.Storage;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Types.Queries.GetFundTypeById;

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
