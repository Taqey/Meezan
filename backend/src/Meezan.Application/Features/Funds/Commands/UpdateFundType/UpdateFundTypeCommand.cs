using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Commands.UpdateFundType;

public sealed record UpdateFundTypeCommand : IRequest<FundTypeDefinition>, ITransactionalCommand
{
    public int Id { get; init; }
    public string? NameAr { get; init; }
    public string? NameEn { get; init; }
    public string? Icon { get; init; }
    public string? Color { get; init; }
    public int? DisplayOrder { get; init; }
    public bool? IsVisible { get; init; }
}
