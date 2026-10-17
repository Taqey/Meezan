using FluentValidation;

namespace Meezan.Application.Features.Funds.Types.Commands.SetFundType;

public sealed class SetFundTypeCommandValidator : AbstractValidator<SetFundTypeCommand>
{
    public SetFundTypeCommandValidator()
    {
        RuleFor(x => x.FundId).GreaterThan(0);
        RuleFor(x => x.TypeId).GreaterThan(0);
    }
}
