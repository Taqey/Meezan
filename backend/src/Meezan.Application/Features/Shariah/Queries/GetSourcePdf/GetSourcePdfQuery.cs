using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Shariah.DTOs;

namespace Meezan.Application.Features.Shariah.Queries.GetSourcePdf;

public sealed record GetSourcePdfQuery(string SourceKey) : IRequest<Result<SourcePdfFileResultDto>>;
