using FluentValidation;

namespace Meezan.Application.Features.Scraping.Queries.GetSupportResistance;

public class GetSupportResistanceQueryValidator : AbstractValidator<GetSupportResistanceQuery>
{
    public GetSupportResistanceQueryValidator()
    {
        RuleFor(x => x.Ticker)
            .NotEmpty().WithMessage("Ticker is required.")
            .MaximumLength(20);
    }
}
