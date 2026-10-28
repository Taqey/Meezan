using MediatR;
using Meezan.Application.Common.Interfaces.Caching;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Details.Commands.UpdateAdminFundDetails;

public sealed record UpdateAdminFundDetailsCommand : IRequest<Result<Fund>>, ITransactionalCommand, IInvalidatesFundCatalog
{
    public int Id { get; init; }
    public string NameAr { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? DescriptionAr { get; init; }
}

