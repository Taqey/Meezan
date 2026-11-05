namespace Meezan.Application.Features.Funds.Details.Dtos;

public sealed class FundCategoryMembershipDto
{
    public int CategoryId { get; set; }
    public string CategoryNameAr { get; set; } = string.Empty;
    public string CategoryNameEn { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}
