using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.API;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Forecast
{
    /// <summary>
    /// Where a forecast's simulated runs happen follows whoever started it: a round started on Lighthouse's own
    /// forecast threads draws every run there, and a forecast started anywhere else draws on the shared pool.
    /// A team that delivers one item every day finishes twelve items on day twelve, so a forecast that reads
    /// twelve was simulated in full before it was handed back.
    /// </summary>
    public class ForecastServiceSchedulerTest
    {
        private const int TheRemainingItems = 12;

        private const string TheNameEveryLaneThreadCarries = "Lighthouse forecast";

        private Mock<ITeamMetricsService> teamMetricsServiceMock;

        [SetUp]
        public void Setup()
        {
            teamMetricsServiceMock = new Mock<ITeamMetricsService>();
        }

        [TestCase(2)]
        [TestCase(4)]
        [TestCase(8)]
        public async Task Started_on_a_scheduler_of_its_own_every_simulated_run_draws_on_that_schedulers_threads(int cores)
        {
            var limits = new ForecastSimulationLimits(400, 60) { CoresSeen = cores };
            using var draws = new CountedDrawStream();
            var round = new ThreadsAForecastRoundRanOn(draws);
            using var lane = new ForecastLane(limits);
            var subject = CreateSubject(round, limits);
            var team = ATeamThatDeliversOneItemADay();

            var forecast = await Task.Factory.StartNew(
                () => subject.When(team, TheRemainingItems),
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                lane.Scheduler).Unwrap();

            var threads = round.TheThreadsItDrewOn;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(forecast.GetProbability(50), Is.EqualTo(TheRemainingItems), "The forecast was handed back before its runs were done.");
                Assert.That(threads, Is.Not.Empty);
                Assert.That(threads, Has.None.Matches<ThreadsAForecastRoundRanOn.SeenThread>(thread => thread.OnTheSharedPool),
                    "A run drew on the shared pool: " + string.Join("; ", threads));
                Assert.That(threads, Has.All.Matches<ThreadsAForecastRoundRanOn.SeenThread>(thread => thread.Name != null && thread.Name.StartsWith(TheNameEveryLaneThreadCarries, StringComparison.Ordinal)),
                    "A run drew off the forecast threads: " + string.Join("; ", threads));
                Assert.That(threads, Has.Count.LessThanOrEqualTo(limits.MostRunsAtOnce));
            }
        }

        [Test]
        public async Task Started_anywhere_else_every_simulated_run_draws_on_the_shared_pool()
        {
            var limits = new ForecastSimulationLimits(400, 60) { CoresSeen = 4 };
            using var draws = new CountedDrawStream();
            var round = new ThreadsAForecastRoundRanOn(draws);
            var subject = CreateSubject(round, limits);

            var forecast = await subject.When(ATeamThatDeliversOneItemADay(), TheRemainingItems);

            var threads = round.TheThreadsItDrewOn;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(forecast.GetProbability(50), Is.EqualTo(TheRemainingItems), "The forecast was handed back before its runs were done.");
                Assert.That(threads, Is.Not.Empty);
                Assert.That(threads, Has.All.Matches<ThreadsAForecastRoundRanOn.SeenThread>(thread => thread.OnTheSharedPool),
                    "A run drew off the shared pool: " + string.Join("; ", threads));
            }
        }

        private ForecastService CreateSubject(ThreadsAForecastRoundRanOn round, ForecastSimulationLimits limits)
        {
            return new ForecastService(
                new NotSoRandomNumberService(),
                Mock.Of<ILogger<ForecastService>>(),
                teamMetricsServiceMock.Object,
                Mock.Of<IRepository<Feature>>(),
                Mock.Of<IPortfolioRepository>(),
                new NothingWaitsForAnything(),
                new ForecastSimulation(round, limits),
                TimeProvider.System);
        }

        private Team ATeamThatDeliversOneItemADay()
        {
            var team = new Team { Name = "Team", FeatureWIP = 1, Id = 1 };

            var runChart = new RunChartData(RunChartDataGenerator.GenerateRunChartData([1]));
            teamMetricsServiceMock.Setup(x => x.GetForecastThroughputStatus(team, ThroughputFilterMode.RespectTeamSetting)).Returns(new ForecastThroughputStatus(runChart, false, null));

            return team;
        }
    }
}
