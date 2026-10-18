using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.API.Integration.ResponsiveDuringUpdates
{
    /// <summary>
    /// Ingrid presses stop on "Forecasting Portfolio 'Platform Modernisation' - Running". No further
    /// simulated run starts, the row ends as cancelled, and the Features keep the dates they had, in
    /// Lighthouse and in the tracker.
    ///
    /// The forecast is held part-way, stopped through the same route the Task Manager row uses, and let
    /// go. "Promptly" is then a count rather than a duration: the runs begun afterwards are at most the
    /// ones already in flight, one per worker.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6311-responsive-during-updates")]
    [Category("slice-02")]
    [Category("us-02")]
    public class Slice02StopEndsAForecastScenarios : ForecastWhileItUpdatesAcceptanceTest
    {
        [Test]
        [Ignore(PendingEpic6311.Slice02)]
        [Category("ac-02.1")]
        [Category("ac-02.2")]
        [Category("walking_skeleton")]
        [Category("driving_port")]
        [Category("kpi-OUT-6311-KPI-2")]
        [Category("contract-shape:bounded-change")]
        public async Task A_stopped_forecast_starts_no_further_run_and_its_row_ends_as_cancelled()
        {
            var portfolio = GivenAPortfolioWithOneFeatureOfFiveItems();

            WhenThePortfolioForecastIsUnderWay(portfolio);
            var begunWhenStopped = Draws.RunsBegun;
            await WhenTheOperatorStopsTheForecastOf(portfolio);
            await TheForecastIsLetGoAndSettles(portfolio);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Draws.RunsBegun, Is.LessThanOrEqualTo(begunWhenStopped + HalfTheCores),
                    $"The forecast went on starting runs after it was stopped: {begunWhenStopped} when stopped, {Draws.RunsBegun} in the end.");
                Assert.That(TheBrowserWasTold.LastStatusOf(UpdateType.Forecasts, portfolio.Id), Is.EqualTo(UpdateProgress.Cancelled),
                    $"The row did not end as cancelled. The browser was told: {TheBrowserWasTold.Describe()}");
            }
        }

        [Test]
        [Ignore(PendingEpic6311.Slice02)]
        [Category("ac-02.3")]
        [Category("error")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task The_Features_keep_the_forecast_they_had_before_the_stopped_run()
        {
            var portfolio = GivenAPortfolioWithOneFeatureOfFiveItems();
            await GivenThePortfolioWasForecastBefore(portfolio);
            var before = TheStoredForecastOf(portfolio);

            GivenRunsThatFinish(inEveryTen: 3);
            WhenThePortfolioForecastIsUnderWay(portfolio);
            await WhenTheOperatorStopsTheForecastOf(portfolio);
            await TheForecastIsLetGoAndSettles(portfolio);

            Assert.That(TheStoredForecastOf(portfolio), Is.EqualTo(before),
                "A stopped forecast replaced the dates the Features already had.");
        }

        [Test]
        [Ignore(PendingEpic6311.Slice02)]
        [Category("ac-02.4")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task A_stopped_forecast_sends_the_tracker_no_forecast_dates()
        {
            var portfolio = GivenAPortfolioWithOneFeatureOfFiveItems();
            GivenTheForecastIsWrittenBackToTheTracker(portfolio);
            await GivenThePortfolioWasForecastBefore(portfolio);
            var sentByTheForecastThatFinished = TheForecastDatesTheTrackerReceived();

            ConnectorMock.Invocations.Clear();
            Clock.SetInstant(Now.AddDays(7));
            WhenThePortfolioForecastIsUnderWay(portfolio);
            await WhenTheOperatorStopsTheForecastOf(portfolio);
            await TheForecastIsLetGoAndSettles(portfolio);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(sentByTheForecastThatFinished, Is.Not.Empty,
                    "A forecast that ran to the end sent the tracker no date, so this scenario cannot tell a stop from a quiet tracker.");
                Assert.That(TheForecastDatesTheTrackerReceived(), Is.Empty,
                    "The stopped forecast still sent its dates to the tracker.");
            }
        }

        [Test]
        [Ignore(PendingEpic6311.Slice02)]
        [Category("ac-02.5")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task Refresh_history_records_the_stopped_forecast_as_cancelled()
        {
            var portfolio = GivenAPortfolioWithOneFeatureOfFiveItems();

            WhenThePortfolioForecastIsUnderWay(portfolio);
            await WhenTheOperatorStopsTheForecastOf(portfolio);
            await TheForecastIsLetGoAndSettles(portfolio);

            var recorded = TheRecordedRefreshFor(RefreshType.Forecast, portfolio.Id);

            Assert.That(recorded, Is.Not.Null, "The stopped forecast left no line in the refresh history.");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(recorded!.Cancelled, Is.True);
                Assert.That(recorded.Success, Is.False);
            }
        }

        /// <summary>
        /// A forecast a person asks for on a Team page is not an update and nothing can stop it, so it runs
        /// every run, exactly as before. Already true; kept running as the guard that the stop reaches only
        /// the update it was asked of.
        /// </summary>
        [Test]
        [Category("ac-02.6")]
        [Category("edge")]
        [Category("contract-shape:bounded-change")]
        public async Task A_manual_forecast_outside_any_update_runs_every_one_of_its_runs()
        {
            var portfolio = GivenAPortfolioWithOneFeatureOfFiveItems();

            using (var scope = Factory.Services.CreateScope())
            {
                var team = scope.ServiceProvider.GetRequiredService<IRepository<Team>>().GetById(portfolio.TeamId);
                Assert.That(team, Is.Not.Null);
                await scope.ServiceProvider.GetRequiredService<IForecastService>().When(team!, FiveItems);
            }

            Assert.That(Draws.RunsBegun, Is.EqualTo(TheRunsAForecastMakes));
        }
    }
}
