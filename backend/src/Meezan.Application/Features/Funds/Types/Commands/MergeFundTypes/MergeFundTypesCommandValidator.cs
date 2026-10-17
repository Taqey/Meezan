using FluentValidation;

namespace Meezan.Application.Features.Funds.Types.Commands.MergeFundTypes;

public sealed class MergeFundTypesCommandValidator : AbstractValidator<MergeFundTypesCommand>
{
    public MergeFundTypesCommandValidator()
    {
        RuleFor(x => x.SourceId).GreaterThan(0);
        RuleFor(x => x.TargetId).GreaterThan(0);
        RuleFor(x => x).Must(x => x.SourceId != x.TargetId).WithMessage("Source and target types cannot be the same.");
    }
}
