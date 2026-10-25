using Lighthouse.Backend.Data;
using Lighthouse.Backend.Health;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Tests.API.Integration.ResponsiveDuringUpdates;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lighthouse.Backend.Tests.API.Integration.FastWithManyFeatures
{
    /// <summary>
    /// One fixture per host size, because how many forecast threads a round may use follows from the cores: two
    /// give it one thread, four give it two, eight give it four. Whether pages then answer fast enough is read
    /// off the measurement harness rather than asserted; these scenarios pin where the round's work runs.
    /// </summary>
    [TestFixture(2)]
    [TestFixture(4)]
    [TestFixture(8)]
    [Category("acceptance")]
    [Category("epic-6400-fast-with-many-features")]
    [Category("slice-01")]
    [Category("us-01")]
    [Category("story-6404")]
    public class Slice01ForecastRoundsRunOnTheirOwnThreadsScenarios(int coresTheHostHas) : ForecastWhileItUpdatesAcceptanceTest
    {
        private static readonly TimeSpan TheLongestAStopMayTake = TimeSpan.FromSeconds(30);

        private ThreadsAForecastRoundRanOn round = null!;

        private int TheForecastThreads => Math.Max(1, coresTheHostHas / 2);

        protected override void ConfigureAdditionalServices(IServiceCollection services)
        {
            base.ConfigureAdditionalServices(services);

            round = new ThreadsAForecastRoundRanOn(Draws);

            services.RemoveAll<IDrawStreamFactory>();
            services.AddSingleton<IDrawStreamFactory>(round);

            services.RemoveAll<ForecastSimulationLimits>();
            services.AddSingleton(new ForecastSimulationLimits(TheRunsAForecastMakes, TheDaysARunMayCover) { CoresSeen = coresTheHostHas });

            services.ConfigureDbContext<LighthouseAppContext>(options => options.AddInterceptors(round));

            // The test host drops every hosted service; this one is put back so that stopping the host drains the
            // update queue as it does in production.
            services.AddHostedService<GracefulShutdownService>();
        }

        [Test]
        [Category("driving_port")]
        [Category("kpi-OUT-6400-no-stall-during-rounds")]
        [Category("contract-shape:bounded-change")]
        public async Task A_background_round_simulates_on_Lighthouses_own_forecast_threads_never_on_the_threads_that_answer_pages()
        {
            var portfolio = GivenAPortfolioWithOneFeatureOfFiveItems();

            await WhenABackgroundRoundRunsFor(portfolio);

            var threads = round.TheThreadsItDrewOn;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(threads, Is.Not.Empty, "The round simulated nothing, so this proved nothing.");
                Assert.That(threads, Has.None.Matches<ThreadsAForecastRoundRanOn.SeenThread>(thread => thread.OnTheSharedPool),
                    "The round simulated on the threads that answer pages: " + string.Join("; ", threads));
                Assert.That(threads, Has.All.Matches<ThreadsAForecastRoundRanOn.SeenThread>(thread => thread.InTheBackground),
                    "A forecast thread must never hold the process open when Lighthouse shuts down: " + string.Join("; ", threads));
                Assert.That(threads, Has.Count.LessThanOrEqualTo(TheForecastThreads),
                    $"A forecast may use {TheForecastThreads} of {coresTheHostHas} cores, so the round has no more threads than that: " + string.Join("; ", threads));
            }
        }

        [Test]
        [Category("driving_port")]
        [Category("kpi-OUT-6400-no-stall-during-rounds")]
        [Category("contract-shape:bounded-change")]
        public async Task A_background_round_saves_its_forecasts_from_the_forecast_threads_too()
        {
            var portfolio = GivenAPortfolioWithOneFeatureOfFiveItems();

            await WhenABackgroundRoundRunsFor(portfolio);

            var threads = round.TheThreadsItSavedForecastsOn;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(threads, Is.Not.Empty, "The round saved no forecast, so this proved nothing.");
                Assert.That(threads, Has.None.Matches<ThreadsAForecastRoundRanOn.SeenThread>(thread => thread.OnTheSharedPool),
                    "The round saved its forecasts from the threads that answer pages: " + string.Join("; ", threads));
            }
        }

        [Test]
        [Category("driving_port")]
        [Category("error")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task A_round_stopped_from_the_Task_Manager_ends_and_every_Feature_keeps_the_forecast_it_had()
        {
            var portfolio = GivenAPortfolioWithOneFeatureOfFiveItems();
            await GivenThePortfolioWasForecastBefore(portfolio);
            var yesterdays = TheStoredForecastOf(portfolio);
            round.Forget();

            WhenThePortfolioForecastIsUnderWay(portfolio);
            await WhenTheOperatorStopsTheForecastOf(portfolio);
            await TheForecastIsLetGoAndSettles(portfolio);

            Assert.That(round.TheThreadsItDrewOn, Is.Not.Empty, "The stopped round had not simulated anything, so where it ran proves nothing.");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheStoredForecastOf(portfolio), Is.EqualTo(yesterdays), "A stopped round replaced the forecast the Feature had.");
                Assert.That(round.TheThreadsItDrewOn, Has.None.Matches<ThreadsAForecastRoundRanOn.SeenThread>(thread => thread.OnTheSharedPool),
                    "The stopped round had been simulating on the threads that answer pages: " + string.Join("; ", round.TheThreadsItDrewOn));
            }
        }

        [Test]
        [Category("driving_port")]
        [Category("error")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task When_Lighthouse_stops_in_the_middle_of_a_round_the_round_ends_and_every_Feature_keeps_the_forecast_it_had()
        {
            var portfolio = GivenAPortfolioWithOneFeatureOfFiveItems();
            await GivenThePortfolioWasForecastBefore(portfolio);
            var yesterdays = TheStoredForecastOf(portfolio);
            GivenRunsThatFinish(inEveryTen: 7);

            WhenThePortfolioForecastIsUnderWay(portfolio);
            var lighthouseStops = WhenLighthouseIsStopped();
            Draws.LetItCarryOn();

            Assert.That(lighthouseStops.Wait(TheLongestAStopMayTake), Is.True,
                $"Lighthouse did not finish stopping within {TheLongestAStopMayTake.TotalSeconds}s of a round being under way.");
            WhenLighthouseStartsAgain();

            Assert.That(TheStoredForecastOf(portfolio), Is.EqualTo(yesterdays),
                "The round under way when Lighthouse stopped saved a new forecast, so it was finished rather than ended.");
        }

        private Task WhenABackgroundRoundRunsFor(SeededPortfolio portfolio)
            => RunUpdate(sp => sp.GetRequiredService<IForecastUpdater>().TriggerUpdate(portfolio.Id));

        /// <summary>Stops the host the way a service manager does; the round is still held when this returns.</summary>
        private Task WhenLighthouseIsStopped()
        {
            var host = Factory;
            return Task.Run(host.Dispose);
        }

        /// <summary>A fresh host on the same database, so what the stop left behind is read as the next start finds it.</summary>
        private void WhenLighthouseStartsAgain() => Factory = RootFactory.WithWebHostBuilder(_ => { });
    }
}
