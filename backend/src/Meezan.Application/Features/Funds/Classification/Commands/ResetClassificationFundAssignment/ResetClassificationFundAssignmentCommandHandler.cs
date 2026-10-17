using MediatR;
using Meezan.Application.Common.Exceptions;
using Meezan.Application.Common.Interfaces.Integrations;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Queries.Funds;
using Meezan.Application.Common.Interfaces.Queries.RemovalCandidates;
using Meezan.Application.Common.Interfaces.Queries.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Interfaces.Repositories.Indices;
using Meezan.Application.Common.Interfaces.Repositories.Market;
using Meezan.Application.Common.Interfaces.Repositories.Scraping;
using Meezan.Application.Common.Interfaces.Repositories.Sectors;
using Meezan.Application.Common.Interfaces.Repositories.Shariah;
using Meezan.Application.Common.Interfaces.Repositories.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.System;
using Meezan.Application.Common.Interfaces.Security;
using Meezan.Application.Common.Interfaces.Storage;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Classification.Commands.ResetClassificationFundAssignment;

public sealed class ResetClassificationFundAssignmentCommandHandler : IRequestHandler<ResetClassificationFundAssignmentCommand, ResetClassificationFundAssignmentResultDto>
{
    private readonly IFundRepository _fundRepo;
    private readonly IFundClassificationOverrideRepository _overrideRepo;
    private readonly IFundClassificationChangeLogRepository _changeLogRepo;
    private readonly IUnitOfWork _unitOfWork;

    public ResetClassificationFundAssignmentCommandHandler(
        IFundRepository fundRepo,
        IFundClassificationOverrideRepository overrideRepo,
        IFundClassificationChangeLogRepository changeLogRepo,
        IUnitOfWork unitOfWork)
    {
        _fundRepo = fundRepo;
        _overrideRepo = overrideRepo;
        _changeLogRepo = changeLogRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<ResetClassificationFundAssignmentResultDto> Handle(ResetClassificationFundAssignmentCommand request, CancellationToken cancellationToken)
    {
        var fund = await _fundRepo.GetByIdAsync(request.FundId, cancellationToken);
        if (fund == null)
            throw new NotFoundException("Fund not found.");

        var manual = (await _overrideRepo.GetByFundIdAsync(request.FundId, cancellationToken))
            .Where(o => o.CreatedBy != "AutoSync").ToList();

        _overrideRepo.DeleteRange(manual);

        await _changeLogRepo.AddAsync(new FundClassificationChangeLog
        {
            FundId = request.FundId,
            NodeId = null,
            Action = ClassificationChangeAction.ResetToAuto,
            PerformedBy = "admin",
            Reason = "إرجاع للتلقائي",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ResetClassificationFundAssignmentResultDto
        {
            Message = "Fund reset to auto",
            ClearedCount = manual.Count
        };
    }
}
