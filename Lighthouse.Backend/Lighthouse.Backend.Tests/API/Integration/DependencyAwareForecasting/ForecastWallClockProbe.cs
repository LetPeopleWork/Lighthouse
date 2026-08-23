using System.Diagnostics;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.API.Integration.DependencyAwareForecasting
{
    /// <summary>
    /// How long a forecast of the benchmark Portfolio takes on the machine it is run on. Run by hand,
    /// before and after the simulation is restructured, and the two numbers written into the slice brief.
    ///
    /// It times the Portfolio at the number of simulated runs the product ships with, which is more than
    /// the tests around it use. The question it answers is what the change costs a customer, so the numbers
    /// it prints are not comparable with how long those tests take.
    ///
    /// It is deliberately not an assertion. A wall clock recorded on one machine says nothing on another,
    /// and a test that compared against a number checked in from somebody's laptop would fail in CI for a
    /// reason that is not a defect - which is what happened the one time a bound was set from a number
    /// taken here. The guard that does run everywhere lives beside it and bounds the run only against a
    /// forecast that is stuck, leaving the question of whether one got dearer to this measurement.
    /// </summary>
    [TestFixture]
    [Category("epic-5792-dependency-aware-forecasting")]
    [Category("slice-02")]
    public class ForecastWallClockProbe
    {
        private const int RunsPerMeasurement = 3;

        [Test]
        [Explicit("Measures wall clock. Run by hand, on one machine, before and after the change.")]
        public async Task MeasureForecastWallClock()
        {
            var timings = new List<long>();

            for (var run = 0; run < RunsPerMeasurement; run++)
            {
                timings.Add(await TimeOneForecast());
            }

            await TestContext.Out.WriteLineAsync(
                $"Benchmark Portfolio forecast: {string.Join(" ms, ", timings)} ms " +
                $"(fastest {timings.Min()} ms, median {timings.Order().ElementAt(timings.Count / 2)} ms)");

            Assert.That(timings, Is.Not.Empty);
        }

        private static async Task<long> TimeOneForecast()
        {
            var benchmark = new BenchmarkPortfolio().Build();

            var forecastService = new ForecastService(
                new RandomNumberService(),
                Mock.Of<ILogger<ForecastService>>(),
                benchmark.TeamMetrics,
                benchmark.FeatureRepository,
                new NothingWaitsForAnything(),
                new DrawsAfreshEveryTime(),
                ForecastSimulationLimits.Default);

            var stopwatch = Stopwatch.StartNew();
            await forecastService.UpdateForecastsForPortfolio(benchmark.Portfolio);
            stopwatch.Stop();

            return stopwatch.ElapsedMilliseconds;
        }
    }
}
