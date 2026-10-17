namespace Meezan.Application.Features.RemovalCandidates.DTOs;

public sealed record ConfirmRemovalsResultDto(
    string Message,
    List<string> Confirmed,
    List<string> NotFound);
