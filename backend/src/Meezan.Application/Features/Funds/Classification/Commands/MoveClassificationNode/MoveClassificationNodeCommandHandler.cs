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
using Meezan.Application.Features.Funds.Categories.Dtos;
using Meezan.Application.Features.Funds.Classification.Dtos;
using Meezan.Application.Features.Funds.Details;
using Meezan.Application.Features.Funds.Details.Dtos;
using Meezan.Application.Features.Funds.Listing.Dtos;
using Meezan.Application.Features.Funds.Types.Dtos;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Classification.Commands.MoveClassificationNode;

public sealed class MoveClassificationNodeCommandHandler : IRequestHandler<MoveClassificationNodeCommand, Result<AdminClassificationNodeDto>>
{
    private readonly IFundClassificationNodeRepository _nodeRepo;
    private readonly IFundClassificationOverrideRepository _overrideRepo;
    private readonly IFundClassificationChangeLogRepository _changeLogRepo;
    private readonly IFundClassificationService _classification;
    private readonly IUnitOfWork _unitOfWork;

    public MoveClassificationNodeCommandHandler(
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

    public async Task<Result<AdminClassificationNodeDto>> Handle(MoveClassificationNodeCommand request, CancellationToken cancellationToken)
    {
        var node = await _nodeRepo.GetByIdAsync(request.Id, includeDeleted: true, cancellationToken);
        if (node == null)
            return Error.NotFound("NodeNotFound", "Node not found.");
        if (node.IsDeleted)
            return Error.Validation("NodeDeleted", "العقدة محذوفة. استعدها أولاً.");

        var (ok, err) = await _classification.ValidateMoveAsync(request.Id, request.NewParentId, cancellationToken);
        if (!ok)
            return Error.Validation("InvalidMove", err ?? "Invalid move.");

        var oldParent = node.ParentId;
        node.ParentId = request.NewParentId;

        var all = await _nodeRepo.GetAllAsync(includeDeleted: true, cancellationToken);
        var byId = all.ToDictionary(n => n.Id);
        int baseLevel = request.NewParentId.HasValue && byId.TryGetValue(request.NewParentId.Value, out var p) ? p.Level + 1 : 0;

        var now = DateTime.UtcNow;
        node.UpdatedAt = now;
        node.UpdatedBy = "admin";
        _nodeRepo.Update(node);

        var movedSubtreeIds = new HashSet<int>();
        Relevel(node.Id, baseLevel, byId, movedSubtreeIds, now);

        foreach (var idToUpdate in movedSubtreeIds)
        {
            if (idToUpdate != node.Id && byId.TryGetValue(idToUpdate, out var subtreeNode))
            {
                _nodeRepo.Update(subtreeNode);
            }
        }

        await _changeLogRepo.AddAsync(new FundClassificationChangeLog
        {
            FundId = null,
            Node = node,
            Action = ClassificationChangeAction.NodeMoved,
            PerformedBy = "admin",
            Reason = $"نُقلت من {oldParent?.ToString() ?? "الجذر"} إلى {request.NewParentId?.ToString() ?? "الجذر"}",
            CreatedAt = now
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var directCount = await _overrideRepo.CountByNodeIdAsync(node.Id, cancellationToken);
        return new AdminClassificationNodeDto
        {
            Id = node.Id,
            ParentId = node.ParentId,
            Level = node.Level,
            Slug = node.Slug,
            NameAr = node.NameAr,
            NameEn = node.NameEn,
            Icon = node.Icon,
            Color = node.Color,
            DisplayOrder = node.DisplayOrder,
            IsVisible = node.IsVisible,
            IsDeleted = node.IsDeleted,
            DeletedAt = node.DeletedAt,
            DeletedReason = node.DeletedReason,
            RequiresChildSelection = node.RequiresChildSelection,
            ChildrenLabelAr = node.ChildrenLabelAr,
            ChildrenLabelEn = node.ChildrenLabelEn,
            AutoRuleJson = node.AutoRuleJson,
            AutoClassificationEnabled = node.AutoClassificationEnabled,
            CreatedAt = node.CreatedAt,
            UpdatedAt = node.UpdatedAt,
            CreatedBy = node.CreatedBy,
            UpdatedBy = node.UpdatedBy,
            DirectCount = directCount,
            TotalCount = directCount
        };
    }

    private static void Relevel(int rootId, int level, Dictionary<int, FundClassificationNode> byId, HashSet<int> movedSubtreeIds, DateTime now)
    {
        if (!byId.TryGetValue(rootId, out var root)) return;
        root.Level = level;
        root.UpdatedAt = now;
        root.UpdatedBy = "admin";
        movedSubtreeIds.Add(rootId);

        foreach (var k in byId.Values.Where(n => n.ParentId == rootId).ToList())
            Relevel(k.Id, level + 1, byId, movedSubtreeIds, now);
    }
}
