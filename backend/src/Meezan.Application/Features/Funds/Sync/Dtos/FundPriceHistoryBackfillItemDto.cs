using System.Collections.Generic;

namespace Meezan.Application.Features.Funds.Sync.Dtos;

public sealed class FundPriceHistoryBackfillItemDto
{
    public int FundId { get; set; }
    public string FundCode { get; set; } = string.Empty;
    public long SndukId { get; set; }
    public int MissingCount { get; set; }
    public List<string> MissingDates { get; set; } = [];
}
