using MediatR;

namespace Meezan.Application.Features.Funds.Queries.GetClassificationDeletePreview;

public sealed record GetClassificationDeletePreviewQuery(int Id) : IRequest<ClassificationDeletePreviewDto?>;
