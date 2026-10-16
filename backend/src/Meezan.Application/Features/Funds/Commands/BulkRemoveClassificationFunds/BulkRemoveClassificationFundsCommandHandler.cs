using MediatR;
using Meezan.Application.Common.Exceptions;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Features.Funds.Common;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Commands.BulkRemoveClassificationFunds;

public sealed class BulkRemoveClassificationFundsCommandHandler : IRequestHandler<BulkRemoveClassificationFundsCommand, BulkRemoveFundsResultDto>
{
    private readonly IFundClassificationNodeRepository _nodeRepo;
    private readonly IFundClassificationOverrideRepository _overrideRepo;
    private readonly IFundClassificationChangeLogRepository _changeLogRepo;
    private readonly IUnitOfWork _unitOfWork;

    public BulkRemoveClassificationFundsCommandHandler(
        IFundClassificationNodeRepository nodeRepo,
        IFundClassificationOverrideRepository overrideRepo,
        IFundClassificationChangeLogRepository changeLogRepo,
        IUnitOfWork unitOfWork)
    {
        _nodeRepo = nodeRepo;
        _overrideRepo = overrideRepo;
        _changeLogRepo = changeLogRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<BulkRemoveFundsResultDto> Handle(BulkRemoveClassificationFundsCommand request, CancellationToken cancellationToken)
    {
        var node = await _nodeRepo.GetByIdAsync(request.NodeId, includeDeleted: true, cancellationToken);
        if (node == null)
            throw new NotFoundException("Node not found.");

        int moved = 0, reset = 0, failed = 0;
        var errors = new List<string>();
        var now = DateTime.UtcNow;

        FundClassificationNode? destNode = null;
        if (request.DestinationNodeId.HasValue && !request.ResetToAuto)
        {
            destNode = await _nodeRepo.GetByIdAsync(request.DestinationNodeId.Value, includeDeleted: true, cancellationToken);
            if (destNode == null || destNode.IsDeleted)
                throw new BadRequestException("عقدة الوجهة غير صالحة.");
        }

        foreach (var fid in (request.FundIds ?? new List<int>()).Distinct())
        {
            try
            {
                if (request.ResetToAuto)
                {
                    var manual = (await _overrideRepo.GetByFundIdAsync(fid, cancellationToken))
                        .Where(o => o.CreatedBy != "AutoSync").ToList();
                    _overrideRepo.DeleteRange(manual);

                    await _changeLogRepo.AddAsync(new FundClassificationChangeLog
                    {
                        FundId = fid,
                        Node = node,
                        Action = ClassificationChangeAction.ResetToAuto,
                        PerformedBy = "admin",
                        Reason = "إعادة جماعية للتلقائي",
                        CreatedAt = now
                    }, cancellationToken);

                    reset++;
                }
                else if (destNode != null)
                {
                    var manual = await _overrideRepo.GetByFundIdAsync(fid, cancellationToken);
                    foreach (var m in manual.Where(m => m.CreatedBy != "AutoSync" && m.Action == ClassificationOverrideAction.Include && m.NodeId != destNode.Id))
                    {
                        _overrideRepo.Delete(m);
                    }

                    var existing = manual.FirstOrDefault(o => o.NodeId == destNode.Id);
                    if (existing == null)
                    {
                        await _overrideRepo.AddAsync(new FundClassificationOverride
                        {
                            FundId = fid,
                            Node = destNode,
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
                        FundId = fid,
                        Node = destNode,
                        Action = ClassificationChangeAction.FundAssigned,
                        PerformedBy = "admin",
                        Reason = "نقل جماعي",
                        CreatedAt = now
                    }, cancellationToken);

                    moved++;
                }
                else
                {
                    failed++;
                    errors.Add($"{fid}: حدد الوجهة أو resetToAuto.");
                }
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{fid}: {ex.Message}");
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new BulkRemoveFundsResultDto
        {
            Moved = moved,
            Reset = reset,
            Failed = failed,
            Errors = errors
        };
    }
}
