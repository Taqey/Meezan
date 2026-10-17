using Meezan.Application.Common.Results;
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

namespace Meezan.Application.Features.Funds.Classification.Commands.ResetFundOverrides;

public sealed class ResetFundOverridesCommandHandler : IRequestHandler<ResetFundOverridesCommand, Result<ResetFundOverridesResultDto>>
{
    private readonly IFundRepository _fundRepo;
    private readonly IFundCategoryOverrideRepository _overrideRepo;
    private readonly IFundCategoryChangeLogRepository _changeLogRepo;
    private readonly IUnitOfWork _unitOfWork;

    public ResetFundOverridesCommandHandler(
        IFundRepository fundRepo,
        IFundCategoryOverrideRepository overrideRepo,
        IFundCategoryChangeLogRepository changeLogRepo,
        IUnitOfWork unitOfWork)
    {
        _fundRepo = fundRepo;
        _overrideRepo = overrideRepo;
        _changeLogRepo = changeLogRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ResetFundOverridesResultDto>> Handle(ResetFundOverridesCommand request, CancellationToken cancellationToken)
    {
        var fund = await _fundRepo.GetByIdAsync(request.FundId, cancellationToken);
        if (fund == null)
        {
            return Error.NotFound("FundNotFound", "");
        }

        var manual = (await _overrideRepo.GetByFundIdAsync(request.FundId, cancellationToken))
            .Where(o => o.CreatedBy != "AutoSync")
            .ToList();

        foreach (var o in manual)
        {
            _overrideRepo.Delete(o);
            await _changeLogRepo.AddAsync(new FundCategoryChangeLog
            {
                FundId = request.FundId,
                CategoryId = o.CategoryId,
                Action = CategoryChangeAction.ResetToAuto,
                PerformedBy = "admin",
                Reason = "Reset fund to auto",
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ResetFundOverridesResultDto("Fund reset to auto", manual.Count);
    }
}
