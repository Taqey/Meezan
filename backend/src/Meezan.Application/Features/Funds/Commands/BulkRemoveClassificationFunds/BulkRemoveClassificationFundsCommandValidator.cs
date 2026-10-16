using FluentValidation;

namespace Meezan.Application.Features.Funds.Commands.BulkRemoveClassificationFunds;

public sealed class BulkRemoveClassificationFundsCommandValidator : AbstractValidator<BulkRemoveClassificationFundsCommand>
{
    public BulkRemoveClassificationFundsCommandValidator()
    {
        RuleFor(x => x.NodeId)
            .GreaterThan(0)
            .WithMessage("NodeId must be greater than zero.");
    }
}
