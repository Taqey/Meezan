using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Funds.Classification.Dtos;

namespace Meezan.Application.Features.Funds.Classification.Queries.GetAdminClassificationTree;

public sealed record GetAdminClassificationTreeQuery(bool IncludeDeleted = false) : IRequest<Result<List<AdminClassificationNodeDto>>>;
