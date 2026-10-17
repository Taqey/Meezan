using FluentValidation;

namespace Meezan.Application.Features.Funds.Commands.BulkAddFundsToType;

public sealed class BulkAddFundsToTypeCommandValidator : AbstractValidator<BulkAddFundsToTypeCommand>
{
    public BulkAddFundsToTypeCommandValidator()
    {
        RuleFor(x => x.TypeId).GreaterThan(0);
    }
}
