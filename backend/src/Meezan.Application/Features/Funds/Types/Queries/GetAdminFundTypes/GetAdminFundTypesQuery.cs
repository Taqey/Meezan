using MediatR;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.Funds.Types.Queries.GetAdminFundTypes;

public sealed record GetAdminFundTypesQuery(bool IncludeDeleted = false) : IRequest<Result<List<AdminFundTypeItemDto>>>;
