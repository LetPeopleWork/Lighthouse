namespace Lighthouse.Backend.Tests.API.Integration.SkipUnchangedForecasts
{
    /// <summary>
    /// Maria presses a button to make sure. "Update forecast" is the press that always gets her a forecast
    /// computed from scratch, even with forecasts already running or waiting. A refresh button forgets what
    /// was confirmed too, so the next forecast of each Portfolio it touches computes; a forecast that
    /// starts between the press and the refreshed data can use that up, and the forecasts after it may be
    /// confirmed again, which is right whenever their inputs are the ones that forecast computed from.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-6354-skip-unchanged-forecasts")]
    [Category("slice-01")]
    [Category("us-01")]
    [Category("us-02")]
    public class Slice01UserPressScenarios : SkipUnchangedForecastsAcceptanceTest
    {
        private static readonly DateTimeOffset TenFifteen = TenOClock + AQuarterOfAnHour;

        private static readonly DateTimeOffset TenThirty = TenFifteen + AQuarterOfAnHour;

        [TestCase(UserPress.UpdateForecastOfThePortfolio)]
        [TestCase(UserPress.UpdateForecastsOfATeamsPortfolios)]
        [TestCase(UserPress.RefreshOfATeam)]
        [TestCase(UserPress.RefreshAllTeams)]
        [TestCase(UserPress.RefreshOfThePortfolio)]
        [TestCase(UserPress.RefreshAllPortfolios)]
        [Category("driving_port")]
        [Category("contract-shape:bounded-change")]
        public async Task A_press_forecasts_in_full_even_when_nothing_changed(UserPress press)
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
        /// The forecast the first press caused started after that press and read what it left. A second press
        /// during that forecast is owed a computed forecast of its own, so the running one may not record what
        /// it read over the second press, however little the first press left.
        /// </summary>
        [Test]
        [Category("driving_port")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task Update_forecast_pressed_again_while_the_forecast_of_the_first_press_runs_ends_in_a_computed_forecast()
        {
            GivenPaymentsPlatformAsTheTrackerHoldsIt();
            await GivenPaymentsPlatformWasForecastAt(TenOClock);
            await GivenAQuietRoundSkippedTheForecastAt(TenFifteen);
            var rowsBeforeThePresses = (await TheForecastRowsOfPaymentsPlatform()).Count;

            await AForecastIsUnderWayAfterThePressAt(TenThirty, UserPress.UpdateForecastOfThePortfolio);
            await Press(UserPress.UpdateForecastOfThePortfolio);
            await TheRunningForecastIsLetGo();

            var afterThePresses = (await TheForecastRowsOfPaymentsPlatform()).Skip(rowsBeforeThePresses).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(afterThePresses, Has.Count.GreaterThanOrEqualTo(2),
                    $"The forecast the second press is owed never ran after the one the first press caused. Rows: {Describe(afterThePresses)}");
                Assert.That(Bool(afterThePresses[^1], "skipped"), Is.False,
                    $"The forecast after the second press was confirmed instead of computed. Rows: {Describe(afterThePresses)}");
            }
        }

        /// <summary>
        /// Refresh all with a forecast of Payments Platform already running and another waiting behind it. The
        /// press forgets what was confirmed, so the first forecast to start after it computes. Whether the ones
        /// after that compute too is not promised: they may be confirmed against what that forecast computed
        /// from, which is correct whenever their inputs are unchanged since. Only "Update forecast" promises a
        /// computed forecast after everything that was already under way.
        /// </summary>
        [Test]
        [Category("driving_port")]
        [Category("edge")]
        [Category("contract-shape:bounded-change")]
        public async Task Refresh_all_with_forecasts_already_under_way_computes_the_next_forecast_to_start_and_may_confirm_the_later_ones()
        {
            GivenPaymentsPlatformAsTheTrackerHoldsIt();
            await GivenPaymentsPlatformWasForecastAt(TenOClock);
            await GivenAQuietRoundSkippedTheForecastAt(TenFifteen);
            Tracker.NoLongerShowsTheTeam(Fss, "FSS-3");
            var rowsBeforeThePress = (await TheForecastRowsOfPaymentsPlatform()).Count;

            AForecastIsUnderWayAfterTheRoundAt(TenThirty);
            AnotherForecastIsAlreadyWaitingBehindIt();

            await Press(UserPress.RefreshAllTeams);
            await TheRunningForecastIsLetGo();

            var afterThePress = (await TheForecastRowsOfPaymentsPlatform()).Skip(rowsBeforeThePress).ToList();

            var theFirstToStartAfterThePress = afterThePress.Skip(1).Select(row => Bool(row, "skipped")).FirstOrDefault();

            Assert.That(theFirstToStartAfterThePress, Is.False,
                $"The first forecast to start after the press was confirmed instead of computed, or none started. Rows: {Describe(afterThePress)}");
        }
    }
}
