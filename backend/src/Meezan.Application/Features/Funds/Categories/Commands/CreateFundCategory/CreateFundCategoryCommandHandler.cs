using MediatR;
using Meezan.Application.Common.Results;
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

namespace Meezan.Application.Features.Funds.Categories.Commands.CreateFundCategory;

public sealed class CreateFundCategoryCommandHandler : IRequestHandler<CreateFundCategoryCommand, Result<FundCategory>>
{
    private readonly IFundCategoryRepository _categoryRepo;
    private readonly IFundCategoryChangeLogRepository _changeLogRepo;
    private readonly IUnitOfWork _unitOfWork;

    public CreateFundCategoryCommandHandler(
        IFundCategoryRepository categoryRepo,
        IFundCategoryChangeLogRepository changeLogRepo,
        IUnitOfWork unitOfWork)
    {
        _categoryRepo = categoryRepo;
        _changeLogRepo = changeLogRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<FundCategory>> Handle(CreateFundCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = new FundCategory
        {
            NameAr = request.NameAr,
            NameEn = request.NameEn,
            Slug = request.Slug ?? GenerateSlug(request.NameEn ?? request.NameAr),
            Icon = request.Icon ?? string.Empty,
            Color = request.Color ?? string.Empty,
            IsAutoDerived = request.IsAutoDerived,
            IsIndexTracking = request.IsIndexTracking,
            AutoRuleJson = request.AutoRuleJson ?? string.Empty,
            IsVisible = request.IsVisible,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "admin",
            UpdatedBy = "admin"
        };

        await _categoryRepo.AddAsync(category, cancellationToken);

        await _changeLogRepo.AddAsync(new FundCategoryChangeLog
        {
            FundId = null,
            CategoryId = category.Id,
            Action = CategoryChangeAction.CategoryCreated,
            PerformedBy = "admin",
            Reason = $"Created category: {category.NameAr}",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return category;
    }

    private static string GenerateSlug(string input)
    {
        return input.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-")
            .Replace(".", "")
            .Replace("/", "-");
    }
}
