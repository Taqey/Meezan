namespace Meezan.Domain.Entities;

/// <summary>
/// Per-fund manual type assignment. The sync writes <see cref="Fund.Type"/> from
/// SNDUK on every run but NEVER touches this table, so an operator decision wins.
/// </summary>
public class FundTypeOverride
{
    public int Id { get; set; }
    public int FundId { get; set; }
    public int FundTypeDefinitionId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;

    public Fund? Fund { get; set; }
    public FundTypeDefinition? Definition { get; set; }
}
