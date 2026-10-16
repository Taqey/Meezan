using MediatR;
using Meezan.Application.Features.Funds.Common;

namespace Meezan.Application.Features.Funds.Queries.GetAdminClassificationTree;

public sealed record GetAdminClassificationTreeQuery(bool IncludeDeleted = false) : IRequest<List<AdminClassificationNodeDto>>;
