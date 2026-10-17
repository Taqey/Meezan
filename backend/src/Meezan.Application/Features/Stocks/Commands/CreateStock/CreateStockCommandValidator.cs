using FluentValidation;

namespace Meezan.Application.Features.Stocks.Commands.CreateStock;

public sealed class CreateStockCommandValidator : AbstractValidator<CreateStockCommand>
{
    public CreateStockCommandValidator()
    {
        RuleFor(x => x.Ticker)
            .NotEmpty().WithMessage("Ticker is required.")
            .Must(t => !string.IsNullOrWhiteSpace(t)).WithMessage("Ticker is required.");
    }
}
