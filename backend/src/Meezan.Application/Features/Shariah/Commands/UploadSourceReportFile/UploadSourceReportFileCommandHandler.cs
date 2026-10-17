using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Features.Shariah.DTOs;

namespace Meezan.Application.Features.Shariah.Commands.UploadSourceReportFile;

public sealed class UploadSourceReportFileCommandHandler : IRequestHandler<UploadSourceReportFileCommand, UploadSourceReportFileResultDto>
{
    private readonly IShariahSourcePdfStorage _storage;

    public UploadSourceReportFileCommandHandler(IShariahSourcePdfStorage storage)
    {
        _storage = storage;
    }

    public async Task<UploadSourceReportFileResultDto> Handle(UploadSourceReportFileCommand request, CancellationToken cancellationToken)
    {
        return await _storage.SaveSourceReportFileAsync(
            request.SourceKey,
            request.FileStream,
            request.FileName,
            request.ContentType,
            request.Length,
            request.ReportDate,
            cancellationToken);
    }
}
