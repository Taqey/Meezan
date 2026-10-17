namespace Meezan.Infrastructure.Tests.Features.JobSettings;

public class JobScheduleAndSettingsTests
{
    private static readonly TimeZoneInfo CairoTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");

    [Fact]
    public void MarketSnapshots_ComputeDelayUntilNextRun_DuringTradingHours_SchedulesTodayAt4PM()
    {
        // Wednesday at 10:00 AM Cairo (08:00 UTC during standard time / 07:00 UTC DST)
        // Let's create a UTC date that is guaranteed to be Wednesday 10:00 Cairo
        var wedCairo = new DateTime(2026, 10, 14, 10, 0, 0); // Wednesday
        var wedUtc = TimeZoneInfo.ConvertTimeToUtc(wedCairo, CairoTimeZone);

        var delay = MarketSnapshotsBackgroundService.ComputeDelayUntilNextRun(wedUtc);

        // Next run is at 16:00 Cairo same day -> 6 hours delay
        Assert.Equal(TimeSpan.FromHours(6), delay);
    }

    [Fact]
    public void MarketSnapshots_ComputeDelayUntilNextRun_After4PM_SchedulesNextTradingDayAt4PM()
    {
        // Tuesday at 17:00 Cairo (5:00 PM)
        var tueCairo = new DateTime(2026, 10, 13, 17, 0, 0); // Tuesday
        var tueUtc = TimeZoneInfo.ConvertTimeToUtc(tueCairo, CairoTimeZone);

        var delay = MarketSnapshotsBackgroundService.ComputeDelayUntilNextRun(tueUtc);

        // Next run is Wednesday at 16:00 Cairo -> exactly 23 hours later
        Assert.Equal(TimeSpan.FromHours(23), delay);
    }

    [Fact]
    public void MarketSnapshots_ComputeDelayUntilNextRun_OnThursdayAfter4PM_SkipsFridayAndSaturday()
    {
        // Thursday at 16:30 Cairo
        var thuCairo = new DateTime(2026, 10, 15, 16, 30, 0); // Thursday
        var thuUtc = TimeZoneInfo.ConvertTimeToUtc(thuCairo, CairoTimeZone);

        var delay = MarketSnapshotsBackgroundService.ComputeDelayUntilNextRun(thuUtc);

        // Friday and Saturday are skipped. Next run is Sunday at 16:00 Cairo.
        // Thursday 16:30 -> Sunday 16:00 = 2 days, 23 hours, 30 minutes
        var expected = (new DateTime(2026, 10, 18, 16, 0, 0) - thuCairo);
        Assert.Equal(expected, delay);
    }

    [Fact]
    public void MarketSnapshots_ComputeDelayUntilNextRun_OnFriday_SchedulesSundayAt4PM()
    {
        // Friday at 12:00 Cairo
        var friCairo = new DateTime(2026, 10, 16, 12, 0, 0); // Friday
        var friUtc = TimeZoneInfo.ConvertTimeToUtc(friCairo, CairoTimeZone);

        var delay = MarketSnapshotsBackgroundService.ComputeDelayUntilNextRun(friUtc);

        // Friday 12:00 -> Sunday 16:00 = 2 days and 4 hours = 52 hours
        var expected = (new DateTime(2026, 10, 18, 16, 0, 0) - friCairo);
        Assert.Equal(expected, delay);
    }
}
