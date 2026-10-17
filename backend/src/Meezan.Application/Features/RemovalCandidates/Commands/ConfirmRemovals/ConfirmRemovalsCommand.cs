using MediatR;
using Meezan.Application.Features.RemovalCandidates.DTOs;

namespace Meezan.Application.Features.RemovalCandidates.Commands.ConfirmRemovals;

public sealed record ConfirmRemovalsCommand(
    List<string>? Tickers,
    string? Reason) : IRequest<ConfirmRemovalsResultDto>;
