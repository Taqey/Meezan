namespace Meezan.Application.Features.Market.DTOs;

public record RefreshMarketSnapshotsResultDto(
    int Succeeded,
    int Failed,
    int TotalTargets,
    TimeSpan Duration);
