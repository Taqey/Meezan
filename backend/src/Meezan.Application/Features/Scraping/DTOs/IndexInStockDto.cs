namespace Meezan.Application.Features.Scraping.DTOs;

public record IndexInStockDto(
    string Code,
    string NameAr,
    string NameEn,
    decimal? Weight,
    bool? IsCapped = null,
    decimal? CappingFactor = null
);
