using Lighthouse.Backend.Models.Events;

namespace Lighthouse.Backend.Tests.API.Integration.SkipUnchangedForecasts
{
    /// <summary>
    /// A skip is still a forecast round: the Delivery publishing and the day's snapshot follow it as they
    /// follow a computed one. And the first round of each instance day computes, so the snapshot of that
    /// day is recorded from a forecast of that day.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-6354-skip-unchanged-forecasts")]
    [Category("slice-01")]
    [Category("us-01")]
    public class Slice01AnnouncedAndDailyScenarios : SkipUnchangedForecastsAcceptanceTest
    {
        private static readonly DateTimeOffset TenFifteen = TenOClock + AQuarterOfAnHour;

        [Test]
        [Category("driving_port")]
        [Category("contract-shape:bounded-change")]
        public async Task A_skipped_forecast_is_still_announced_and_the_days_snapshot_is_not_doubled()
        {
            GivenPaymentsPlatformAsTheTrackerHoldsIt();
            var q4Release = GivenTheQ4ReleaseIsTrackedOnPaymentsPlatform();
            await GivenPaymentsPlatformWasForecastAt(TenOClock);
            RaisedSignals.Clear();

            var quietRound = await TheTeamRoundAt(TenFifteen, FssId);

            ThenNothingWasSimulated(quietRound, "Nothing the forecast reads changed since 10:00");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(RaisedSignals.Of<PortfolioForecastsUpdated>().Count(raised => raised.PortfolioId == PaymentsPlatformId), Is.EqualTo(1),
                    "A skipped forecast has to tell the Delivery publishing and the daily snapshot that the round finished.");
                Assert.That(TheSnapshotsOfTheDay(q4Release, new DateOnly(2026, 10, 23)), Is.EqualTo(1),
                    "The day's snapshot of the Q4 Release was recorded twice.");
            }
        }

        [Test]
        [Category("driving_port")]
        [Category("edge")]
        [Category("contract-shape:bounded-change")]
        public async Task The_first_round_of_a_day_forecasts_in_full_and_records_that_days_snapshot()
        {
            GivenPaymentsPlatformAsTheTrackerHoldsIt();
            var q4Release = GivenTheQ4ReleaseIsTrackedOnPaymentsPlatform();
            var twentyThreeForty = new DateTimeOffset(2026, 10, 23, 23, 40, 0, TimeSpan.Zero);
            await GivenPaymentsPlatformWasForecastAt(twentyThreeForty);
            await GivenAQuietRoundSkippedTheForecastAt(twentyThreeForty + AQuarterOfAnHour);

            var firstRoundOfTheDay = await TheTeamRoundAt(new DateTimeOffset(2026, 10, 24, 0, 10, 0, TimeSpan.Zero), FssId);

            ThenPaymentsPlatformWasForecastInFull(firstRoundOfTheDay, "It is the first round of 2026-10-24");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheSnapshotsOfTheDay(q4Release, new DateOnly(2026, 10, 24)), Is.EqualTo(1));
                Assert.That(TheSnapshotsOfTheDay(q4Release, new DateOnly(2026, 10, 23)), Is.EqualTo(1));
            }
        }
    }
}
