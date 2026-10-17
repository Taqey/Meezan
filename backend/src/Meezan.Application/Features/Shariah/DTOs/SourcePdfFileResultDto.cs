namespace Meezan.Application.Features.Shariah.DTOs;

public sealed record SourcePdfFileResultDto(
    Stream FileStream,
    string ContentType);
