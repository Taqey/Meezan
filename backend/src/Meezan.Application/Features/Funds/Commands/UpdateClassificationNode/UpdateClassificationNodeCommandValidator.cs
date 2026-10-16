using FluentValidation;

namespace Meezan.Application.Features.Funds.Commands.UpdateClassificationNode;

public sealed class UpdateClassificationNodeCommandValidator : AbstractValidator<UpdateClassificationNodeCommand>
{
    public UpdateClassificationNodeCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Node id must be greater than zero.");
    }
}
