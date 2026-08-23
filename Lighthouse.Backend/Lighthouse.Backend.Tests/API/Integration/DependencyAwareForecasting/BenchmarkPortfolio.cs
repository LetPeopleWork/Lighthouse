using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Moq;

namespace Lighthouse.Backend.Tests.API.Integration.DependencyAwareForecasting
{
    /// <summary>
    /// A Portfolio big enough for the time a forecast takes to be worth measuring, and small enough to run
    /// inside a test. What decides how long a run takes is the number of Teams, how much work each of them
    /// has left, and how fast they get through it, so those are what this varies.
    ///
    /// Every timing in this slice is taken over this same builder. A benchmark whose workload drifts
    /// between runs measures the workload rather than the change.
    /// </summary>
    internal sealed class BenchmarkPortfolio
    {
        private const int TeamCount = 6;

        private const int FeaturesPerTeam = 4;

        private const int RemainingItemsPerFeature = 40;

        private readonly Mock<IRepository<Feature>> featureRepository = new();

        private readonly Mock<ITeamMetricsService> teamMetrics = new();

        internal IRepository<Feature> FeatureRepository => featureRepository.Object;

        internal ITeamMetricsService TeamMetrics => teamMetrics.Object;

        internal Portfolio Portfolio { get; private set; } = new();

        internal IReadOnlyList<Feature> Features { get; private set; } = [];

        internal BenchmarkPortfolio Build()
        {
            var teams = Enumerable.Range(0, TeamCount).Select(CreateTeam).ToList();

            var features = new List<Feature>();
            var id = 1;

            foreach (var team in teams)
            {
                for (var index = 0; index < FeaturesPerTeam; index++)
                {
                    features.Add(CreateFeature(id, [(team, RemainingItemsPerFeature)]));
                    id++;
                }
            }

            // One Feature the first two Teams share, so the aggregation over several Teams is timed too
            // rather than only the single-Team path.
            features.Add(CreateFeature(id, [(teams[0], RemainingItemsPerFeature), (teams[1], RemainingItemsPerFeature)]));

            featureRepository.Setup(repository => repository.GetAll()).Returns(features);

            var portfolio = new Portfolio
            {
                Id = 1,
                Name = "Benchmark Portfolio",
                UpdateTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            };
            portfolio.UpdateFeatures(features);

            Portfolio = portfolio;
            Features = features;

            return this;
        }

        /// <summary>
        /// Delivery histories that differ between Teams on purpose: a run lasts as long as its slowest Team,
        /// and Teams that all deliver at the same rate would hide that.
        /// </summary>
        private Team CreateTeam(int index)
        {
            var team = new Team
            {
                Id = index + 1,
                Name = $"Benchmark Team {index + 1}",
                FeatureWIP = 1 + (index % 3),
            };

            var history = Enumerable
                .Range(0, 30)
                .Select(day => (day + index) % (2 + (index % 4)))
                .ToArray();

            var runChart = new RunChartData(RunChartDataGenerator.GenerateRunChartData(history));

            teamMetrics
                .Setup(service => service.GetForecastThroughputStatus(team, ThroughputFilterMode.RespectTeamSetting))
                .Returns(new ForecastThroughputStatus(runChart, false, null));

            return team;
        }

        private static Feature CreateFeature(int id, (Team team, int remainingItems)[] work)
        {
            return new Feature(work.Select(entry => (entry.team, entry.remainingItems, entry.remainingItems)))
            {
                Id = id,
                ReferenceId = $"BENCH-{id}",
                Name = $"Benchmark Feature {id}",
            };
        }
    }
}
