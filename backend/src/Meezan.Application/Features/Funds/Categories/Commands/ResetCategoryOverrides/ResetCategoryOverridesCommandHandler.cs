using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Categories.Commands.ResetCategoryOverrides;

public sealed class ResetCategoryOverridesCommandHandler : IRequestHandler<ResetCategoryOverridesCommand, Result<ResetCategoryOverridesResultDto>>
{
    private readonly IFundCategoryRepository _categoryRepo;
    private readonly IFundCategoryOverrideRepository _overrideRepo;
    private readonly IFundCategoryChangeLogRepository _changeLogRepo;
    private readonly IUnitOfWork _unitOfWork;

    public ResetCategoryOverridesCommandHandler(
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

    public async Task<Result<ResetCategoryOverridesResultDto>> Handle(ResetCategoryOverridesCommand request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepo.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category == null)
        {
            return Error.NotFound("CategoryNotFound", $"{nameof(FundCategory)} with id '{request.CategoryId}' was not found.");
        }

        var manual = (await _overrideRepo.GetByCategoryIdAsync(request.CategoryId, cancellationToken))
            .Where(o => o.CreatedBy != "AutoSync")
            .ToList();

        foreach (var o in manual)
        {
            _overrideRepo.Delete(o);
            await _changeLogRepo.AddAsync(new FundCategoryChangeLog
            {
                FundId = o.FundId,
                CategoryId = request.CategoryId,
                Action = CategoryChangeAction.ResetToAuto,
                PerformedBy = "admin",
                Reason = "Reset category to auto",
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ResetCategoryOverridesResultDto("Category reset to auto", manual.Count);
    }
}
