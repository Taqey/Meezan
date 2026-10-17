using FluentValidation;

namespace Meezan.Application.Features.RemovalCandidates.Commands.ConfirmRemovals;

public sealed class ConfirmRemovalsCommandValidator : AbstractValidator<ConfirmRemovalsCommand>
{
    public ConfirmRemovalsCommandValidator()
    {
    }
}
