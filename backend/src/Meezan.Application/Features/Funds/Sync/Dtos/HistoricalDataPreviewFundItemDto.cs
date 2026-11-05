namespace Meezan.Application.Features.Funds.Sync.Dtos;

public sealed record HistoricalDataPreviewFundItemDto(
    int FundId,
    string FundCode,
    string FundNameAr,
    long SndukId,
    int SourcePointsCount,
    int MyPointsCount,
    int MatchingCount,
    int MissingCount,
    int DifferentCount,
    List<string> MissingDates,
    List<HistoricalDataDiffPointDto> DifferentPoints);
