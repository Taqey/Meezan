using FluentValidation;

namespace Meezan.Application.Features.Funds.Commands.MoveClassificationNode;

public sealed class MoveClassificationNodeCommandValidator : AbstractValidator<MoveClassificationNodeCommand>
{
    public MoveClassificationNodeCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Node id must be greater than zero.");
    }
}
