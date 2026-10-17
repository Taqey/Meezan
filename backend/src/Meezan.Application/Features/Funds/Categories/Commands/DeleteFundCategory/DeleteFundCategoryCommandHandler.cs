using MediatR;
using Meezan.Application.Common.Results;
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

namespace Meezan.Application.Features.Funds.Categories.Commands.DeleteFundCategory;

public sealed class DeleteFundCategoryCommandHandler : IRequestHandler<DeleteFundCategoryCommand, Result<DeleteFundCategoryResultDto>>
{
    private readonly IFundCategoryRepository _categoryRepo;
    private readonly IFundCategoryOverrideRepository _overrideRepo;
    private readonly IFundCategoryChangeLogRepository _changeLogRepo;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteFundCategoryCommandHandler(
        IFundCategoryRepository categoryRepo,
        IFundCategoryOverrideRepository overrideRepo,
        IFundCategoryChangeLogRepository changeLogRepo,
        IUnitOfWork unitOfWork)
    {
        _categoryRepo = categoryRepo;
        _overrideRepo = overrideRepo;
        _changeLogRepo = changeLogRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DeleteFundCategoryResultDto>> Handle(DeleteFundCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepo.GetByIdAsync(request.Id, cancellationToken);
        if (category == null)
        {
            return Error.NotFound("CategoryNotFound", $"{nameof(FundCategory)} with id '{request.Id}' was not found.");
        }

        var overrides = await _overrideRepo.GetByCategoryIdAsync(request.Id, cancellationToken);
        var fundCount = overrides.Count;

        if (!request.Confirm && fundCount > 0)
        {
            return Error.Validation(
                "CategoryHasFunds",
                $"Category has {fundCount} funds. Pass confirm=true to delete.",
                new { message = $"Category has {fundCount} funds. Pass confirm=true to delete.", fundCount });
        }

        category.IsDeleted = true;
        category.DeletedAt = DateTime.UtcNow;
        category.DeletedReason = $"Deleted by admin. Had {fundCount} funds.";
        _categoryRepo.Update(category);

        foreach (var o in overrides)
        {
            _overrideRepo.Delete(o);
        }

        await _changeLogRepo.AddAsync(new FundCategoryChangeLog
        {
            FundId = null,
            CategoryId = category.Id,
            Action = CategoryChangeAction.CategoryDeleted,
            PerformedBy = "admin",
            Reason = $"Deleted category: {category.NameAr} (had {fundCount} funds)",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new DeleteFundCategoryResultDto("Category deleted", fundCount);
    }
}
