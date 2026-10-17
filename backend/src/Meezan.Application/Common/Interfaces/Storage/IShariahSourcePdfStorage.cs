using Meezan.Application.Features.Shariah.DTOs;

namespace Meezan.Application.Common.Interfaces.Storage;

public interface IShariahSourcePdfStorage
{
    Task<List<SourcePdfStatusDto>> GetSourcePdfsStatusAsync(CancellationToken cancellationToken = default);
    Task<SourcePdfFileResultDto?> GetSourcePdfStreamAsync(string sourceKey, CancellationToken cancellationToken = default);
    Task<UploadSourceReportFileResultDto> SaveSourceReportFileAsync(
        string sourceKey,
        Stream fileStream,
        string fileName,
        string contentType,
        long length,
        string? reportDate,
        CancellationToken cancellationToken = default);
}
