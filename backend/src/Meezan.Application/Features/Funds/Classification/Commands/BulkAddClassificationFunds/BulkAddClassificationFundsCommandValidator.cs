using FluentValidation;

namespace Meezan.Application.Features.Funds.Classification.Commands.BulkAddClassificationFunds;

public sealed class BulkAddClassificationFundsCommandValidator : AbstractValidator<BulkAddClassificationFundsCommand>
{
    public BulkAddClassificationFundsCommandValidator()
    {
        RuleFor(x => x.NodeId)
            .GreaterThan(0)
            .WithMessage("NodeId must be greater than zero.");
    }
}
