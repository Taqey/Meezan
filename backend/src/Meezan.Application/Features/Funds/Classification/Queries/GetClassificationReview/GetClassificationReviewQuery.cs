using MediatR;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.Funds.Classification.Queries.GetClassificationReview;

public sealed record GetClassificationReviewQuery : IRequest<Result<ClassificationReviewDto>>;
