using Meezan.Application.Common.Results;
using MediatR;
using Meezan.Application.Features.Shariah.DTOs;

namespace Meezan.Application.Features.Shariah.Queries.GetSourcePdfsStatus;

public sealed record GetSourcePdfsStatusQuery : IRequest<Result<List<SourcePdfStatusDto>>>;
