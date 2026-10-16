using MediatR;
using Meezan.Application.Common.Exceptions;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Commands.ResetClassificationFundAssignment;

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
