using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Funds.Classification.Dtos;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Classification.Commands.RestoreClassificationNode;

public sealed class RestoreClassificationNodeCommandHandler : IRequestHandler<RestoreClassificationNodeCommand, Result<AdminClassificationNodeDto>>
{
    private readonly IFundClassificationNodeRepository _nodeRepo;
    private readonly IFundClassificationOverrideRepository _overrideRepo;
    private readonly IFundClassificationChangeLogRepository _changeLogRepo;
    private readonly IFundClassificationService _classification;
    private readonly IUnitOfWork _unitOfWork;

    public RestoreClassificationNodeCommandHandler(
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

    public async Task<Result<AdminClassificationNodeDto>> Handle(RestoreClassificationNodeCommand request, CancellationToken cancellationToken)
    {
        var node = await _nodeRepo.GetByIdAsync(request.Id, includeDeleted: true, cancellationToken);
        if (node == null)
            return Error.NotFound("NodeNotFound", "Node not found.");
        if (!node.IsDeleted)
            return Error.Validation("NodeNotDeleted", "العقدة ليست محذوفة.");

        var (ok, err) = await _classification.ValidateSlugAsync(node.Slug, request.Id, cancellationToken);
        if (!ok)
            return Error.Validation("RestoreFailed", $"تعذرت الاستعادة: {err}");

        if (node.ParentId.HasValue)
        {
            var parent = await _nodeRepo.GetByIdAsync(node.ParentId.Value, includeDeleted: true, cancellationToken);
            if (parent == null || parent.IsDeleted)
                node.ParentId = null;
        }

        var now = DateTime.UtcNow;
        node.IsDeleted = false;
        node.DeletedAt = null;
        node.DeletedReason = string.Empty;
        node.UpdatedAt = now;
        node.UpdatedBy = "admin";
        _nodeRepo.Update(node);

        await _changeLogRepo.AddAsync(new FundClassificationChangeLog
        {
            FundId = null,
            Node = node,
            Action = ClassificationChangeAction.NodeRestored,
            PerformedBy = "admin",
            Reason = $"استُعيدت العقدة '{node.NameAr}'",
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
}
