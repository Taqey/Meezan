using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Models.Funds;

public sealed class FundDetailBundle
{
    public Fund? Fund { get; set; }
    public List<FundPriceHistory> PriceHistory { get; set; } = new();
    public FundAnalysis? Analysis { get; set; }
    public List<FundSimilar> SimilarFunds { get; set; } = new();
    public List<FundCategoryOverride> Overrides { get; set; } = new();
    public List<FundCategory> Categories { get; set; } = new();
    public List<FundTypeDefinition> TypeDefinitions { get; set; } = new();
    public List<FundTypeOverride> TypeOverrides { get; set; } = new();
}
