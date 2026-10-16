using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Models;

namespace Meezan.Application.Features.Funds.Commands.ReclassifyUnclassified;

public sealed class ReclassifyUnclassifiedCommandHandler : IRequestHandler<ReclassifyUnclassifiedCommand, ReclassifyResult>
{
    private readonly IFundClassificationService _classification;

    public ReclassifyUnclassifiedCommandHandler(IFundClassificationService classification)
    {
        _classification = classification;
    }

    public Task<ReclassifyResult> Handle(ReclassifyUnclassifiedCommand request, CancellationToken cancellationToken)
    {
        return _classification.ReclassifyUnclassifiedAsync("admin", cancellationToken);
    }
}
