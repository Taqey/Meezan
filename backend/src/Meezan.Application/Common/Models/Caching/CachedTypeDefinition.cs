namespace Meezan.Application.Common.Models.Caching;

public sealed class CachedTypeDefinition
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; }
    public bool IsDeleted { get; set; }
}
