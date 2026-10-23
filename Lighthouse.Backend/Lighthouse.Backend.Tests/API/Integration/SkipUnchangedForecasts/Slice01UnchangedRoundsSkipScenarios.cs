using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;

namespace Lighthouse.Backend.Tests.API.Integration.SkipUnchangedForecasts
{
    /// <summary>
    /// At 10:15 the periodic refresh of Team "FSS" finds nothing the forecast of "Payments Platform" reads has
    /// changed since 10:00. The forecast is not computed again: the round says so, Refresh History records a
    /// skipped forecast, and the dates stay as they are. Any change to an input, and the "Update forecast"
    /// buttons, still forecast in full.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-6354-skip-unchanged-forecasts")]
    [Category("slice-01")]
    [Category("us-01")]
    public class Slice01UnchangedRoundsSkipScenarios : SkipUnchangedForecastsAcceptanceTest
    {
        private static readonly DateTimeOffset TenFifteen = TenOClock + AQuarterOfAnHour;

        private static readonly DateTimeOffset TenThirty = TenFifteen + AQuarterOfAnHour;

        private static readonly DateTimeOffset TenFortyFive = TenThirty + AQuarterOfAnHour;

        [Test]
        [Ignore(Pending)]
        [Category("walking_skeleton")]
        [Category("driving_port")]
        [Category("kpi-OUT-6354-unchanged-rounds-skip")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task A_background_round_that_changed_nothing_skips_the_forecast_and_says_so()
        {
            GivenPaymentsPlatformAsTheTrackerHoldsIt();
            await GivenPaymentsPlatformWasForecastAt(TenOClock);
            var datesAtTen = TheForecastValuesOfPaymentsPlatform();

            var quietRound = await TheTeamRoundAt(TenFifteen, FssId);

            ThenNothingWasSimulated(quietRound, "Nothing the forecast reads changed since 10:00");

            var skipped = quietRound.NewForecastRows[^1];
            var line = TheLineOfTheTeamRound(quietRound, Fss);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(line, Does.Match(ASkippedForecastOnTheLine),
                    $"The round's summary line does not say the forecast was skipped. It read: {line ?? "(no line)"}");
                Assert.That(Bool(skipped, "success"), Is.True,
                    "A skipped forecast is a successful one; an older web app shows it as a short successful run.");
                Assert.That(Bool(skipped, "cancelled"), Is.False);
                Assert.That(TheForecastValuesOfPaymentsPlatform(), Is.EqualTo(datesAtTen),
                    "A skipped forecast changed the forecasts it confirmed.");
                Assert.That(TheBrowserWasTold.LastStatusOf(UpdateType.Forecasts, PaymentsPlatformId), Is.EqualTo(UpdateProgress.Completed),
                    "The Task Manager row of a skipped forecast has to end the way any finished forecast does.");
            }
        }

        public enum InputChange
        {
            TheDayHasTurned,
            AnItemLeftATeamsQuery,
            ATeamsFeatureWipMovedFromTwoToThree,
            ATeamsThroughputWindowGrew,
            ABlackoutTookADayOutOfATeamsThroughput,
            TheForecastFilterNowExcludesItems,
            ATeamNoLongerHasEnoughData,
            ThePortfoliosDefaultFeatureSizeChanged,
            AFeatureGainedADependency,
            TwoFeaturesSwappedOrder,
            AFeatureJoinedThePortfolio,
            AFeatureLeftThePortfolio,
            ATeamJoinedThePortfolio,
            ATeamLeftThePortfolio,
        }

