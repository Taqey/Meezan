namespace Meezan.WebApi.Contracts.AdminFundTypes;

public sealed class UpdateFundTypeRequest
{
    public string? NameAr { get; set; }
    public string? NameEn { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public int? DisplayOrder { get; set; }
    public bool? IsVisible { get; set; }
}
