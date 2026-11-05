using System;
using System.Collections.Generic;

namespace Meezan.Application.Features.Funds.Sync.Dtos;

public sealed class FetchFundMissingPricesResultDto
{
    public int FundId { get; set; }
    public string FundCode { get; set; } = string.Empty;
    public string FundNameAr { get; set; } = string.Empty;
    public long SndukId { get; set; }
    public string TimeframeRequested { get; set; } = string.Empty;
    public int AddedCount { get; set; }
    public List<string> MissingRangesFound { get; set; } = [];
    public List<string> AddedDates { get; set; } = [];
    public List<string> SourceMissingDates { get; set; } = [];
    public bool NothingMissing { get; set; }
    public bool OnCooldown { get; set; }
    public bool AlreadyRunning { get; set; }
    public int CooldownRemainingSeconds { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime RanAtUtc { get; set; }
    public string TriggeredBy { get; set; } = string.Empty;
}
