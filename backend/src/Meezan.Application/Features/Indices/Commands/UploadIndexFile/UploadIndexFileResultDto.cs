namespace Meezan.Application.Features.Indices.Commands.UploadIndexFile;

public class UploadIndexFileResultDto
{
    public string IndexCode { get; set; } = string.Empty;
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public int SectorsAdded { get; set; }
    public int TotalConstituents { get; set; }
    public List<SkippedRowDetail> SkippedDetails { get; set; } = new();
    public string Status { get; set; } = "Success";
    public string Message { get; set; } = string.Empty;
    public string? Warning { get; set; }
}
