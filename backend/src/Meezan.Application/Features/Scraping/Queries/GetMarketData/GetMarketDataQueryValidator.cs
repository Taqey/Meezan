using FluentValidation;

namespace Meezan.Application.Features.Scraping.Queries.GetMarketData;

public class GetMarketDataQueryValidator : AbstractValidator<GetMarketDataQuery>
{
    public GetMarketDataQueryValidator()
    {
        RuleFor(x => x.Ticker)
            .NotEmpty().WithMessage("Ticker is required.")
            .MaximumLength(20);
    }
}