        [TestCase(InputChange.TheDayHasTurned)]
        [TestCase(InputChange.AnItemLeftATeamsQuery)]
        [TestCase(InputChange.ATeamsFeatureWipMovedFromTwoToThree)]
        [TestCase(InputChange.ATeamsThroughputWindowGrew)]
        [TestCase(InputChange.ABlackoutTookADayOutOfATeamsThroughput)]
        [TestCase(InputChange.TheForecastFilterNowExcludesItems)]
        [TestCase(InputChange.ATeamNoLongerHasEnoughData)]
        [TestCase(InputChange.ThePortfoliosDefaultFeatureSizeChanged)]
        [TestCase(InputChange.AFeatureGainedADependency)]
        [TestCase(InputChange.TwoFeaturesSwappedOrder)]
        [TestCase(InputChange.AFeatureJoinedThePortfolio)]
        [TestCase(InputChange.AFeatureLeftThePortfolio)]
        [TestCase(InputChange.ATeamJoinedThePortfolio)]
        [TestCase(InputChange.ATeamLeftThePortfolio)]
        [Ignore(Pending)]
        [Category("driving_port")]
        [Category("kpi-OUT-6354-no-stale-forecast")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task A_background_round_after_an_input_changed_forecasts_in_full(InputChange change)
        {
            GivenPaymentsPlatformAsTheTrackerHoldsIt();
            await GivenPaymentsPlatformWasForecastAt(TenOClock);
            await GivenAQuietRoundSkippedTheForecastAt(TenFifteen);

            var nextRound = await TheNextBackgroundRoundAfter(change);

            ThenPaymentsPlatformWasForecastInFull(nextRound, $"{change} since the last forecast");
        }

        /// <summary>
        /// A fresh instance, and one just upgraded into this release, has nothing confirmed for any Portfolio.
        /// Its first round computes, and that computation is what a later quiet round can be compared with.
        /// </summary>
        [Test]
        [Ignore(Pending)]
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

        [Test]
        [Ignore(Pending)]
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

            Assert.That(failedRound.NewForecastRows.Select(row => Bool(row, "success")), Has.Some.False,
                "The forecast this scenario breaks did not fail, so it says nothing about a failed one.");
            ThenPaymentsPlatformWasForecastInFull(nextRound, "The last forecast failed");
        }

        [Test]
        [Ignore(Pending)]
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

        [TestCase(UserPress.UpdateForecastOfThePortfolio)]
        [TestCase(UserPress.UpdateForecastsOfATeamsPortfolios)]
        [Ignore(Pending)]
        [Category("driving_port")]
        [Category("contract-shape:bounded-change")]
        public async Task Update_forecast_always_forecasts_in_full(UserPress press)
        {
            GivenPaymentsPlatformAsTheTrackerHoldsIt();
            await GivenPaymentsPlatformWasForecastAt(TenOClock);
            await GivenAQuietRoundSkippedTheForecastAt(TenFifteen);

            var pressed = await WhenTheOperatorPresses(press);

            ThenPaymentsPlatformWasForecastInFull(pressed, $"The operator pressed {press}");
        }

        /// <summary>
        /// A forecast running when the button is pressed read its inputs before the press. It must not record
        /// them over what the press cleared, or the forecast the press is owed would be confirmed instead of
        /// computed. With another forecast already queued, the press joins that one, and it is that one which
        /// has to compute.
        /// </summary>
        [TestCase(false)]
        [TestCase(true)]
        [Ignore(Pending)]
        [Category("driving_port")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task Update_forecast_pressed_while_a_forecast_runs_still_ends_in_a_computed_forecast(bool anotherIsAlreadyWaiting)
        {
            GivenPaymentsPlatformAsTheTrackerHoldsIt();
            await GivenPaymentsPlatformWasForecastAt(TenOClock);
            await GivenAQuietRoundSkippedTheForecastAt(TenFifteen);
            Tracker.NoLongerShowsTheTeam(Fss, "FSS-3");
            var rowsBeforeThePress = (await TheForecastRowsOfPaymentsPlatform()).Count;

            AForecastIsUnderWayAfterTheRoundAt(TenThirty);
            if (anotherIsAlreadyWaiting)
            {
                AnotherForecastIsAlreadyWaitingBehindIt();
            }

            await Press(UserPress.UpdateForecastOfThePortfolio);
            await TheRunningForecastIsLetGo();

            var afterThePress = (await TheForecastRowsOfPaymentsPlatform()).Skip(rowsBeforeThePress).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(afterThePress, Has.Count.GreaterThanOrEqualTo(2),
                    $"The forecast the press is owed never ran after the one that was running. Rows: {Describe(afterThePress)}");
                Assert.That(Bool(afterThePress[^1], "skipped"), Is.False,
                    $"The forecast after the press was confirmed instead of computed. Rows: {Describe(afterThePress)}");
            }
        }

