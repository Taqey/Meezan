using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.RemovalCandidates.DTOs;

namespace Meezan.Application.Features.RemovalCandidates.Queries.GetRemovalCandidates;

public sealed record GetRemovalCandidatesQuery : IRequest<Result<List<RemovalCandidateDto>>>;
