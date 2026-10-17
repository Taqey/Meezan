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

namespace Meezan.Application.Features.Funds.Categories.Commands.UpdateFundCategory;

public sealed class UpdateFundCategoryCommandHandler : IRequestHandler<UpdateFundCategoryCommand, FundCategory>
{
    private readonly IFundCategoryRepository _categoryRepo;
    private readonly IFundCategoryChangeLogRepository _changeLogRepo;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateFundCategoryCommandHandler(
        IFundCategoryRepository categoryRepo,
        IFundCategoryChangeLogRepository changeLogRepo,
        IUnitOfWork unitOfWork)
    {
        _categoryRepo = categoryRepo;
        _changeLogRepo = changeLogRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<FundCategory> Handle(UpdateFundCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepo.GetByIdAsync(request.Id, cancellationToken);
        if (category == null)
        {
            throw new NotFoundException(nameof(FundCategory), request.Id);
        }

        var oldNameAr = category.NameAr;

        category.NameAr = request.NameAr ?? category.NameAr;
        category.NameEn = request.NameEn ?? category.NameEn;
        if (request.Slug != null) category.Slug = request.Slug;
        category.Icon = request.Icon ?? category.Icon;
        category.Color = request.Color ?? category.Color;
        if (request.IsIndexTracking.HasValue) category.IsIndexTracking = request.IsIndexTracking.Value;
        category.IsVisible = request.IsVisible ?? category.IsVisible;
        category.DisplayOrder = request.DisplayOrder ?? category.DisplayOrder;
        if (request.AutoRuleJson != null) category.AutoRuleJson = request.AutoRuleJson;
        category.UpdatedAt = DateTime.UtcNow;
        category.UpdatedBy = "admin";

        _categoryRepo.Update(category);

        if (oldNameAr != category.NameAr)
        {
            await _changeLogRepo.AddAsync(new FundCategoryChangeLog
            {
                FundId = null,
                CategoryId = category.Id,
                Action = CategoryChangeAction.CategoryRenamed,
                PerformedBy = "admin",
                Reason = $"Renamed from '{oldNameAr}' to '{category.NameAr}'",
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return category;
    }
}
