namespace Meezan.Application.Features.RemovalCandidates.DTOs;

public sealed record ReactivateSelectedResultDto(
    string Message,
    List<string> Reactivated,
    List<string> NotFound);
