namespace Meezan.Application.Features.Indices.DTOs;

public record IndexSummaryDto(
    string Code,
    string NameAr,
    string NameEn,
    string? Description,
    int ConstituentsCount,
    DateTime? LastUpdated
);
