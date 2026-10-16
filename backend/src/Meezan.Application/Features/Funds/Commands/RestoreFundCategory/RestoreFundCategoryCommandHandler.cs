using MediatR;
using Meezan.Application.Common.Exceptions;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Commands.RestoreFundCategory;

public sealed class RestoreFundCategoryCommandHandler : IRequestHandler<RestoreFundCategoryCommand, FundCategory>
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

    public async Task<FundCategory> Handle(RestoreFundCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepo.GetByIdAsync(request.Id, cancellationToken);
        if (category == null)
        {
            throw new NotFoundException(nameof(FundCategory), request.Id);
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
