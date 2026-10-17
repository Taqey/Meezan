using MediatR;

namespace Meezan.Application.Features.Funds.Types.Queries.GetAdminFundTypes;

public sealed record GetAdminFundTypesQuery(bool IncludeDeleted = false) : IRequest<List<AdminFundTypeItemDto>>;
