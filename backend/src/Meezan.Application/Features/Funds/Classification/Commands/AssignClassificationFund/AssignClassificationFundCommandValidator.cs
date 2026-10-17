using FluentValidation;

namespace Meezan.Application.Features.Funds.Classification.Commands.AssignClassificationFund;

public sealed class AssignClassificationFundCommandValidator : AbstractValidator<AssignClassificationFundCommand>
{
    public AssignClassificationFundCommandValidator()
    {
        RuleFor(x => x.FundId)
            .GreaterThan(0)
            .WithMessage("FundId must be greater than zero.");

        RuleFor(x => x.NodeId)
            .GreaterThan(0)
            .WithMessage("NodeId must be greater than zero.");
    }
}
