namespace Meezan.WebApi.Models.Admin;

/// <summary>Response DTO for Shariah manual overrides (feed, override, and effective values).</summary>
public sealed record ShariahOverridesDto(
    string Ticker,
    bool? FeedCoreActivityCompliant,
    string? FeedCategoryAr,
    string? FeedCategoryEn,
    decimal? FeedHaramRevenuePercentage,
    decimal? FeedLoansPercentage,
    bool? OverrideCoreActivityCompliant,
    string? OverrideCategoryAr,
    string? OverrideCategoryEn,
    decimal? OverrideHaramRevenuePercentage,
    decimal? OverrideLoansPercentage,
    bool? EffectiveCoreActivityCompliant,
    string? EffectiveCategoryAr,
    string? EffectiveCategoryEn,
    decimal? EffectiveHaramRevenuePercentage,
    decimal? EffectiveLoansPercentage);
