using System.Text.Json;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.API.Integration.DependencyAwareForecasting
{
    /// <summary>
    /// The percentiles a forecast of the benchmark Portfolio produces from one pinned starting number, and
    /// the file they are written down in.
    ///
    /// This is the baseline the rest of slice 02 is held to. It is recorded once, on the commit that
    /// replaced where the forecast's numbers come from, and every commit after that has to reproduce it
    /// exactly - putting every Team on one clock, and then running the simulated runs side by side, are both
    /// meant to leave every date where it is. Re-recording it to make a failure go away throws away the only
    /// thing standing under those two changes.
    ///
    /// Several Teams on purpose. A single-Team Portfolio cannot tell a shared clock from separate ones.
    /// </summary>
    internal static class SharedClockBaselineFixture
    {
        internal const long TheStartingNumberTheBaselineWasRecordedFrom = 20260824;

        private const string BaselineFileName = "slice-02-shared-clock-percentiles.json";

        private static readonly int[] ThePercentilesLighthouseShows = [50, 70, 85, 95];

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
        };

        internal static Task<BaselinePercentiles[]> ForecastTheBenchmarkPortfolio()
            => ForecastTheBenchmarkPortfolio(new DrawsFromAPinnedStartingNumber(TheStartingNumberTheBaselineWasRecordedFrom));

        internal static async Task<BaselinePercentiles[]> ForecastTheBenchmarkPortfolio(IDrawStreamFactory draws)
        {
            var benchmark = new BenchmarkPortfolio().Build();

            var forecastService = new ForecastService(
                new RandomNumberService(),
                Mock.Of<ILogger<ForecastService>>(),
                benchmark.TeamMetrics,
                benchmark.FeatureRepository,
                new NothingWaitsForAnything(),
                draws,
                ForecastSimulationLimits.Default);

            await forecastService.UpdateForecastsForPortfolio(benchmark.Portfolio);

            return benchmark.Features
                .Select(feature => new BaselinePercentiles(
                    feature.ReferenceId,
                    feature.Forecast.GetProbability(50),
                    feature.Forecast.GetProbability(70),
                    feature.Forecast.GetProbability(85),
                    feature.Forecast.GetProbability(95)))
                .ToArray();
        }

        internal static IReadOnlyList<int> Percentiles => ThePercentilesLighthouseShows;

        internal static BaselineSet ReadBaseline()
        {
            var path = BaselineFilePath();

            if (!File.Exists(path))
            {
                Assert.Fail($"No recorded baseline at {path}. Record one before asserting against it.");
            }

            return JsonSerializer.Deserialize<BaselineSet>(File.ReadAllText(path), JsonOptions)
                ?? throw new InvalidOperationException($"The recorded baseline at {path} is empty.");
        }

        internal static async Task<string> WriteBaseline(string recordedAt, BaselinePercentiles[] percentiles)
        {
            var path = BaselineFilePath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            await File.WriteAllTextAsync(
                path,
                JsonSerializer.Serialize(
                    new BaselineSet(recordedAt, TheStartingNumberTheBaselineWasRecordedFrom, percentiles),
                    JsonOptions));

            return path;
        }

        // Read from and written to the source tree rather than the build output, so a recording run rewrites
        // the copy that is under version control.
        private static string BaselineFilePath()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.Backend.Tests.csproj")))
            {
                directory = directory.Parent;
            }

            if (directory is null)
            {
                throw new InvalidOperationException("Could not locate the test project directory from the test run directory.");
            }

            return Path.Combine(directory.FullName, "API", "Integration", "DependencyAwareForecasting", "gold", BaselineFileName);
        }

        internal sealed record BaselinePercentiles(string ReferenceId, int P50, int P70, int P85, int P95)
        {
            internal int At(int percentile) => percentile switch
            {
                50 => P50,
                70 => P70,
                85 => P85,
                95 => P95,
                _ => throw new ArgumentOutOfRangeException(nameof(percentile), percentile, "Not a percentile Lighthouse shows."),
            };
        }

        internal sealed record BaselineSet(string RecordedAt, long StartingNumber, BaselinePercentiles[] Features);
    }
}
