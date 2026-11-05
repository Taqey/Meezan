namespace Meezan.WebApi.Models.Admin;

/// <summary>Request body for saving Shariah manual overrides. Null values clear the override.</summary>
public sealed record SaveShariahOverridesRequest(
    bool? CoreActivityCompliantOverride,
    string? CategoryArOverride,
    string? CategoryEnOverride,
    decimal? HaramRevenuePercentageOverride,
    decimal? LoansPercentageOverride);
