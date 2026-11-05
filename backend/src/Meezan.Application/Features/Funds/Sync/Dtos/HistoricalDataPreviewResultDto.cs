namespace Meezan.Application.Features.Funds.Sync.Dtos;

public sealed record HistoricalDataPreviewResultDto(
    int TotalFundsInspected,
    int FundsWithGaps,
    int TotalMissingPoints,
    int TotalDifferentPoints,
    List<HistoricalDataPreviewFundItemDto> Funds);