        /// <summary>
        /// A skip is still a forecast round: the Delivery publishing and the daily snapshot follow it as they
        /// follow a computed one, and the snapshot of the day is kept up to date rather than added to.
        /// </summary>
        [Test]
        [Ignore(Pending)]
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
        [Ignore(Pending)]
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

        // --- The change, and the background round that brings it in ---

        private Task<ObservedRounds> TheNextBackgroundRoundAfter(InputChange change)
        {
            switch (change)
            {
                case InputChange.TheDayHasTurned:
                    return TheTeamRoundAt(new DateTimeOffset(2026, 10, 24, 0, 10, 0, TimeSpan.Zero), FssId);
                case InputChange.AnItemLeftATeamsQuery:
                    Tracker.NoLongerShowsTheTeam(Fss, "FSS-3");
                    return TheTeamRoundAt(TenThirty, FssId);
                case InputChange.ATeamsFeatureWipMovedFromTwoToThree:
                    ChangeTheTeam(FssId, team => team.FeatureWIP = 3);
                    return TheTeamRoundAt(TenThirty, FssId);
                case InputChange.ATeamsThroughputWindowGrew:
                    TheTeamDelivers(FssId, [.. FssDelivers, .. FssDelivers]);
                    return TheTeamRoundAt(TenThirty, FssId);
                case InputChange.ABlackoutTookADayOutOfATeamsThroughput:
                    TheTeamDelivers(FssId, FssDelivers[..^1]);
                    return TheTeamRoundAt(TenThirty, FssId);
                case InputChange.TheForecastFilterNowExcludesItems:
                    TheTeamDelivers(FssId, [.. FssDelivers.Select(day => Math.Max(0, day - 1))], filterApplied: true, excludedSummary: "4 Items excluded: Bug");
                    return TheTeamRoundAt(TenThirty, FssId);
                case InputChange.ATeamNoLongerHasEnoughData:
                    TheTeamDelivers(FssId, FssDelivers, hasSufficientData: false);
                    return TheTeamRoundAt(TenThirty, FssId);
                case InputChange.ThePortfoliosDefaultFeatureSizeChanged:
                    ChangeThePortfolio(PaymentsPlatformId, portfolio => portfolio.DefaultAmountOfWorkItemsPerFeature = 30);
                    return ThePortfolioRoundAt(TenThirty);
                case InputChange.AFeatureGainedADependency:
                    TheFeatureNowWaitsFor("PAY-90", "PAY-88");
                    return TheTeamRoundAt(TenThirty, FssId);
                case InputChange.TwoFeaturesSwappedOrder:
                    Tracker.Reorders(PaymentsPlatform, "PAY-88", "2");
                    Tracker.Reorders(PaymentsPlatform, "PAY-90", "1");
                    return ThePortfolioRoundAt(TenThirty);
                case InputChange.AFeatureJoinedThePortfolio:
                    Tracker.HoldsFeature(PaymentsPlatform, "PAY-99", "Chargebacks", order: "4");
                    Tracker.HoldsItems(Fss, "PAY-99", "FSS-9");
                    return TheBackgroundCycleAt(TenThirty, FssId);
                case InputChange.AFeatureLeftThePortfolio:
                    Tracker.NoLongerHoldsFeature(PaymentsPlatform, "PAY-97");
                    return ThePortfolioRoundAt(TenThirty);
                case InputChange.ATeamJoinedThePortfolio:
                    Tracker.HoldsItems(Orion, "PAY-88", "ORN-1", "ORN-2");
                    return TheBackgroundCycleAt(TenThirty, OrionId);
                case InputChange.ATeamLeftThePortfolio:
                    Tracker.NoLongerShowsTheTeam(Atlas);
                    return TheBackgroundCycleAt(TenThirty, AtlasId);
                default:
                    throw new ArgumentOutOfRangeException(nameof(change), change, null);
            }
        }

        private void TheThroughputOfFssCanBeReadAgain() => TheTeamDelivers(FssId, FssDelivers);
    }
}
