using Meezan.Domain.Entities;

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

public sealed record HistoricalDataDiffPointDto(
    string Date,
    decimal MyPrice,
    decimal SourcePrice,
    decimal DiffPct);

public sealed record HistoricalDataPreviewResultDto(
    int TotalFundsInspected,
    int FundsWithGaps,
    int TotalMissingPoints,
    int TotalDifferentPoints,
    List<HistoricalDataPreviewFundItemDto> Funds);

public sealed record HistoricalDataCommitFundResultDto(
    int FundId,
    string FundCode,
    int InsertedCount,
    int OverwrittenCount,
    bool Success,
    string? Error);

public sealed record HistoricalDataCommitResultDto(
    int TotalFundsProcessed,
    int TotalInsertedPoints,
    int TotalOverwrittenPoints,
    List<HistoricalDataCommitFundResultDto> Funds);

public sealed record FundCompareSeriesPointDto(
    string Date,
    decimal? MyPrice,
    decimal? SourcePrice,
    bool IsMissingInDb,
    bool IsDifferent);

public sealed record FundCompareChartDto(
    int FundId,
    string FundCode,
    string FundNameAr,
    string Currency,
    List<FundCompareSeriesPointDto> Points);

public sealed record CheckNewFundsResultDto(
    int TotalSourceFunds,
    int TotalDbFunds,
    int ExistingSkippedCount,
    int NewFundsFoundCount,
    List<string> NewFundNames,
    List<string> Failures);

