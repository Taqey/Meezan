using FluentValidation;

namespace Meezan.Application.Features.Funds.Commands.MergeClassificationNodes;

public sealed class MergeClassificationNodesCommandValidator : AbstractValidator<MergeClassificationNodesCommand>
{
    public MergeClassificationNodesCommandValidator()
    {
        RuleFor(x => x.SourceId)
            .GreaterThan(0)
            .WithMessage("SourceId must be greater than zero.");

        RuleFor(x => x.TargetId)
            .GreaterThan(0)
            .WithMessage("TargetId must be greater than zero.");

        RuleFor(x => x)
            .Must(x => x.SourceId != x.TargetId)
            .WithMessage("المصدر والهدف متماثلان.");
    }
}
