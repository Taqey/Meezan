namespace Meezan.Application.Features.Funds.Sync.Dtos;

public sealed class FundPriceHistoryBackfillItemDto
{
    public int FundId { get; set; }
    public string FundCode { get; set; } = string.Empty;
    public long SndukId { get; set; }
    public int MissingCount { get; set; }
    public List<string> MissingDates { get; set; } = [];
}

public sealed class FundPriceHistoryBackfillResultDto
{
    public bool IsDryRun { get; set; }
    public int TotalFundsInspected { get; set; }
    public int FundsWithGaps { get; set; }
    public int TotalMissingPointsFound { get; set; }
    public int TotalPointsInserted { get; set; }
    public List<FundPriceHistoryBackfillItemDto> FundDetails { get; set; } = [];
    public string Message { get; set; } = string.Empty;
}
