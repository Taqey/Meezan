using MediatR;
using Meezan.Application.Common.Exceptions;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Features.Funds.Common;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Commands.UpdateClassificationNode;

public sealed class UpdateClassificationNodeCommandHandler : IRequestHandler<UpdateClassificationNodeCommand, AdminClassificationNodeDto>
{
    private readonly IFundClassificationNodeRepository _nodeRepo;
    private readonly IFundClassificationOverrideRepository _overrideRepo;
    private readonly IFundClassificationChangeLogRepository _changeLogRepo;
    private readonly IFundClassificationService _classification;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateClassificationNodeCommandHandler(
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

    public async Task<AdminClassificationNodeDto> Handle(UpdateClassificationNodeCommand request, CancellationToken cancellationToken)
    {
        var node = await _nodeRepo.GetByIdAsync(request.Id, includeDeleted: true, cancellationToken);
        if (node == null)
            throw new NotFoundException("Node not found.");
        if (node.IsDeleted)
            throw new BadRequestException("العقدة محذوفة. استعدها أولاً.");

        var now = DateTime.UtcNow;

        if (request.Slug != null && !string.Equals(request.Slug.Trim(), node.Slug, StringComparison.OrdinalIgnoreCase))
        {
            var (ok, err) = await _classification.ValidateSlugAsync(request.Slug, request.Id, cancellationToken);
            if (!ok)
                throw new BadRequestException(err ?? "Invalid slug.");

            var oldSlug = node.Slug;
            node.Slug = request.Slug.Trim().ToLowerInvariant();

            await _changeLogRepo.AddAsync(new FundClassificationChangeLog
            {
                FundId = null,
                Node = node,
                Action = ClassificationChangeAction.NodeRenamed,
                PerformedBy = "admin",
                Reason = $"تغير المعرف '{oldSlug}' → '{node.Slug}'",
                CreatedAt = now
            }, cancellationToken);
        }

        bool renamed = false;
        if (request.NameAr != null && request.NameAr != node.NameAr) { renamed = true; node.NameAr = request.NameAr; }
        if (request.NameEn != null && request.NameEn != node.NameEn) { renamed = true; node.NameEn = request.NameEn; }
        if (request.Icon != null) node.Icon = request.Icon;
        if (request.Color != null) node.Color = request.Color;
        if (request.DisplayOrder.HasValue) node.DisplayOrder = request.DisplayOrder.Value;

        if (request.IsVisible.HasValue && request.IsVisible.Value != node.IsVisible)
        {
            node.IsVisible = request.IsVisible.Value;
            await _changeLogRepo.AddAsync(new FundClassificationChangeLog
            {
                FundId = null,
                Node = node,
                Action = ClassificationChangeAction.VisibilityChanged,
                PerformedBy = "admin",
                Reason = node.IsVisible ? "أظهرت" : "أخفيت",
                CreatedAt = now
            }, cancellationToken);
        }

        if (request.RequiresChildSelection.HasValue) node.RequiresChildSelection = request.RequiresChildSelection.Value;
        if (request.ChildrenLabelAr != null) node.ChildrenLabelAr = request.ChildrenLabelAr;
        if (request.ChildrenLabelEn != null) node.ChildrenLabelEn = request.ChildrenLabelEn;

        if (request.AutoRuleJson != null && request.AutoRuleJson != node.AutoRuleJson)
        {
            node.AutoRuleJson = request.AutoRuleJson;
            await _changeLogRepo.AddAsync(new FundClassificationChangeLog
            {
                FundId = null,
                Node = node,
                Action = ClassificationChangeAction.RuleChanged,
                PerformedBy = "admin",
                Reason = "عُدّلت قاعدة التصنيف التلقائي",
                CreatedAt = now
            }, cancellationToken);
        }

        if (request.AutoClassificationEnabled.HasValue) node.AutoClassificationEnabled = request.AutoClassificationEnabled.Value;

        node.UpdatedAt = now;
        node.UpdatedBy = "admin";
        _nodeRepo.Update(node);

        if (renamed)
        {
            await _changeLogRepo.AddAsync(new FundClassificationChangeLog
            {
                FundId = null,
                Node = node,
                Action = ClassificationChangeAction.NodeRenamed,
                PerformedBy = "admin",
                Reason = $"أعيدت تسمية العقدة إلى '{node.NameAr}'",
                CreatedAt = now
            }, cancellationToken);
        }

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
}
