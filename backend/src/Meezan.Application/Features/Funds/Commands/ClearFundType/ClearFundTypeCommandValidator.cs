using FluentValidation;

namespace Meezan.Application.Features.Funds.Commands.ClearFundType;

public sealed class ClearFundTypeCommandValidator : AbstractValidator<ClearFundTypeCommand>
{
    public ClearFundTypeCommandValidator()
    {
        RuleFor(x => x.FundId).GreaterThan(0);
    }
}
