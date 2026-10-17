using FluentValidation;

namespace Meezan.Application.Features.Funds.Types.Commands.MoveFundType;

public sealed class MoveFundTypeCommandValidator : AbstractValidator<MoveFundTypeCommand>
{
    public MoveFundTypeCommandValidator()
    {
        RuleFor(x => x.FundId).GreaterThan(0);
        When(x => x.TargetTypeId.HasValue, () =>
        {
            RuleFor(x => x.TargetTypeId!.Value).GreaterThan(0);
        });
    }
}
