using MediatR;
using Meezan.Application.Common.Interfaces;

namespace Meezan.Application.Features.Funds.Queries.GetClassificationReview;

public sealed class GetClassificationReviewQueryHandler : IRequestHandler<GetClassificationReviewQuery, ClassificationReviewDto>
{
    private readonly IFundClassificationService _classification;

    public GetClassificationReviewQueryHandler(IFundClassificationService classification)
    {
        _classification = classification;
    }

    public async Task<ClassificationReviewDto> Handle(GetClassificationReviewQuery request, CancellationToken cancellationToken)
    {
        var q = await _classification.GetReviewQueueAsync(cancellationToken);
        return new ClassificationReviewDto
        {
            UnclassifiedCount = q.UnclassifiedEquity.Count,
            ConflictingCount = q.Conflicting.Count,
            MissingChildCount = q.MissingChild.Count,
            UnclassifiedEquity = q.UnclassifiedEquity,
            Conflicting = q.Conflicting,
            MissingChild = q.MissingChild
        };
    }
}
