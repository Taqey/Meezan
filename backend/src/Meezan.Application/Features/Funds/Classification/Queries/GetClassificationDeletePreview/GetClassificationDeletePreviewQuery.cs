using MediatR;

namespace Meezan.Application.Features.Funds.Classification.Queries.GetClassificationDeletePreview;

public sealed record GetClassificationDeletePreviewQuery(int Id) : IRequest<ClassificationDeletePreviewDto?>;
