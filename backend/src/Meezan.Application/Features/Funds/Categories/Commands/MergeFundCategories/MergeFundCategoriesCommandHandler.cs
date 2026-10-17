using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Categories.Commands.MergeFundCategories;

public sealed class MergeFundCategoriesCommandHandler : IRequestHandler<MergeFundCategoriesCommand, Result<MergeFundCategoriesResultDto>>
{
    private readonly IFundCategoryRepository _categoryRepo;
    private readonly IFundCategoryOverrideRepository _overrideRepo;
    private readonly IFundCategoryChangeLogRepository _changeLogRepo;
    private readonly IUnitOfWork _unitOfWork;

    public MergeFundCategoriesCommandHandler(
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

    public async Task<Result<MergeFundCategoriesResultDto>> Handle(MergeFundCategoriesCommand request, CancellationToken cancellationToken)
    {
        var source = await _categoryRepo.GetByIdAsync(request.SourceCategoryId, cancellationToken);
        var target = await _categoryRepo.GetByIdAsync(request.TargetCategoryId, cancellationToken);
        if (source == null || target == null)
        {
            return Error.NotFound("CategoryNotFound", "One or both categories not found.");
        }

        var overrides = await _overrideRepo.GetByCategoryIdAsync(source.Id, cancellationToken);
        foreach (var o in overrides)
        {
            var existing = await _overrideRepo.GetAsync(o.FundId, target.Id, cancellationToken);
            if (existing == null)
            {
                o.CategoryId = target.Id;
                _overrideRepo.Update(o);
            }
            else
            {
                if (o.Action == OverrideAction.Include && existing.Action == OverrideAction.Exclude)
                {
                    existing.Action = OverrideAction.Include;
                    _overrideRepo.Update(existing);
                }
                _overrideRepo.Delete(o);
            }
        }

        source.IsDeleted = true;
        source.DeletedAt = DateTime.UtcNow;
        source.DeletedReason = $"Merged into '{target.NameAr}'";
        _categoryRepo.Update(source);

        await _changeLogRepo.AddAsync(new FundCategoryChangeLog
        {
            FundId = null,
            CategoryId = target.Id,
            Action = CategoryChangeAction.CategoryMerged,
            PerformedBy = "admin",
            Reason = $"Merged '{source.NameAr}' into '{target.NameAr}'",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new MergeFundCategoriesResultDto("Categories merged", overrides.Count);
    }
}
