using FluentValidation;

namespace Meezan.Application.Features.Funds.Commands.DeleteClassificationNode;

public sealed class DeleteClassificationNodeCommandValidator : AbstractValidator<DeleteClassificationNodeCommand>
{
    public DeleteClassificationNodeCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Node id must be greater than zero.");
    }
}
