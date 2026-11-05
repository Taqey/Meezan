namespace Meezan.Application.Features.Indices.Commands.UploadIndexFile;

public class SkippedRowDetail
{
    public int RowNumber { get; set; }
    public string Identifier { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
