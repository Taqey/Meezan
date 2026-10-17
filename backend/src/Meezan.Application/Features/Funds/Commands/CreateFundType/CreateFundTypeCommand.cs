using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Commands.CreateFundType;

public sealed record CreateFundTypeCommand : IRequest<FundTypeDefinition>, ITransactionalCommand
{
    public string NameAr { get; init; } = string.Empty;
    public string NameEn { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string? Icon { get; init; }
    public string? Color { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsVisible { get; init; } = true;
}
