using MediatR;
using Meezan.Application.Common.Exceptions;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Commands.ResetCategoryOverrides;

public sealed class ResetCategoryOverridesCommandHandler : IRequestHandler<ResetCategoryOverridesCommand, ResetCategoryOverridesResultDto>
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

    public async Task<ResetCategoryOverridesResultDto> Handle(ResetCategoryOverridesCommand request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepo.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category == null)
        {
            throw new NotFoundException(nameof(FundCategory), request.CategoryId);
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
