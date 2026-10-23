using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;

namespace Lighthouse.Backend.Tests.API.Integration.SkipUnchangedForecasts
{
    /// <summary>
    /// At 10:15 the periodic refresh of Team "FSS" scans its items, downloads none, and finds nothing the
    /// forecast of "Payments Platform" reads has changed since 10:00. The forecast is not computed again: the
    /// round says so, Refresh History records a skipped forecast, and the dates stay as they are.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-6354-skip-unchanged-forecasts")]
    [Category("slice-01")]
    [Category("us-01")]
    public class Slice01QuietRoundsSkipScenarios : SkipUnchangedForecastsAcceptanceTest
    {
        private static readonly DateTimeOffset TenFifteen = TenOClock + AQuarterOfAnHour;

        [Test]
        [Category("walking_skeleton")]
        [Category("driving_port")]
        [Category("kpi-OUT-6354-unchanged-rounds-skip")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task A_background_round_that_changed_nothing_skips_the_forecast_and_says_so()
        {
            GivenTheTrackerCanBeScanned();
            GivenPaymentsPlatformAsTheTrackerHoldsIt();
            await GivenPaymentsPlatformWasForecastAt(TenOClock);
            var datesAtTen = TheForecastValuesOfPaymentsPlatform();

            var quietRound = await TheTeamRoundAt(TenFifteen, FssId);

            ThenNothingWasSimulated(quietRound, "Nothing the forecast reads changed since 10:00");

            var skipped = quietRound.NewForecastRows[^1];
            var line = TheLineOfTheTeamRound(quietRound, Fss);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(line, Does.Contain(" | mode=Delta | ").And.Contain(" | fetched=0 | "),
                    $"The round is not the reporter's kind, one that scanned and downloaded nothing. It read: {line ?? "(no line)"}");
                Assert.That(line, Does.Match(ASkippedForecastOnTheLine),
                    $"The round's summary line does not say the forecast was skipped. It read: {line ?? "(no line)"}");
                Assert.That(Bool(skipped, "success"), Is.True,
                    "A skipped forecast is a successful one; an older web app shows it as a short successful run.");
                Assert.That(Bool(skipped, "cancelled"), Is.False);
                Assert.That(TheForecastValuesOfPaymentsPlatform(), Is.EqualTo(datesAtTen),
                    "A skipped forecast changed the forecasts it confirmed.");
            }

            await ThenTheTaskManagerShowsTheForecastFinished();
        }

        /// <summary>
        /// A fresh instance, and one just upgraded into this release, has nothing confirmed for any Portfolio.
        /// Its first round computes, and that computation is what a later quiet round can be compared with.
        /// </summary>
        [Test]
        [Category("edge")]
        [Category("contract-shape:bounded-change")]
        public async Task A_Portfolio_with_nothing_confirmed_yet_is_forecast_in_full_and_the_next_quiet_round_skips()
        {
            GivenPaymentsPlatformAsTheTrackerHoldsIt();

            var firstCycle = await TheBackgroundCycleAt(TenOClock, FssId, AtlasId);
            var quietRound = await TheTeamRoundAt(TenFifteen, FssId);

            ThenPaymentsPlatformWasForecastInFull(firstCycle, "Nothing had been confirmed for this Portfolio");
            ThenNothingWasSimulated(quietRound, "The first round's forecast is current and nothing changed since");
        }
    }
}
