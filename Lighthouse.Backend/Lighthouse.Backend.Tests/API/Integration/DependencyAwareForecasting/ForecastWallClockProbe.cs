using System.Diagnostics;
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
    /// It is deliberately not an assertion. A wall clock recorded on one machine says nothing on another,
    /// and a test that compared against a number checked in from somebody's laptop would fail in CI for a
    /// reason that is not a defect. The guard that does run everywhere lives beside it and bounds the run
    /// generously enough to survive a slow agent while still catching a restructure that made the forecast
    /// many times slower.
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
                new DrawsAfreshEveryTime());

            var stopwatch = Stopwatch.StartNew();
            await forecastService.UpdateForecastsForPortfolio(benchmark.Portfolio);
            stopwatch.Stop();

            return stopwatch.ElapsedMilliseconds;
        }
    }
}
