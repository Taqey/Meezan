using MediatR;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.Funds.Classification.Queries.GetClassificationReview;

public sealed class GetClassificationReviewQueryHandler : IRequestHandler<GetClassificationReviewQuery, Result<ClassificationReviewDto>>
{
    private readonly IFundClassificationService _classification;

    public GetClassificationReviewQueryHandler(IFundClassificationService classification)
    {
        _classification = classification;
    }

    public async Task<Result<ClassificationReviewDto>> Handle(GetClassificationReviewQuery request, CancellationToken cancellationToken)
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
