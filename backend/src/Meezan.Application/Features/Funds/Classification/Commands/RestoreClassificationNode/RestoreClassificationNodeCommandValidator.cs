using FluentValidation;

namespace Meezan.Application.Features.Funds.Classification.Commands.RestoreClassificationNode;

public sealed class RestoreClassificationNodeCommandValidator : AbstractValidator<RestoreClassificationNodeCommand>
{
    public RestoreClassificationNodeCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Node id must be greater than zero.");
    }
}
