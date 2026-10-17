using FluentValidation;

namespace Meezan.Application.Features.Scraping.Commands.RunCombinedScrape;

public class RunCombinedScrapeCommandValidator : AbstractValidator<RunCombinedScrapeCommand>
{
    public RunCombinedScrapeCommandValidator()
    {
        RuleFor(x => x.TriggeredBy)
            .NotEmpty()
            .MaximumLength(100);
    }
}
