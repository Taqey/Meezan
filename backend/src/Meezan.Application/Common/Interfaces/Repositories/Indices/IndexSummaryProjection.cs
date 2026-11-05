namespace Meezan.Application.Common.Interfaces.Repositories.Indices;

public record IndexSummaryProjection(
    string Code,
    string NameAr,
    string NameEn,
    string? Description,
    int ConstituentsCount,
    DateTime? LastUpdated
);
