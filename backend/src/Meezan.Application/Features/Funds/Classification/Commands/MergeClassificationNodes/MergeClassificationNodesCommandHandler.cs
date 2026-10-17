using Meezan.Application.Common.Results;
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

namespace Meezan.Application.Features.Funds.Classification.Commands.MergeClassificationNodes;

public sealed class MergeClassificationNodesCommandHandler : IRequestHandler<MergeClassificationNodesCommand, Result<MergeClassificationNodesResultDto>>
{
    private readonly IFundClassificationNodeRepository _nodeRepo;
    private readonly IFundClassificationOverrideRepository _overrideRepo;
    private readonly IFundClassificationChangeLogRepository _changeLogRepo;
    private readonly IFundClassificationService _classification;
    private readonly IUnitOfWork _unitOfWork;

    public MergeClassificationNodesCommandHandler(
        IFundClassificationNodeRepository nodeRepo,
        IFundClassificationOverrideRepository overrideRepo,
        IFundClassificationChangeLogRepository changeLogRepo,
        IFundClassificationService classification,
        IUnitOfWork unitOfWork)
    {
        _nodeRepo = nodeRepo;
        _overrideRepo = overrideRepo;
        _changeLogRepo = changeLogRepo;
        _classification = classification;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<MergeClassificationNodesResultDto>> Handle(MergeClassificationNodesCommand request, CancellationToken cancellationToken)
    {
        if (request.SourceId == request.TargetId)
            return Error.Validation("SourceTargetSame", "المصدر والهدف متماثلان.");

        var source = await _nodeRepo.GetByIdAsync(request.SourceId, includeDeleted: true, cancellationToken);
        var target = await _nodeRepo.GetByIdAsync(request.TargetId, includeDeleted: true, cancellationToken);
        if (source == null || target == null)
            return Error.NotFound("NodeNotFound", "Source or target node not found.");

        if (source.IsDeleted || target.IsDeleted)
            return Error.Validation("CannotMergeDeleted", "لا يمكن دمج عقد محذوفة.");

        var desc = await _classification.GetDescendantIdsAsync(source.Id, cancellationToken);
        if (desc.Contains(target.Id))
            return Error.Validation("CycleDetected", "لا يمكن الدمج في عقدة بنت (دورة).");

        var allNodes = await _nodeRepo.GetAllAsync(includeDeleted: true, cancellationToken);
        var byParent = allNodes.Where(n => n.ParentId.HasValue).GroupBy(n => n.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var children = byParent.TryGetValue(source.Id, out var kids) ? kids : new List<FundClassificationNode>();
        var now = DateTime.UtcNow;

        foreach (var c in children)
        {
            c.ParentId = target.Id;
            c.Level = target.Level + 1;
            c.UpdatedAt = now;
            c.UpdatedBy = "admin";
            FixSubtreeLevels(c.Id, target.Level + 1, byParent, now);
            _nodeRepo.Update(c);
        }

        var sourceRows = await _overrideRepo.GetByNodeIdAsync(source.Id, cancellationToken);
        int moved = 0;
        var fundsHandled = new HashSet<int>();

        foreach (var row in sourceRows.Where(o => o.Action == ClassificationOverrideAction.Include && !string.Equals(o.CreatedBy, "AutoSync", StringComparison.OrdinalIgnoreCase)))
        {
            var otherOverrides = await _overrideRepo.GetByFundIdAsync(row.FundId, cancellationToken);
            bool hasOther = otherOverrides.Any(o => o.NodeId != source.Id && o.Action == ClassificationOverrideAction.Include && o.CreatedBy != "AutoSync");
            if (hasOther)
            {
                _overrideRepo.Delete(row);
                continue;
            }

            row.NodeId = target.Id;
            _overrideRepo.Update(row);
            moved++;
        }

        var autoRows = sourceRows.Where(o => o.CreatedBy == "AutoSync").ToList();
        _overrideRepo.DeleteRange(autoRows);

        source.IsDeleted = true;
        source.DeletedAt = now;
        source.DeletedReason = $"Merged into '{target.NameAr}'";
        source.UpdatedAt = now;
        source.UpdatedBy = "admin";
        _nodeRepo.Update(source);

        await _changeLogRepo.AddAsync(new FundClassificationChangeLog
        {
            FundId = null,
            Node = target,
            Action = ClassificationChangeAction.NodeMerged,
            PerformedBy = "admin",
            Reason = $"دُمج '{source.NameAr}' في '{target.NameAr}' (نُقل {moved} تعييناً)",
            CreatedAt = now
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new MergeClassificationNodesResultDto
        {
            Message = "Nodes merged",
            MovedAssignments = moved,
            MovedChildren = children.Count
        };
    }

    private static void FixSubtreeLevels(int rootId, int level, Dictionary<int, List<FundClassificationNode>> byParent, DateTime now)
    {
        var stack = new Stack<(int Id, int Level)>();
        stack.Push((rootId, level));
        while (stack.Count > 0)
        {
            var (cur, lv) = stack.Pop();
            if (byParent.TryGetValue(cur, out var kids))
            {
                foreach (var k in kids)
                {
                    k.Level = lv + 1;
                    k.UpdatedAt = now;
                    k.UpdatedBy = "admin";
                    stack.Push((k.Id, lv + 1));
                }
            }
        }
    }
}
