using MediatR;
using Meezan.Application.Features.RemovalCandidates.DTOs;

namespace Meezan.Application.Features.RemovalCandidates.Commands.ReactivateSelected;

public sealed record ReactivateSelectedCommand(
    List<string>? Tickers) : IRequest<ReactivateSelectedResultDto>;
