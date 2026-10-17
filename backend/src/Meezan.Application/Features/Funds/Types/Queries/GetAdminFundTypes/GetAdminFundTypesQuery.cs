using Meezan.Application.Common.Results;
using MediatR;

namespace Meezan.Application.Features.Funds.Types.Queries.GetAdminFundTypes;

public sealed record GetAdminFundTypesQuery(bool IncludeDeleted = false) : IRequest<Result<List<AdminFundTypeItemDto>>>;
