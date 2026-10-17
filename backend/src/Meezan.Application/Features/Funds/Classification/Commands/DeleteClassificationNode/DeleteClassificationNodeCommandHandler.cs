using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Classification.Commands.DeleteClassificationNode;

public sealed class DeleteClassificationNodeCommandHandler : IRequestHandler<DeleteClassificationNodeCommand, Result<DeleteClassificationNodeResultDto>>
{
    private readonly IFundClassificationNodeRepository _nodeRepo;
    private readonly IFundRepository _fundRepo;
    private readonly IFundClassificationOverrideRepository _overrideRepo;
    private readonly IFundClassificationChangeLogRepository _changeLogRepo;
    private readonly IFundClassificationService _classification;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteClassificationNodeCommandHandler(
        IFundClassificationNodeRepository nodeRepo,
        IFundRepository fundRepo,
        IFundClassificationOverrideRepository overrideRepo,
        IFundClassificationChangeLogRepository changeLogRepo,
        IFundClassificationService classification,
        IUnitOfWork unitOfWork)
    {
        _nodeRepo = nodeRepo;
        _fundRepo = fundRepo;
        _overrideRepo = overrideRepo;
        _changeLogRepo = changeLogRepo;
        _classification = classification;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DeleteClassificationNodeResultDto>> Handle(DeleteClassificationNodeCommand request, CancellationToken cancellationToken)
    {
        var node = await _nodeRepo.GetByIdAsync(request.Id, includeDeleted: true, cancellationToken);
        if (node == null)
            return Error.NotFound("NodeNotFound", "Node not found.");
        if (node.IsDeleted)
            return Error.Validation("NodeAlreadyDeleted", "العقدة محذوفة بالفعل.");

        var desc = await _classification.GetDescendantIdsAsync(request.Id, cancellationToken);
        var funds = (await _fundRepo.GetAllAsync(cancellationToken)).Where(f => f.IsActive).ToList();
        var ctx = await _classification.LoadContextAsync(cancellationToken);
        var eff = await _classification.ResolveEffectiveManyAsync(funds, ctx);
        var affected = eff.Where(kv => kv.Value.NodeId == request.Id || (kv.Value.NodeId.HasValue && desc.Contains(kv.Value.NodeId.Value))).Select(kv => kv.Key).ToList();

        var allNodes = await _nodeRepo.GetAllAsync(includeDeleted: true, cancellationToken);
        var children = allNodes.Where(n => n.ParentId == request.Id && !n.IsDeleted).ToList();
        var childCount = children.Count;

        if (!request.Confirm && (affected.Count > 0 || childCount > 0))
        {
            return Error.Validation(
                "ConfirmRequired",
                $"العقدة تحوي {affected.Count} صندوق و{childCount} عقد بنت. حدد الوجهة وأعد التأكيد.",
                new { message = $"العقدة تحوي {affected.Count} صندوق و{childCount} عقد بنت. حدد الوجهة وأعد التأكيد.", directFunds = affected.Count, childNodes = childCount });
        }

        var now = DateTime.UtcNow;

        if (affected.Count > 0)
        {
            if (request.ResetFundsToAuto)
            {
                foreach (var fid in affected)
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
                        Reason = "إعادة للتلقائي مع حذف العقدة",
                        CreatedAt = now
                    }, cancellationToken);
                }
            }
            else if (request.MoveFundsToNodeId.HasValue)
            {
                var dest = await _nodeRepo.GetByIdAsync(request.MoveFundsToNodeId.Value, includeDeleted: true, cancellationToken);
                if (dest == null || dest.IsDeleted)
                    return Error.Validation("InvalidDestinationNode", "عقدة الوجهة غير صالحة.");

                foreach (var fid in affected)
                {
                    var manual = await _overrideRepo.GetByFundIdAsync(fid, cancellationToken);
                    foreach (var m in manual.Where(m => m.CreatedBy != "AutoSync" && m.Action == ClassificationOverrideAction.Include && m.NodeId != dest.Id))
                    {
                        _overrideRepo.Delete(m);
                    }

                    var existing = manual.FirstOrDefault(o => o.NodeId == dest.Id);
                    if (existing == null)
                    {
                        await _overrideRepo.AddAsync(new FundClassificationOverride
                        {
                            FundId = fid,
                            Node = dest,
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
                        Node = dest,
                        Action = ClassificationChangeAction.FundAssigned,
                        PerformedBy = "admin",
                        Reason = $"نُقل مع حذف العقدة '{node.NameAr}'",
                        CreatedAt = now
                    }, cancellationToken);
                }
            }
            else
            {
                return Error.Validation("MissingFundResolution", "حدد وجهة الصناديق (moveFundsToNodeId) أو resetFundsToAuto=true.");
            }
        }

        int? childDest = request.MoveChildrenToNodeId ?? node.ParentId;
        foreach (var c in children)
        {
            var (ok, err) = await _classification.ValidateMoveAsync(c.Id, childDest, cancellationToken);
            if (!ok)
                return Error.Validation("ChildMoveFailed", $"تعذر نقل البنت '{c.NameAr}': {err}");

            c.ParentId = childDest;
            if (childDest.HasValue)
            {
                var p = allNodes.FirstOrDefault(n => n.Id == childDest.Value);
                c.Level = (p?.Level ?? -1) + 1;
            }
            else
            {
                c.Level = 0;
            }
            c.UpdatedAt = now;
            c.UpdatedBy = "admin";
            _nodeRepo.Update(c);
        }

        node.IsDeleted = true;
        node.DeletedAt = now;
        node.DeletedReason = $"Deleted by admin. Had {affected.Count} fund(s), {childCount} child node(s).";
        node.UpdatedAt = now;
        node.UpdatedBy = "admin";
        _nodeRepo.Update(node);

        await _changeLogRepo.AddAsync(new FundClassificationChangeLog
        {
            FundId = null,
            Node = node,
            Action = ClassificationChangeAction.NodeDeleted,
            PerformedBy = "admin",
            Reason = $"حُذفت العقدة '{node.NameAr}'",
            CreatedAt = now
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new DeleteClassificationNodeResultDto
        {
            Message = "Node deleted",
            MovedFunds = affected.Count,
            MovedChildren = children.Count
        };
    }
}
