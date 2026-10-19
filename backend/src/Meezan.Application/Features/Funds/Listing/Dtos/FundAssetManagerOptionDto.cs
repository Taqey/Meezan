namespace Meezan.Application.Features.Funds.Listing.Dtos;

public sealed class FundAssetManagerOptionDto
{
    public long Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public int FundCount { get; set; }
}
