using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Dependencies;
using Lighthouse.Backend.Tests.TestHelpers;
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
    /// This is the baseline the rest of slice 02 is held to. Every commit has to reproduce it exactly -
    /// putting every Team on one clock, and then running the simulated runs side by side, are both meant to
    /// leave every date where it is, and re-recording the file to make a red test go away throws away the
    /// only thing standing under either change.
    ///
    /// It has been re-recorded once since it was first written, and not for that reason. The benchmark
    /// Portfolio was made smaller and given a lower trial count, because the slice was costing minutes of
    /// CI for more precision than its assertions need. Changing how much work a simulation does moves every
    /// date by construction, so no implementation, right or wrong, could have reproduced the old file.
    /// That is the only kind of re-record there is an honest reason for: the workload changed and the
    /// forecast did not.
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

        internal static IDrawStreamFactory PinnedDraws => new DrawsFromAPinnedStartingNumber(TheStartingNumberTheBaselineWasRecordedFrom);

        internal static Task<BaselinePercentiles[]> ForecastTheBenchmarkPortfolio()
            => ForecastTheBenchmarkPortfolio(PinnedDraws);

        internal static Task<BaselinePercentiles[]> ForecastTheBenchmarkPortfolio(IDrawStreamFactory draws)
            => ForecastTheBenchmarkPortfolio(draws, _ => new NothingWaitsForAnything());

        internal static async Task<BaselinePercentiles[]> ForecastTheBenchmarkPortfolio(
            IDrawStreamFactory draws, Func<IReadOnlyList<Feature>, IWhatTheForecastWaitsFor> whatWaitsForWhat)
        {
            var benchmark = new BenchmarkPortfolio().Build();

            var forecastService = new ForecastService(
                new RandomNumberService(),
                Mock.Of<ILogger<ForecastService>>(),
                benchmark.TeamMetrics,
                benchmark.FeatureRepository,
                whatWaitsForWhat(benchmark.Features),
                draws,
                BenchmarkPortfolio.Limits);

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

        /// <summary>
        /// A chain of waits down the Portfolio, so that every simulated day of every run has to work out
        /// what may be started. It is the heaviest shape the readiness check meets, which is what makes it
        /// the one worth timing.
        /// </summary>
        internal static IWhatTheForecastWaitsFor EachFeatureWaitsOnTheOneBefore(IReadOnlyList<Feature> features)
        {
            var waits = new Waits();

            for (var index = 1; index < features.Count; index++)
            {
                waits.And(features[index].ReferenceId, features[index - 1].ReferenceId);
            }

            return new WaitsHandedStraightToTheForecast(waits);
        }

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
