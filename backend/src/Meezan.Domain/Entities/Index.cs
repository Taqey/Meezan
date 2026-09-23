namespace Meezan.Domain.Entities;

public class Index
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? LastUpdated { get; set; }

    public ICollection<IndexConstituent> Constituents { get; set; } = new List<IndexConstituent>();
    public ICollection<UploadHistory> UploadHistories { get; set; } = new List<UploadHistory>();
}
