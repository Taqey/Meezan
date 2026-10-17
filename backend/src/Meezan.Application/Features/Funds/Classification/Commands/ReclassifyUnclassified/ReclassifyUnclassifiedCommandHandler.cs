using MediatR;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;
using Meezan.Domain.Models;

namespace Meezan.Application.Features.Funds.Classification.Commands.ReclassifyUnclassified;

public sealed class ReclassifyUnclassifiedCommandHandler : IRequestHandler<ReclassifyUnclassifiedCommand, Result<ReclassifyResult>>
{
    private readonly IFundClassificationService _classification;

    public ReclassifyUnclassifiedCommandHandler(IFundClassificationService classification)
    {
        _classification = classification;
    }

    public async Task<Result<ReclassifyResult>> Handle(ReclassifyUnclassifiedCommand request, CancellationToken cancellationToken)
    {
        return await _classification.ReclassifyUnclassifiedAsync("admin", cancellationToken);
    }
}
