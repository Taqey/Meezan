using Meezan.Application.Common.Results;
using MediatR;

namespace Meezan.Application.Features.Funds.Classification.Queries.GetClassificationReview;

public sealed record GetClassificationReviewQuery : IRequest<Result<ClassificationReviewDto>>;
