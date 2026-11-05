namespace Meezan.Domain.Entities;

/// <summary>
/// Similar/Competing funds from SNDUK.
/// </summary>
public class FundSimilar
{
    public int Id { get; set; }
    public int FundId { get; set; }
    public int SimilarFundId { get; set; } // references Fund.Id
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Fund? Fund { get; set; }
    public Fund? SimilarFund { get; set; }
}
