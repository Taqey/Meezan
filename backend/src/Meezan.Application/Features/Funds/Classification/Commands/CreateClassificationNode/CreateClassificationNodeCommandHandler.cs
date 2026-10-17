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
using Meezan.Domain.Services;

namespace Meezan.Application.Features.Funds.Classification.Commands.CreateClassificationNode;

public sealed class CreateClassificationNodeCommandHandler : IRequestHandler<CreateClassificationNodeCommand, Result<CreateClassificationNodeResultDto>>
{
    private readonly IFundClassificationNodeRepository _nodeRepo;
    private readonly IFundRepository _fundRepo;
    private readonly IFundClassificationOverrideRepository _overrideRepo;
    private readonly IFundClassificationChangeLogRepository _changeLogRepo;
    private readonly IFundClassificationService _classification;
    private readonly IUnitOfWork _unitOfWork;

    public CreateClassificationNodeCommandHandler(
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

    public async Task<Result<CreateClassificationNodeResultDto>> Handle(CreateClassificationNodeCommand request, CancellationToken cancellationToken)
    {
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? FundClassificationTreeEngine.GenerateSlug(request.NameEn)
            : request.Slug.Trim().ToLowerInvariant();

        var (slugOk, slugErr) = await _classification.ValidateSlugAsync(slug, null, cancellationToken);
        if (!slugOk)
            return Error.Validation("InvalidSlug", slugErr ?? "Invalid slug.");

        FundClassificationNode? parent = null;
        int level = 0;
        if (request.ParentId.HasValue)
        {
            parent = await _nodeRepo.GetByIdAsync(request.ParentId.Value, includeDeleted: true, cancellationToken);
            if (parent == null)
                return Error.NotFound("ParentNotFound", "الأب غير موجود.");
            if (parent.IsDeleted)
                return Error.Validation("ParentDeleted", "لا يمكن الإضافة تحت عقدة محذوفة.");

            level = parent.Level + 1;
            if (level > FundClassificationNode.MaxDepth - 1)
                return Error.Validation("MaxDepthExceeded", $"يتجاوز أقصى عمق ({FundClassificationNode.MaxDepth} مستويات).");
        }

        List<Fund> parentFunds = new();
        if (parent != null && request.FundDisposition != null
            && !string.Equals(request.FundDisposition, "keep", StringComparison.OrdinalIgnoreCase))
        {
            var funds = (await _fundRepo.GetAllAsync(cancellationToken)).Where(f => f.IsActive).ToList();
            var ctx0 = await _classification.LoadContextAsync(cancellationToken);
            var eff0 = await _classification.ResolveEffectiveManyAsync(funds, ctx0);
            parentFunds = funds.Where(f => eff0.TryGetValue(f.Id, out var e) && e.NodeId == parent.Id).ToList();
        }

        var now = DateTime.UtcNow;
        var node = new FundClassificationNode
        {
            Slug = slug,
            NameAr = request.NameAr.Trim(),
            NameEn = request.NameEn.Trim(),
            Level = level,
            ParentId = parent?.Id,
            Icon = request.Icon ?? string.Empty,
            Color = request.Color ?? string.Empty,
            DisplayOrder = request.DisplayOrder ?? 0,
            IsVisible = request.IsVisible ?? true,
            IsDeleted = false,
            RequiresChildSelection = request.RequiresChildSelection ?? false,
            ChildrenLabelAr = request.ChildrenLabelAr ?? "التصنيف الفرعي",
            ChildrenLabelEn = request.ChildrenLabelEn ?? "Sub classification",
            AutoRuleJson = request.AutoRuleJson ?? string.Empty,
            AutoClassificationEnabled = request.AutoClassificationEnabled ?? true,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "admin",
            UpdatedBy = "admin"
        };

        await _nodeRepo.AddAsync(node, cancellationToken);

        await _changeLogRepo.AddAsync(new FundClassificationChangeLog
        {
            FundId = null,
            Node = node,
            Action = ClassificationChangeAction.NodeCreated,
            PerformedBy = "admin",
            Reason = $"أنشئت العقدة '{node.NameAr}' ({node.Slug})",
            CreatedAt = now
        }, cancellationToken);

        int movedFunds = 0;
        if (parent != null && string.Equals(request.FundDisposition, "moveAll", StringComparison.OrdinalIgnoreCase) && parentFunds.Count > 0)
        {
            foreach (var f in parentFunds)
            {
                var manual = await _overrideRepo.GetByFundIdAsync(f.Id, cancellationToken);
                foreach (var m in manual.Where(m => m.CreatedBy != "AutoSync" && m.Action == ClassificationOverrideAction.Include && m.NodeId != node.Id))
                {
                    _overrideRepo.Delete(m);
                }

                var existing = manual.FirstOrDefault(o => o.NodeId == node.Id);
                if (existing == null)
                {
                    await _overrideRepo.AddAsync(new FundClassificationOverride
                    {
                        FundId = f.Id,
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
                    FundId = f.Id,
                    Node = node,
                    Action = ClassificationChangeAction.FundAssigned,
                    PerformedBy = "admin",
                    Reason = "نقل تلقائي مع إنشاء التصنيف الفرعي",
                    CreatedAt = now
                }, cancellationToken);

                movedFunds++;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var directCount = await _overrideRepo.CountByNodeIdAsync(node.Id, cancellationToken);
        var dto = new AdminClassificationNodeDto
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

        return new CreateClassificationNodeResultDto
        {
            Node = dto,
            MovedFunds = movedFunds
        };
    }
}
