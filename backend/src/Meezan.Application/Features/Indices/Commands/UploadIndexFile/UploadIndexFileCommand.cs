using MediatR;

namespace Meezan.Application.Features.Indices.Commands.UploadIndexFile;

public record UploadIndexFileCommand(
    string IndexCode,
    Stream FileStream,
    string FileName,
    string? UploadedBy = null
) : IRequest<UploadIndexFileResultDto>;
