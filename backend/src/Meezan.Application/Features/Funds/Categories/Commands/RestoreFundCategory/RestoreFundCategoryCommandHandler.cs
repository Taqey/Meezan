using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Categories.Commands.RestoreFundCategory;

public sealed class RestoreFundCategoryCommandHandler : IRequestHandler<RestoreFundCategoryCommand, Result<FundCategory>>
{
    private readonly IFundCategoryRepository _categoryRepo;
    private readonly IFundCategoryChangeLogRepository _changeLogRepo;
    private readonly IUnitOfWork _unitOfWork;

    public RestoreFundCategoryCommandHandler(
        IFundCategoryRepository categoryRepo,
        IFundCategoryChangeLogRepository changeLogRepo,
        IUnitOfWork unitOfWork)
    {
        _categoryRepo = categoryRepo;
        _changeLogRepo = changeLogRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<FundCategory>> Handle(RestoreFundCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepo.GetByIdAsync(request.Id, cancellationToken);
        if (category == null)
        {
            return Error.NotFound("CategoryNotFound", $"{nameof(FundCategory)} with id '{request.Id}' was not found.");
        }

        category.IsDeleted = false;
        category.DeletedAt = null;
        category.DeletedReason = string.Empty;
        _categoryRepo.Update(category);

        await _changeLogRepo.AddAsync(new FundCategoryChangeLog
        {
            FundId = null,
            CategoryId = category.Id,
            Action = CategoryChangeAction.CategoryRestored,
            PerformedBy = "admin",
            Reason = $"Restored category: {category.NameAr}",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return category;
    }
}
