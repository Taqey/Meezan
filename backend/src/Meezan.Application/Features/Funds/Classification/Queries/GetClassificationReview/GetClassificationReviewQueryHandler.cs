using MediatR;
using Meezan.Application.Common.Interfaces.Integrations;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Queries.Funds;
using Meezan.Application.Common.Interfaces.Queries.RemovalCandidates;
using Meezan.Application.Common.Interfaces.Queries.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Interfaces.Repositories.Indices;
using Meezan.Application.Common.Interfaces.Repositories.Market;
using Meezan.Application.Common.Interfaces.Repositories.Scraping;
using Meezan.Application.Common.Interfaces.Repositories.Sectors;
using Meezan.Application.Common.Interfaces.Repositories.Shariah;
using Meezan.Application.Common.Interfaces.Repositories.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.System;
using Meezan.Application.Common.Interfaces.Security;
using Meezan.Application.Common.Interfaces.Storage;

namespace Meezan.Application.Features.Funds.Classification.Queries.GetClassificationReview;

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
