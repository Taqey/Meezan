using FluentValidation;

namespace Meezan.Application.Features.RemovalCandidates.Commands.ReactivateSelected;

public sealed class ReactivateSelectedCommandValidator : AbstractValidator<ReactivateSelectedCommand>
{
    public ReactivateSelectedCommandValidator()
    {
    }
}
