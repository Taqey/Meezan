using FluentValidation;

namespace Meezan.Application.Features.Funds.Classification.Commands.ResetClassificationFundAssignment;

public sealed class ResetClassificationFundAssignmentCommandValidator : AbstractValidator<ResetClassificationFundAssignmentCommand>
{
    public ResetClassificationFundAssignmentCommandValidator()
    {
        RuleFor(x => x.FundId)
            .GreaterThan(0)
            .WithMessage("FundId must be greater than zero.");
    }
}
