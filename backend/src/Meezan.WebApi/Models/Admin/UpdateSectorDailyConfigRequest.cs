namespace Meezan.WebApi.Models.Admin;

public sealed record UpdateSectorDailyConfigRequest(
    decimal? UsdEgpRate,
    string? WeightingMethod,
    bool? ExternalSectorPullEnabled);
