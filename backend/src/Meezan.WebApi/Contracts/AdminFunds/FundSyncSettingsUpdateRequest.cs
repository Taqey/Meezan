namespace Meezan.WebApi.Contracts.AdminFunds;

public sealed class FundSyncSettingsUpdateRequest
{
    public bool? IsEnabled { get; set; }
    public TimeOnly? RunTimeCairo { get; set; }
    public bool? PreventOverlap { get; set; }
    public int? RequestDelaySeconds { get; set; }
    public int? MaxRetries { get; set; }
    public int? RetryBackoffSeconds { get; set; }
    public bool? AutoIndexFundTypeEnabled { get; set; }
    public bool? AutoClassificationEnabled { get; set; }
}
