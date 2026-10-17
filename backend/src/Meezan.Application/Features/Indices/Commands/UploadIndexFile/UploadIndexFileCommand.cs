using MediatR;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.Indices.Commands.UploadIndexFile;

public record UploadIndexFileCommand(
    string IndexCode,
    Stream FileStream,
    string FileName,
    string? UploadedBy = null
) : IRequest<Result<UploadIndexFileResultDto>>;
