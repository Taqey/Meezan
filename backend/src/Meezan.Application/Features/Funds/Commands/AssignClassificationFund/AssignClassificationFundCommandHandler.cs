using MediatR;
using Meezan.Application.Common.Exceptions;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Commands.AssignClassificationFund;

public sealed class AssignClassificationFundCommandHandler : IRequestHandler<AssignClassificationFundCommand, AssignClassificationFundResultDto>
{
    private readonly IFundRepository _fundRepo;
    private readonly IFundClassificationNodeRepository _nodeRepo;
    private readonly IFundClassificationOverrideRepository _overrideRepo;
    private readonly IFundClassificationChangeLogRepository _changeLogRepo;
    private readonly IUnitOfWork _unitOfWork;

    public AssignClassificationFundCommandHandler(
        IFundRepository fundRepo,
        IFundClassificationNodeRepository nodeRepo,
        IFundClassificationOverrideRepository overrideRepo,
        IFundClassificationChangeLogRepository changeLogRepo,
        IUnitOfWork unitOfWork)
    {
        _fundRepo = fundRepo;
        _nodeRepo = nodeRepo;
        _overrideRepo = overrideRepo;
        _changeLogRepo = changeLogRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<AssignClassificationFundResultDto> Handle(AssignClassificationFundCommand request, CancellationToken cancellationToken)
    {
        var fund = await _fundRepo.GetByIdAsync(request.FundId, cancellationToken);
        if (fund == null)
            throw new NotFoundException("الصندوق غير موجود.");

        var node = await _nodeRepo.GetByIdAsync(request.NodeId, includeDeleted: true, cancellationToken);
        if (node == null || node.IsDeleted)
            throw new BadRequestException("العقدة غير صالحة.");

        var manual = await _overrideRepo.GetByFundIdAsync(request.FundId, cancellationToken);
        foreach (var m in manual.Where(m => m.CreatedBy != "AutoSync" && m.Action == ClassificationOverrideAction.Include && m.NodeId != request.NodeId))
        {
            _overrideRepo.Delete(m);
        }

        var now = DateTime.UtcNow;
        var existing = manual.FirstOrDefault(o => o.NodeId == request.NodeId);
        if (existing == null)
        {
            await _overrideRepo.AddAsync(new FundClassificationOverride
            {
                FundId = request.FundId,
                Node = node,
                Action = ClassificationOverrideAction.Include,
                CreatedAt = now,
                CreatedBy = "admin"
            }, cancellationToken);
        }
        else if (!string.Equals(existing.CreatedBy, "admin", StringComparison.OrdinalIgnoreCase))
        {
            existing.CreatedBy = "admin";
            existing.CreatedAt = now;
            _overrideRepo.Update(existing);
        }

        await _changeLogRepo.AddAsync(new FundClassificationChangeLog
        {
            FundId = request.FundId,
            Node = node,
            Action = ClassificationChangeAction.FundAssigned,
            PerformedBy = "admin",
            Reason = request.Reason ?? "تعيين يدوي",
            CreatedAt = now
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AssignClassificationFundResultDto
        {
            Message = "Fund assigned",
            FundId = request.FundId,
            NodeId = request.NodeId
        };
    }
}
