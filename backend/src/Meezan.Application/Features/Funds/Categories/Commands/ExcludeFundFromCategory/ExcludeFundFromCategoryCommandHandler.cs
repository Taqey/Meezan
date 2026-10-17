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

namespace Meezan.Application.Features.Funds.Categories.Commands.ExcludeFundFromCategory;

public sealed class ExcludeFundFromCategoryCommandHandler : IRequestHandler<ExcludeFundFromCategoryCommand, Result>
{
    private readonly IFundCategoryOverrideRepository _overrideRepo;
    private readonly IFundCategoryChangeLogRepository _changeLogRepo;
    private readonly IUnitOfWork _unitOfWork;

    public ExcludeFundFromCategoryCommandHandler(
        IFundCategoryOverrideRepository overrideRepo,
        IFundCategoryChangeLogRepository changeLogRepo,
        IUnitOfWork unitOfWork)
    {
        _overrideRepo = overrideRepo;
        _changeLogRepo = changeLogRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ExcludeFundFromCategoryCommand request, CancellationToken cancellationToken)
    {
        var existing = await _overrideRepo.GetAsync(request.FundId, request.CategoryId, cancellationToken);
        if (existing != null)
        {
            if (existing.Action == OverrideAction.Include)
            {
                existing.Action = OverrideAction.Exclude;
                _overrideRepo.Update(existing);
            }
        }
        else
        {
            await _overrideRepo.AddAsync(new FundCategoryOverride
            {
                FundId = request.FundId,
                CategoryId = request.CategoryId,
                Action = OverrideAction.Exclude,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "admin"
            }, cancellationToken);
        }

        await _changeLogRepo.AddAsync(new FundCategoryChangeLog
        {
            FundId = request.FundId,
            CategoryId = request.CategoryId,
            Action = CategoryChangeAction.ManualExcluded,
            PerformedBy = "admin",
            Reason = "Manual exclude",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
