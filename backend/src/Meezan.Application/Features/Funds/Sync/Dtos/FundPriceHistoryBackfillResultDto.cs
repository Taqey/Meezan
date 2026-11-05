namespace Meezan.Application.Features.Funds.Sync.Dtos;

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



