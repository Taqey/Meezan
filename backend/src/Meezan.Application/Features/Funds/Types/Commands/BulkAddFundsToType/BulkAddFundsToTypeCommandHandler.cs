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

namespace Meezan.Application.Features.Funds.Types.Commands.BulkAddFundsToType;

public sealed class BulkAddFundsToTypeCommandHandler : IRequestHandler<BulkAddFundsToTypeCommand, Result<BulkAddFundsToTypeResultDto>>
{
    private readonly IFundTypeDefinitionRepository _typeDefRepo;
    private readonly IFundRepository _fundRepo;
    private readonly IFundTypeOverrideRepository _typeOverrideRepo;
    private readonly IFundTypeChangeLogRepository _changeLogRepo;
    private readonly IUnitOfWork _unitOfWork;

    public BulkAddFundsToTypeCommandHandler(
        IFundTypeDefinitionRepository typeDefRepo,
        IFundRepository fundRepo,
        IFundTypeOverrideRepository typeOverrideRepo,
        IFundTypeChangeLogRepository changeLogRepo,
        IUnitOfWork unitOfWork)
    {
        _typeDefRepo = typeDefRepo;
        _fundRepo = fundRepo;
        _typeOverrideRepo = typeOverrideRepo;
        _changeLogRepo = changeLogRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<BulkAddFundsToTypeResultDto>> Handle(BulkAddFundsToTypeCommand request, CancellationToken cancellationToken)
    {
        var targetType = await _typeDefRepo.GetByIdAsync(request.TypeId, cancellationToken);
        if (targetType == null) return Error.NotFound("FundTypeDefinitionNotFound", "");

        var fundIds = request.FundIds ?? new List<int>();
        var allFunds = await _fundRepo.GetAllAsync(cancellationToken);
        var activeFunds = allFunds.Where(f => fundIds.Contains(f.Id) && f.IsActive).ToList();
        var activeFundIds = activeFunds.Select(f => f.Id).ToHashSet();
        var failed = fundIds.Where(id => !activeFundIds.Contains(id)).ToList();

        var existingOverrides = await _typeOverrideRepo.GetByFundIdsAsync(activeFundIds, cancellationToken);
        var existingByFund = existingOverrides.ToDictionary(o => o.FundId);

        foreach (var fund in activeFunds)
        {
            if (existingByFund.TryGetValue(fund.Id, out var existing))
            {
                existing.FundTypeDefinitionId = targetType.Id;
                existing.CreatedAt = DateTime.UtcNow;
                existing.CreatedBy = "admin";
                _typeOverrideRepo.Update(existing);
            }
            else
            {
                await _typeOverrideRepo.AddAsync(new FundTypeOverride
                {
                    FundId = fund.Id,
                    FundTypeDefinitionId = targetType.Id,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "admin"
                }, cancellationToken);
            }

            await _changeLogRepo.AddAsync(new FundTypeChangeLog
            {
                FundId = fund.Id,
                FundTypeDefinitionId = targetType.Id,
                Action = FundTypeChangeAction.FundOverridden,
                PerformedBy = "admin",
                Reason = $"Bulk added to type '{targetType.NameAr}'",
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new BulkAddFundsToTypeResultDto(activeFunds.Count, failed);
    }
}
