using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Classification.Commands.ResetClassificationFundAssignment;

public sealed class ResetClassificationFundAssignmentCommandHandler : IRequestHandler<ResetClassificationFundAssignmentCommand, Result<ResetClassificationFundAssignmentResultDto>>
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

    public async Task<Result<ResetClassificationFundAssignmentResultDto>> Handle(ResetClassificationFundAssignmentCommand request, CancellationToken cancellationToken)
    {
        var fund = await _fundRepo.GetByIdAsync(request.FundId, cancellationToken);
        if (fund == null)
            return Error.NotFound("FundNotFound", "Fund not found.");

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
