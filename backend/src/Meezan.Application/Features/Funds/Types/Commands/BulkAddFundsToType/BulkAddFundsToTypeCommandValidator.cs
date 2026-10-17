using FluentValidation;

namespace Meezan.Application.Features.Funds.Types.Commands.BulkAddFundsToType;

public sealed class BulkAddFundsToTypeCommandValidator : AbstractValidator<BulkAddFundsToTypeCommand>
{
    public BulkAddFundsToTypeCommandValidator()
    {
        RuleFor(x => x.TypeId).GreaterThan(0);
    }
}
