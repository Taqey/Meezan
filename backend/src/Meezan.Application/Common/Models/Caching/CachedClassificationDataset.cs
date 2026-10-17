using Meezan.Application.Features.Funds.Classification.Dtos;
using Meezan.Application.Features.Funds.Listing.Dtos;
using Meezan.Application.Features.Funds.Types.Dtos;

namespace Meezan.Application.Common.Models.Caching;

public sealed class CachedClassificationDataset
{
    public List<FundClassificationTreeDto> Tree { get; set; } = new();
    public Dictionary<int, EffectiveClassificationDto> EffectiveByFundId { get; set; } = new();
    public Dictionary<int, HashSet<int>> NodeDescendantFundIds { get; set; } = new();
    public Dictionary<string, int> SlugToNodeId { get; set; } = new();
    public List<FundTypeOptionDto> TypeOptions { get; set; } = new();
    public FundStatsDto Stats { get; set; } = new();
    public Dictionary<int, (int TypeId, string Slug)> TypeOverrides { get; set; } = new();
    public Dictionary<string, CachedTypeDefinition> TypeDefsBySlug { get; set; } = new();
}

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
