namespace Lighthouse.Backend.Tests.API.Integration.SkipUnchangedForecasts
{
    /// <summary>
    /// When in doubt the forecast computes. A forecast that failed or was stopped confirmed nothing, and an
    /// instance that was started again cannot know what another build did to the forecasts in between, so
    /// the next round after any of these forecasts "Payments Platform" in full.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-6354-skip-unchanged-forecasts")]
    [Category("slice-01")]
    [Category("us-01")]
    public class Slice01UnfinishedRunsAndRestartScenarios : SkipUnchangedForecastsAcceptanceTest
    {
        private static readonly DateTimeOffset TenFifteen = TenOClock + AQuarterOfAnHour;

        private static readonly DateTimeOffset TenThirty = TenFifteen + AQuarterOfAnHour;

        private static readonly DateTimeOffset TenFortyFive = TenThirty + AQuarterOfAnHour;

        [Test]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task After_a_forecast_that_failed_the_next_quiet_round_forecasts_in_full()
        {
            GivenPaymentsPlatformAsTheTrackerHoldsIt();
            await GivenPaymentsPlatformWasForecastAt(TenOClock);
            await GivenAQuietRoundSkippedTheForecastAt(TenFifteen);

            ReadingTheThroughputOfTheTeamFails(FssId);
            var failedRound = await TheTeamRoundAt(TenThirty, FssId);
            TheThroughputOfFssCanBeReadAgain();
            var nextRound = await TheTeamRoundAt(TenFortyFive, FssId);

            ThenTheForecastOfTheRoundFailed(failedRound);
            ThenPaymentsPlatformWasForecastInFull(nextRound, "The last forecast failed");
        }

        [Test]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task After_a_forecast_that_was_stopped_the_next_quiet_round_forecasts_in_full()
        {
            GivenPaymentsPlatformAsTheTrackerHoldsIt();
            await GivenPaymentsPlatformWasForecastAt(TenOClock);
            await GivenAQuietRoundSkippedTheForecastAt(TenFifteen);

            Tracker.NoLongerShowsTheTeam(Fss, "FSS-3");
            AForecastIsUnderWayAfterTheRoundAt(TenThirty);
            await WhenTheOperatorStopsTheRunningForecast();
            await TheRunningForecastIsLetGo();
            var nextRound = await TheTeamRoundAt(TenFortyFive, FssId);

            ThenPaymentsPlatformWasForecastInFull(nextRound, "The last forecast was stopped before it finished");
        }

        /// <summary>
        /// Upgraded, rolled back and rolled forward the same day, or restarted: in between, another build may
        /// have computed forecasts without recording what from. The first round after the start computes.
        /// </summary>
        [Test]
        [Category("edge")]
        [Category("contract-shape:bounded-change")]
        public async Task After_the_instance_starts_again_the_next_quiet_round_forecasts_in_full()
        {
            GivenPaymentsPlatformAsTheTrackerHoldsIt();
            await GivenPaymentsPlatformWasForecastAt(TenOClock);
            await GivenAQuietRoundSkippedTheForecastAt(TenFifteen);

            WhenTheInstanceStartsAgain();
            var firstRoundAfterTheStart = await TheTeamRoundAt(TenThirty, FssId);

            ThenPaymentsPlatformWasForecastInFull(firstRoundAfterTheStart, "The instance was started again since the last forecast");
        }

        private void TheThroughputOfFssCanBeReadAgain() => TheTeamDelivers(FssId, FssDelivers);
    }
}
