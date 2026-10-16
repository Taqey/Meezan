namespace Meezan.Application.Features.Funds.Common;

public sealed class FundTypeOptionDto
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}
