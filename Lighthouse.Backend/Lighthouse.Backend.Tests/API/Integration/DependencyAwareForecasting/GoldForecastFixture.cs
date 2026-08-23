using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.API.Integration.DependencyAwareForecasting
{
    // The one Portfolio the gold percentiles were taken from, and the only way to reproduce them.
    // Both the capture that wrote the gold file and the assertion that compares against it run this
    // same code, so the two can never drift apart: change anything here and both sides change together.
    internal sealed class GoldForecastFixture
    {
        // The fake random number service returns these values verbatim, ignoring the upper bound the
        // caller asked for, and the forecast draws twice: once to pick a day from the throughput
        // history, then once per closed item to pick which Feature that item came from. A value too
        // large for the second draw indexes past the end of the remaining-Features list and throws.
        // A single repeated value avoids that but makes every trial identical, which collapses all
        // four percentiles onto one number and pins nothing about the shape of the distribution.
        // This sequence satisfies both. Day 0 of the throughput history below is empty, so a 0 is
        // harmless wherever it lands, and each non-zero value is followed by exactly as many zeros as
        // that day has items, so every Feature draw reads a 0.
        // Width comes from the seven runs having seven different lengths. A trial starts wherever the
        // previous one stopped, so it begins part-way into a run of empty days and loses a different
        // number of them before the first day that delivers anything. That start position cycles
        // with period seven, and that is what gives the four percentiles four distinct values
        // instead of collapsing them onto one.
        private static readonly int[] GoldDrawSequence =
        [
            7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            4, 0, 0, 0, 0, 0, 0, 0,
            3, 0, 0, 0, 0, 0,
            2, 0, 0, 0,
            1, 0, 0,
        ];

        private static readonly int[] GoldThroughputHistory = [0, 2, 3, 5, 7, 11, 13, 17];

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
        };

        private const string GoldFileName = "slice-00-baseline-percentiles.json";

        private readonly Mock<IRepository<Feature>> featureRepositoryMock = new();
        private readonly Mock<ITeamMetricsService> teamMetricsServiceMock = new();

        internal static IReadOnlyList<int> DrawSequence => GoldDrawSequence;

        internal Task<GoldPercentiles[]> ForecastTheGoldPortfolio()
            => ForecastTheGoldPortfolio(new DrawsFromARecordedSequence(GoldDrawSequence));

        internal async Task<GoldPercentiles[]> ForecastTheGoldPortfolio(IDrawStreamFactory draws)
        {
            // One Team, not two. The forecast runs each Team's simulation on its own task, and they
            // would draw concurrently from the single counter inside the fake random number service,
            // so the interleaving — and with it the captured numbers — would differ between runs.
            var team = CreateTeam(1, "Gold Team", 3, GoldThroughputHistory);

            var features = new[]
            {
                CreateFeature(1, "GOLD-1", [(team, 62)]),
                CreateFeature(2, "GOLD-2", [(team, 83)]),
                CreateFeature(3, "GOLD-3", [(team, 15)]),
                CreateFeature(4, "GOLD-4", [(team, 73)]),
            };

            featureRepositoryMock.Setup(x => x.GetAll()).Returns(features);

            var portfolio = new Portfolio
            {
                Id = 1,
                Name = "Gold Portfolio",
                UpdateTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            };
            portfolio.UpdateFeatures(features);

            var forecastService = new ForecastService(new NotSoRandomNumberService(), Mock.Of<ILogger<ForecastService>>(), teamMetricsServiceMock.Object, featureRepositoryMock.Object, new NothingWaitsForAnything(), draws, ForecastSimulationLimits.Default);

            await forecastService.UpdateForecastsForPortfolio(portfolio);

            return features
                .Select(feature => new GoldPercentiles(
                    feature.ReferenceId,
                    feature.Forecast.GetProbability(50),
                    feature.Forecast.GetProbability(70),
                    feature.Forecast.GetProbability(85),
                    feature.Forecast.GetProbability(95)))
                .ToArray();
        }

        internal static GoldForecastSet ReadGoldSet()
        {
            var path = GoldFilePath();

            if (!File.Exists(path))
            {
                Assert.Fail($"No gold forecast set at {path}. Capture one from the released build before asserting against it.");
            }

            return JsonSerializer.Deserialize<GoldForecastSet>(File.ReadAllText(path), JsonOptions)
                ?? throw new InvalidOperationException($"The gold forecast set at {path} is empty.");
        }

        internal static async Task<string> WriteGoldSet(string capturedFrom, GoldPercentiles[] percentiles)
        {
            var goldSet = new GoldForecastSet(capturedFrom, GoldDrawSequence, percentiles);

            var path = GoldFilePath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(goldSet, JsonOptions));

            return path;
        }

        private Team CreateTeam(int id, string name, int featureWip, int[] throughput)
        {
            var team = new Team
            {
                Id = id,
                Name = name,
                FeatureWIP = featureWip,
            };

            var runChart = new RunChartData(RunChartDataGenerator.GenerateRunChartData(throughput));
            teamMetricsServiceMock
                .Setup(x => x.GetForecastThroughputStatus(team, ThroughputFilterMode.RespectTeamSetting))
                .Returns(new ForecastThroughputStatus(runChart, false, null));

            return team;
        }

        private static Feature CreateFeature(int id, string referenceId, (Team team, int remainingItems)[] remainingWork)
        {
            return new Feature(remainingWork.Select(work => (work.team, work.remainingItems, work.remainingItems)))
            {
                Id = id,
                ReferenceId = referenceId,
                Name = referenceId,
            };
        }

        // The gold file is read from, and written to, the source tree rather than the build output, so
        // that a capture run rewrites the copy that is under version control.
        private static string GoldFilePath()
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

            return Path.Combine(directory.FullName, "API", "Integration", "DependencyAwareForecasting", "gold", GoldFileName);
        }

        internal sealed record GoldPercentiles(string ReferenceId, int P50, int P70, int P85, int P95);

        internal sealed record GoldForecastSet(string CapturedFrom, int[] RandomNumberSequence, GoldPercentiles[] Features);
    }
}
