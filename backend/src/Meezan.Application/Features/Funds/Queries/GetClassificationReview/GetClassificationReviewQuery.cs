using MediatR;

namespace Meezan.Application.Features.Funds.Queries.GetClassificationReview;

public sealed record GetClassificationReviewQuery : IRequest<ClassificationReviewDto>;
