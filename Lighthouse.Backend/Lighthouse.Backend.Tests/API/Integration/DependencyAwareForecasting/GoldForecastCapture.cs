using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.API;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.API.Integration.DependencyAwareForecasting
{
    // The numbers in the gold file were produced by the released build, before any change to when a
    // forecast runs. Moving when a forecast runs must not move what it computes, and the only way to
    // show that is to compare against numbers taken before the move. Regenerating this file to make a
    // failure go away destroys the entire point of having it.
    public class GoldForecastCapture
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

        private Mock<IRepository<Feature>> featureRepositoryMock;
        private Mock<ITeamMetricsService> teamMetricsServiceMock;

        [SetUp]
        public void Setup()
        {
            featureRepositoryMock = new Mock<IRepository<Feature>>();
            teamMetricsServiceMock = new Mock<ITeamMetricsService>();
        }

        [Test]
        public async Task PortfolioForecast_GoldFixture_MatchesPercentilesCapturedFromReleasedBuild()
        {
            var gold = ReadGoldSet();

            var actual = await ForecastGoldFixture();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(gold.RandomNumberSequence, Is.EqualTo(GoldDrawSequence), "The gold set was captured with a different draw sequence, so its numbers are not comparable.");
                Assert.That(actual, Is.EqualTo(gold.Features), $"Forecast percentiles differ from the set captured at {gold.CapturedFrom}.");
            }
        }

        [Test]
        [Explicit("Rewrites the committed gold file. Only ever run against a build that predates the change under test.")]
        public async Task CaptureGoldPercentiles()
        {
            var percentiles = await ForecastGoldFixture();

            var goldSet = new GoldForecastSet("4c0dea826", GoldDrawSequence, percentiles);

            var path = GoldFilePath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(goldSet, JsonOptions));

            Assert.That(File.Exists(path), Is.True, $"Failed to write the gold forecast set to {path}.");
        }

        private async Task<GoldPercentiles[]> ForecastGoldFixture()
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

            var randomNumberService = new NotSoRandomNumberService();
            randomNumberService.InitializeRandomNumbers(GoldDrawSequence);

            var forecastService = new ForecastService(randomNumberService, Mock.Of<ILogger<ForecastService>>(), teamMetricsServiceMock.Object, featureRepositoryMock.Object);

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

        private static GoldForecastSet ReadGoldSet()
        {
            var path = GoldFilePath();

            if (!File.Exists(path))
            {
                Assert.Fail($"No gold forecast set at {path}. Capture one from the released build before asserting against it.");
            }

            return JsonSerializer.Deserialize<GoldForecastSet>(File.ReadAllText(path), JsonOptions)
                ?? throw new InvalidOperationException($"The gold forecast set at {path} is empty.");
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

        private sealed record GoldPercentiles(string ReferenceId, int P50, int P70, int P85, int P95);

        private sealed record GoldForecastSet(string CapturedFrom, int[] RandomNumberSequence, GoldPercentiles[] Features);
    }
}
