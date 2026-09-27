namespace Lighthouse.Backend.Tests.API.Integration.ForecastRealityCheck
{
    /// <summary>
    /// Acceptance scenarios for the Forecast Reality Check once its answer opens in a dialog whose table prints,
    /// once per period, what the Team actually delivered. The dialog, its words and its table are the browser's
    /// and are pinned in the Team Forecast view's own tests; what these
    /// pin is the one fact the table needs and the answer did not carry - every period's actual, including a
    /// period in which no sampling window could be checked, whose checks carry no actual of their own.
    ///
    /// Driving port: the check itself, over HTTP. Step definitions live in
    /// Slice04TheAnswerOpensInADialogSpecifications.cs.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-4172-forecast-backtest-sweep")]
    [Category("slice-04")]
    public partial class Slice04TheAnswerOpensInADialogTest
    {
        // @driving_port @real-io @us-04 @slice-04 @kpi-OUT-6094-how-far-each-forecast-landed @contract-shape:pure-function
        [Test]
        public async Task Every_period_the_check_scored_is_in_the_answer_with_its_days_and_what_the_Team_delivered()
        {
            var oceanExplorer = GivenOceanExplorerFinishingAWorkItemEveryDay();

            var answer = await TheAnswerTo(oceanExplorer);

            using (Assert.EnterMultipleScope())
            {
                ThenThereIsOnePeriodPerHorizonInHorizonOrder(answer);
                ThenEachPeriodEndsTodayAndHoldsExactlyItsHorizon(answer);
                ThenEachPeriodSaysTheTeamDeliveredOneWorkItemForEachOfItsDays(answer);
                ThenEveryCheckWasScoredOnTheDaysOfItsPeriod(answer);
            }
        }

        // @driving_port @real-io @us-04 @slice-04 @kpi-OUT-6094-how-far-each-forecast-landed @contract-shape:pure-function
        [Test]
        public async Task Every_check_that_could_be_evaluated_carries_the_same_actual_as_its_period()
        {
            var coastalSurvey = GivenCoastalSurveyWhichFinishesWorkInBursts();

            var answer = await TheAnswerTo(coastalSurvey);

            using (Assert.EnterMultipleScope())
            {
                ThenThereIsOnePeriodPerHorizonInHorizonOrder(answer);
                ThenEveryEvaluatedCheckCarriesItsPeriodsActual(answer);
                ThenEveryCheckThatCouldNotRunStillCarriesNoActualOfItsOwn(answer);
            }
        }

        /// <summary>
        /// The case that made the field necessary: nothing could be checked, so no check carries an actual,
        /// and yet the table still prints what the Team delivered in each period.
        /// </summary>
        // @driving_port @real-io @us-04 @slice-04 @error @kpi-OUT-6094-how-far-each-forecast-landed @contract-shape:pure-function
        [Test]
        public async Task A_Team_whose_history_supports_no_check_still_gets_four_periods_each_with_what_it_delivered()
        {
            var newlyFormed = GivenATeamThatHasFinishedAlmostNothing();

            var answer = await TheAnswerTo(newlyFormed);

            using (Assert.EnterMultipleScope())
            {
                ThenThereIsOnePeriodPerHorizonInHorizonOrder(answer);
                ThenThePeriodsSayTheTeamDelivered(answer, TheNewlyFormedTeamsActualsByHorizon);
                ThenEveryCheckThatCouldNotRunStillCarriesNoActualOfItsOwn(answer);
                ThenNoCheckCarriesAnActual(answer);
            }
        }

        // @driving_port @real-io @us-04 @slice-04 @boundary @kpi-OUT-6094-how-far-each-forecast-landed @contract-shape:pure-function
        [Test]
        public async Task A_week_in_which_the_Team_delivered_nothing_is_a_period_whose_actual_is_zero_not_missing()
        {
            var harbourPilots = GivenHarbourPilotsWhoFinishedNothingLastWeek();

            var answer = await TheAnswerTo(harbourPilots);

            using (Assert.EnterMultipleScope())
            {
                ThenThePeriodsSayTheTeamDelivered(answer, TheHarbourPilotsActualsByHorizon);
                ThenEveryEvaluatedCheckCarriesItsPeriodsActual(answer);
            }
        }

        /// <summary>
        /// A period is a fact about a horizon, never about a sampling window: a Team whose own window adds a
        /// fifth to the ladder gets twenty checks and still four periods, so nothing in the answer is a figure
        /// kept per window.
        /// </summary>
        // @driving_port @real-io @us-04 @slice-04 @boundary @kpi-OUT-6094-no-window-ranked @contract-shape:pure-function
        [Test]
        public async Task A_Team_whose_own_window_is_off_the_ladder_gets_one_period_per_horizon_never_one_per_window()
        {
            var team = GivenATeamFinishingAWorkItemEveryDaySetTo(45);

            var answer = await TheAnswerTo(team);

            using (Assert.EnterMultipleScope())
            {
                ThenTwentyChecksWereRun(answer);
                ThenThereIsOnePeriodPerHorizonInHorizonOrder(answer);
                ThenEveryEvaluatedCheckCarriesItsPeriodsActual(answer);
            }
        }
    }
}
