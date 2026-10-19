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
    public List<FundAssetManagerOptionDto> AssetManagers { get; set; } = new();
}
