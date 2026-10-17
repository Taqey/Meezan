namespace Meezan.Application.Features.Funds.Queries.GetAdminFundTypes;

public sealed record AdminFundTypeItemDto(
    int Id,
    string Slug,
    string NameAr,
    string NameEn,
    string Icon,
    string Color,
    int DisplayOrder,
    bool IsVisible,
    bool IsDeleted,
    DateTime? DeletedAt,
    string DeletedReason,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string CreatedBy,
    string UpdatedBy,
    int FundCount);
