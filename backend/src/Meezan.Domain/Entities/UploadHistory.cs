namespace Meezan.Domain.Entities;

public class UploadHistory
{
    public int Id { get; set; }
    public int IndexId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public int RowsAffected { get; set; }
    public string Status { get; set; } = "Success"; // Success / Failed
    public string? ErrorMessage { get; set; }
    public string? UploadedBy { get; set; }

    public Index? Index { get; set; }
}
