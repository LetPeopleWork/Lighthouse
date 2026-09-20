using System.Diagnostics;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.ForecastedStartDates
{
    /// <summary>
    /// How long the product's own forecast takes over fifty Features, at the number of simulated runs it
    /// ships with. Run by hand on one machine before the recorder is added and again after, and the two
    /// numbers written into the slice brief. AC-1.8's budget is the second within 110% of the first.
    ///
    /// The whole Epic was scoped on the premise that recording the day a Feature starts is nearly free,
    /// and this is the busiest loop in the product - the cheapness is reasoned, not measured, until this
    /// runs. If the budget is missed the shape changes rather than the feature dying: record the day for
    /// the top rows of each Team only, since the ones deep in the order have the least trustworthy start
    /// dates anyway.
    ///
    /// Deliberately not an assertion, for the reason the probe beside it in the dependency-aware slice
    /// gives: a wall clock recorded on one machine says nothing on another, and a bound checked in from
    /// somebody's laptop fails CI for something that is not a defect.
    /// </summary>
    [TestFixture]
    [Category("epic-6033-forecasted-start-dates")]
    [Category("slice-01")]
    public class StartForecastWallClockProbe
    {
        private const int Features = 50;

        private const int Teams = 5;

        private const int RemainingItemsPerFeature = 12;

        private const int RunsPerMeasurement = 3;

        // @probe @us-01 @contract-shape:unbounded-preservation (AC-1.8)
        [Test]
        [Explicit("Measures wall clock. Run by hand, on one machine, before and after the change.")]
        public async Task MeasureFiftyFeatureForecastWallClock()
        {
            var timings = new List<long>();

            for (var run = 0; run < RunsPerMeasurement; run++)
            {
                timings.Add(await TimeOneForecast());
            }

            await TestContext.Out.WriteLineAsync(
                $"Fifty-Feature Portfolio forecast at {ForecastSimulationLimits.Default.Trials} runs: " +
                $"{string.Join(" ms, ", timings)} ms " +
                $"(fastest {timings.Min()} ms, median {timings.Order().ElementAt(timings.Count / 2)} ms)");

            Assert.That(timings, Is.Not.Empty);
        }

        private static async Task<long> TimeOneForecast()
        {
            var teamMetrics = new Mock<ITeamMetricsService>();
            var featureRepository = new Mock<IRepository<Feature>>();

            var teams = Enumerable.Range(0, Teams).Select(index => ATeamThatDelivers(index, teamMetrics)).ToArray();

            var features = Enumerable
                .Range(0, Features)
                .Select(index => new Feature(teams[index % Teams], RemainingItemsPerFeature)
                {
                    Id = index + 1,
                    ReferenceId = $"PROBE-{index + 1}",
                    Name = $"Probe Feature {index + 1}",
                    Order = $"{(index + 1) * 10}",
                })
                .ToList();

            featureRepository.Setup(repository => repository.GetAll()).Returns(features);

            var portfolio = new Portfolio { Id = 1, Name = "Probe Portfolio" };
            portfolio.UpdateFeatures(features);

            var forecastService = new ForecastService(
                new RandomNumberService(),
                Mock.Of<ILogger<ForecastService>>(),
                teamMetrics.Object,
                featureRepository.Object,
                new NothingWaitsForAnything(),
                new DrawsAfreshEveryTime(),
                ForecastSimulationLimits.Default);

            var stopwatch = Stopwatch.StartNew();
            await forecastService.UpdateForecastsForPortfolio(portfolio);
            stopwatch.Stop();

            return stopwatch.ElapsedMilliseconds;
        }

        /// <summary>
        /// Histories that differ between Teams on purpose: a run lasts as long as its slowest Team, and
        /// Teams that all deliver alike would hide that.
        /// </summary>
        private static Team ATeamThatDelivers(int index, Mock<ITeamMetricsService> teamMetrics)
        {
            var team = new Team
            {
                Id = index + 1,
                Name = $"Probe Team {index + 1}",
                FeatureWIP = 1 + (index % 3),
            };

            var history = Enumerable.Range(0, 30).Select(day => (day + index) % (2 + (index % 4))).ToArray();
            var throughput = new RunChartData(RunChartDataGenerator.GenerateRunChartData(history));

            teamMetrics
                .Setup(service => service.GetForecastThroughputStatus(team, It.IsAny<ThroughputFilterMode>()))
                .Returns(new ForecastThroughputStatus(throughput, false, null));

            return team;
        }
    }
}
