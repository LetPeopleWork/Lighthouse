using System.Collections.Concurrent;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.API;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Forecast
{
    /// <summary>
    /// Where the forecast asks for its numbers, rather than what comes back. The draw source answers by
    /// coordinate, so two draws sharing a coordinate are the same number - and the forecast asks for two
    /// quite different things on a single day of a single Team's work: how much that Team delivered, and
    /// which Feature each delivered item came from.
    ///
    /// Sharing a coordinate between those two would tie a high-delivery day to which Feature received the
    /// work. Nothing about the dates that came out would look wrong.
    /// </summary>
    [TestFixture]
    [Category("epic-5792-dependency-aware-forecasting")]
    [Category("slice-02")]
    public class ForecastDrawCoordinatesTest
    {
        private Mock<IRepository<Feature>> featureRepositoryMock;

        private Mock<ITeamMetricsService> teamMetricsServiceMock;

        [SetUp]
        public void Setup()
        {
            featureRepositoryMock = new Mock<IRepository<Feature>>();
            teamMetricsServiceMock = new Mock<ITeamMetricsService>();
        }

        [Test]
        public async Task NoTwoDrawsInAForecast_AreAskedForAtTheSamePlace()
        {
            var recorded = await TheCoordinatesAForecastAsksFor();

            var askedForTwice = recorded
                .GroupBy(at => at)
                .Where(sharing => sharing.Count() > 1)
                .Select(sharing => sharing.Key)
                .ToList();

            Assert.That(askedForTwice, Is.Empty,
                "Two draws in one forecast were asked for at the same place, so they are the same number. " +
                $"First of {askedForTwice.Count}: {askedForTwice.FirstOrDefault()}");
        }

        [Test]
        public async Task HowMuchATeamDelivered_IsAskedForSomewhereElseThanWhichFeatureGotIt()
        {
            var recorded = await TheCoordinatesAForecastAsksFor();

            var daysWorkedThrough = recorded
                .GroupBy(at => (at.Trial, at.Team, at.Day))
                .Where(day => day.Count() > 1)
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(daysWorkedThrough, Is.Not.Empty,
                    "No simulated day in this fixture drew more than once, so the fixture cannot tell the " +
                    "delivery draw apart from the draws that pick a Feature.");

                Assert.That(
                    daysWorkedThrough.All(day => day.Select(at => at.Ordinal).Distinct().Count() == day.Count()),
                    Is.True,
                    "A simulated day asked for two of its draws at the same position within that day.");
            }
        }

        [Test]
        public async Task EachTeam_IsAskedForItsOwnNumbers()
        {
            var recorded = await TheCoordinatesAForecastAsksFor();

            Assert.That(recorded.Select(at => at.Team).Distinct().Count(), Is.EqualTo(2),
                "The two Teams in this fixture did not draw from two different places, so one Team's numbers " +
                "are the other's.");
        }

        private async Task<List<DrawAskedFor>> TheCoordinatesAForecastAsksFor()
        {
            var recording = new RecordsWhereEveryDrawWasAskedFor();

            var firstTeam = CreateTeam(1, [1, 2]);
            var secondTeam = CreateTeam(2, [1, 2]);

            var features = new List<Feature>
            {
                CreateFeature(1, firstTeam, 6),
                CreateFeature(2, firstTeam, 4),
                CreateFeature(3, secondTeam, 5),
            };

            featureRepositoryMock.Setup(repository => repository.GetAll()).Returns(features);

            var portfolio = new Portfolio { Id = 1, Name = "Portfolio" };
            portfolio.UpdateFeatures(features);

            var forecastService = new ForecastService(
                new RandomNumberService(),
                Mock.Of<ILogger<ForecastService>>(),
                teamMetricsServiceMock.Object,
                featureRepositoryMock.Object,
                new NothingWaitsForAnything(),
                recording,
                ForecastSimulationLimits.Default);

            await forecastService.UpdateForecastsForPortfolio(portfolio);

            return recording.Recorded;
        }

        private Team CreateTeam(int id, int[] throughput)
        {
            var team = new Team { Id = id, Name = $"Team {id}", FeatureWIP = 2 };

            var runChart = new RunChartData(RunChartDataGenerator.GenerateRunChartData(throughput));
            teamMetricsServiceMock
                .Setup(service => service.GetForecastThroughputStatus(team, ThroughputFilterMode.RespectTeamSetting))
                .Returns(new ForecastThroughputStatus(runChart, false, null));

            return team;
        }

        private static Feature CreateFeature(int id, Team team, int remainingItems)
            => new(team, remainingItems)
            {
                Id = id,
                Name = $"Feature {id}",
                ReferenceId = $"F-{id}",
            };

        private sealed record DrawAskedFor(int Trial, int Team, int Day, int Ordinal);

        /// <summary>
        /// Answers every draw the same way and writes down where it was asked for. What comes back does not
        /// matter here; only that no two questions share an address.
        ///
        /// The forecast carries out its simulated runs side by side, so what this writes into has to hold up
        /// under several of them at once. A plain list quietly loses entries and throws, and the test that
        /// results reads like a defect in the forecast.
        /// </summary>
        private sealed class RecordsWhereEveryDrawWasAskedFor : IDrawStreamFactory, IDrawStream
        {
            private readonly ConcurrentQueue<DrawAskedFor> recorded = new();

            internal List<DrawAskedFor> Recorded => [.. recorded];

            public long StartingNumber => 0;

            public IDrawStream ForOneRun() => this;

            public int Draw(int trial, int team, int day, int ordinal, int maxExclusive)
            {
                recorded.Enqueue(new DrawAskedFor(trial, team, day, ordinal));

                return 0;
            }
        }
    }
}
