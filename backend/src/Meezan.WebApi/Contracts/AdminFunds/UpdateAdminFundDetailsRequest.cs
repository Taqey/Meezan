namespace Meezan.WebApi.Contracts.AdminFunds;

public sealed class UpdateAdminFundDetailsRequest
{
    public string NameAr { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? DescriptionAr { get; set; }
}

