using FluentValidation;

namespace Meezan.Application.Features.Scraping.Commands.RefreshSelectedStocks;

public class RefreshSelectedStocksCommandValidator : AbstractValidator<RefreshSelectedStocksCommand>
{
    public RefreshSelectedStocksCommandValidator()
    {
        RuleFor(x => x.Tickers)
            .NotNull()
            .Must(x => x.Count > 0).WithMessage("At least one ticker must be provided.");

        RuleFor(x => x.TriggeredBy)
            .NotEmpty()
            .MaximumLength(100);
    }
}
