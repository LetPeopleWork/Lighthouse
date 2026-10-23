using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Dependencies;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.API;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.Extensions.Logging;
using Lighthouse.Backend.Tests.TestHelpers;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Forecast
{
    /// <summary>
    /// The production simulation over Teams whose measured history the test chose, with the runs, the
    /// days a run may cover and the draw source handed in. Delivery history is an input here rather than
    /// closed Work Items, so a scenario that says "one item, then nothing" asserts the simulation and not
    /// the metrics windowing in front of it.
    /// </summary>
    internal sealed class OnePortfolioForecast
    {
        private readonly Mock<ITeamMetricsService> teamMetrics = new();

        private readonly Mock<IRepository<Feature>> features = new();

        public static ForecastSimulationLimits Limits(int trials, int mostDays, int cores = 4)
            => new(trials, mostDays) { CoresSeen = cores };

        public Team ATeamThatDelivers(int id, int[] eachDay, int featureWip = 1)
        {
            var team = new Team { Id = id, Name = $"Team {id}", FeatureWIP = featureWip };
            var history = new RunChartData(RunChartDataGenerator.GenerateRunChartData(eachDay));

            teamMetrics
                .Setup(service => service.GetForecastThroughputStatus(team, It.IsAny<ThroughputFilterMode>()))
                .Returns(new ForecastThroughputStatus(history, false, null));

            return team;
        }

        public Team ATeamThatHasNeverDeliveredAnything(int id)
        {
            var team = new Team { Id = id, Name = $"Team {id}", FeatureWIP = 1 };

            teamMetrics
                .Setup(service => service.GetForecastThroughputStatus(team, It.IsAny<ThroughputFilterMode>()))
                .Returns(new ForecastThroughputStatus(new RunChartData(), false, null, HasSufficientData: false));

            return team;
        }

        public static Feature AFeature(int id, string name, string order, params (Team Team, int Remaining)[] work)
            => new(work.Select(entry => (entry.Team, entry.Remaining, entry.Remaining)))
            {
                Id = id,
                Name = name,
                ReferenceId = name.ToUpperInvariant(),
                Order = order,
            };

        public async Task<List<Feature>> RunsOver(IDrawStreamFactory draws, ForecastSimulationLimits limits, params Feature[] forecast)
            => await RunsOver(draws, limits, new NothingWaitsForAnything(), forecast);

        public async Task<List<Feature>> RunsOver(IDrawStreamFactory draws, ForecastSimulationLimits limits, IWhatTheForecastWaitsFor waits, params Feature[] forecast)
        {
            var all = forecast.ToList();
            features.Holds(all);

            var portfolio = new Portfolio { Id = 1, Name = "Portfolio" };
            portfolio.UpdateFeatures(all);

            await TheSimulation(draws, limits, waits).UpdateForecastsForPortfolio(portfolio, DateOnly.MinValue);

            return all;
        }

        public Task<WhenForecast> AManualForecast(IDrawStreamFactory draws, ForecastSimulationLimits limits, Team team, int remaining)
            => TheSimulation(draws, limits, new NothingWaitsForAnything()).When(team, remaining);

        private ForecastService TheSimulation(IDrawStreamFactory draws, ForecastSimulationLimits limits, IWhatTheForecastWaitsFor waits)
            => new(
                new RandomNumberService(),
                Mock.Of<ILogger<ForecastService>>(),
                teamMetrics.Object,
                features.Object,
                waits,
                new ForecastSimulation(draws, limits),
                TimeProvider.System);
    }
}
