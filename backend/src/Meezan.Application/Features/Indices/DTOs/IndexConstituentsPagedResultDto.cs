namespace Meezan.Application.Features.Indices.DTOs;

public record IndexConstituentsPagedResultDto(
    string IndexCode,
    string IndexNameAr,
    string IndexNameEn,
    DateTime? LastUpdated,
    List<ConstituentItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);
