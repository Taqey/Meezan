using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Commands.UpdateSyncSettings;

public sealed class UpdateSyncSettingsCommandHandler : IRequestHandler<UpdateSyncSettingsCommand, FundSyncSettings>
{
    private readonly IFundSyncSettingsRepository _syncSettingsRepo;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSyncSettingsCommandHandler(
        IFundSyncSettingsRepository syncSettingsRepo,
        IUnitOfWork unitOfWork)
    {
        _syncSettingsRepo = syncSettingsRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<FundSyncSettings> Handle(UpdateSyncSettingsCommand request, CancellationToken cancellationToken)
    {
        var settings = await _syncSettingsRepo.GetAsync(cancellationToken);
        if (settings == null)
        {
            settings = new FundSyncSettings { Id = 1 };
        }

        settings.IsEnabled = request.IsEnabled ?? settings.IsEnabled;
        if (request.RunTimeCairo.HasValue) settings.RunTimeCairo = request.RunTimeCairo.Value;
        settings.PreventOverlap = request.PreventOverlap ?? settings.PreventOverlap;
        settings.RequestDelaySeconds = request.RequestDelaySeconds ?? settings.RequestDelaySeconds;
        settings.MaxRetries = request.MaxRetries ?? settings.MaxRetries;
        settings.RetryBackoffSeconds = request.RetryBackoffSeconds ?? settings.RetryBackoffSeconds;
        if (request.AutoIndexFundTypeEnabled.HasValue) settings.AutoIndexFundTypeEnabled = request.AutoIndexFundTypeEnabled.Value;
        if (request.AutoClassificationEnabled.HasValue) settings.AutoClassificationEnabled = request.AutoClassificationEnabled.Value;
        settings.UpdatedAt = DateTime.UtcNow;
        settings.UpdatedBy = "admin";

        _syncSettingsRepo.Update(settings);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return settings;
    }
}
