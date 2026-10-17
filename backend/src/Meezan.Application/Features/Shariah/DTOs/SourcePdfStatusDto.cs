namespace Meezan.Application.Features.Shariah.DTOs;

public sealed record SourcePdfStatusDto(
    string SourceKey,
    string? DbPdfUrl,
    string Status,
    string? FileName,
    long? SizeBytes,
    DateTime? LastModifiedUtc,
    string? ViewPath);
