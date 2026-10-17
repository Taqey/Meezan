namespace Meezan.Application.Features.Shariah.DTOs;

public sealed record UploadSourceReportFileResultDto(
    bool Success,
    string Message,
    string SourceKey,
    string FileName,
    string StoredAt,
    string? ReportDate,
    string UploadedAt);
