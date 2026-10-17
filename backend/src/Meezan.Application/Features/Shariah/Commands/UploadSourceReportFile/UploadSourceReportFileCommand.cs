using Meezan.Application.Common.Results;
using MediatR;
using Meezan.Application.Features.Shariah.DTOs;

namespace Meezan.Application.Features.Shariah.Commands.UploadSourceReportFile;

public sealed record UploadSourceReportFileCommand(
    string SourceKey,
    Stream FileStream,
    string FileName,
    string ContentType,
    long Length,
    string? ReportDate) : IRequest<Result<UploadSourceReportFileResultDto>>;
