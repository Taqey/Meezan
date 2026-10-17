using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Meezan.Infrastructure.Services.BackgroundServices;

public class FundSyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<FundSyncBackgroundService> _logger;
    private readonly bool _schedulerEnabled;
    private Timer? _timer;

    public FundSyncBackgroundService(
        IServiceProvider services,
        ILogger<FundSyncBackgroundService> logger,
        IConfiguration configuration)
    {
        _services = services;
        _logger = logger;

        // Funds:SchedulerEnabled is OFF in appsettings.Development.json so a local
        // run can never sync on its own. Production leaves it unset → true.
        _schedulerEnabled = configuration.GetValue<bool?>("Funds:SchedulerEnabled") ?? true;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_schedulerEnabled)
        {
            _logger.LogWarning(
                "Fund sync scheduler is DISABLED by configuration (Funds:SchedulerEnabled=false). Manual sync only.");
            return;
        }

        await ScheduleNextRun(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }

    private async Task ScheduleNextRun(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var settingsRepo = scope.ServiceProvider.GetRequiredService<IFundSyncSettingsRepository>();
        var settings = await settingsRepo.GetAsync(cancellationToken);

        if (settings == null || !settings.IsEnabled)
        {
            _logger.LogInformation("Fund sync is disabled. Checking again in 1 hour.");
            _timer = new Timer(_ => _ = ScheduleNextRun(cancellationToken), null, TimeSpan.FromHours(1), Timeout.InfiniteTimeSpan);
            return;
        }

        var tz = ResolveCairoTimeZone();
        var nowCairo = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        var nextRun = nowCairo.Date.Add(settings.RunTimeCairo.ToTimeSpan());

        if (nextRun <= nowCairo)
        {
            nextRun = nextRun.AddDays(1);
        }

        var delay = nextRun - nowCairo;
        _logger.LogInformation("Fund sync scheduled for {NextRun} Cairo time (in {Delay})", nextRun, delay);

        _timer = new Timer(async _ => await RunSyncAsync(cancellationToken), null, delay, Timeout.InfiniteTimeSpan);
    }

    private async Task RunSyncAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var syncService = scope.ServiceProvider.GetRequiredService<IFundSyncService>();

        try
        {
            await syncService.RunSyncAsync("Scheduled", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fund sync background job failed");
        }
        finally
        {
            await ScheduleNextRun(cancellationToken);
        }
    }

    /// <summary>
    /// "Africa/Cairo" on Linux/Windows-with-IANA, "Egypt Standard Time" on Windows
    /// without ICU. Resolving explicitly avoids a hard crash at schedule time.
    /// </summary>
    public static TimeZoneInfo ResolveCairoTimeZone()
    {
        foreach (var id in new[] { "Africa/Cairo", "Egypt Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.Utc;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Dispose();
        await base.StopAsync(cancellationToken);
    }
}
