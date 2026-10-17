using FluentValidation;

namespace Meezan.Application.Features.Scraping.Queries.GetScrapeStatus;

public class GetScrapeStatusQueryValidator : AbstractValidator<GetScrapeStatusQuery>
{
    public GetScrapeStatusQueryValidator()
    {
        RuleFor(x => x.JobKey)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.JobKey));
    }
}
