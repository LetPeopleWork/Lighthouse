using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Tests.API;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    /// <summary>
    /// A recorded fingerprint is only worth comparing against if the next round, reading the same Portfolio
    /// back from the database on a context of its own, arrives at the same value. Anything the database hands
    /// back in an order of its choosing, or in a shape one provider rounds differently, would make every round
    /// look changed. Checked on both providers, each read through a fresh context as every round does.
    /// </summary>
    [TestFixture]
    [Category("story-6354-skip-unchanged-forecasts")]
    [Category("slice-01")]
    public class ForecastInputFingerprintAcrossProvidersTests
    {
        private static readonly DateOnly TheInstanceDay = new(2026, 10, 23);

        // Started by the first Postgres case, so a run of the SQLite case alone needs no Docker.
        private SharedPostgresContainer? postgres;

        [OneTimeTearDown]
        public async Task StopPostgres()
        {
            if (postgres is not null)
            {
                await postgres.DisposeAsync();
            }
        }

        [Test]
        public async Task On_Sqlite_the_same_Portfolio_read_back_fingerprints_the_same()
        {
            var databaseFile = Path.Combine(Path.GetTempPath(), $"lighthouse-fingerprint-{Guid.NewGuid():N}.db");
            try
            {
                await using var provider = BuildProvider(options =>
                    options.UseSqlite($"Data Source={databaseFile};Pooling=False", sqlite => sqlite.MigrationsAssembly("Lighthouse.Migrations.Sqlite")));

                await AssertTheSamePortfolioFingerprintsTheSame(provider);
            }
            finally
            {
                if (File.Exists(databaseFile))
                {
                    File.Delete(databaseFile);
                }
            }
        }

        [Test]
        public async Task On_Postgres_the_same_Portfolio_read_back_fingerprints_the_same()
        {
            postgres ??= await SharedPostgresContainer.StartAsync();

            var connectionString = await postgres.ConnectionStringToANewCreatedDatabaseAsync();

            await using var provider = BuildProvider(options =>
                options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly("Lighthouse.Migrations.Postgres")));

            await AssertTheSamePortfolioFingerprintsTheSame(provider);
        }

        private static async Task AssertTheSamePortfolioFingerprintsTheSame(ServiceProvider provider)
        {
            var portfolioId = await APortfolioWithTwoTeams(provider);

            var firstRound = await FingerprintOf(provider, portfolioId);
            var secondRound = await FingerprintOf(provider, portfolioId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstRound, Is.Not.Null, "The Portfolio's forecast inputs could not be fingerprinted at all.");
                Assert.That(secondRound, Is.EqualTo(firstRound), "The same Portfolio, read back unchanged, fingerprinted differently.");
            }
        }

        private static async Task<int> APortfolioWithTwoTeams(ServiceProvider provider)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            await context.Database.MigrateAsync();

            var payments = new Team { Name = "Payments", FeatureWIP = 2, WorkTrackingSystemConnection = AConnection("Payments") };
            var ledger = new Team { Name = "Ledger", FeatureWIP = 1, WorkTrackingSystemConnection = AConnection("Ledger") };
            var elsewhere = new Team { Name = "Elsewhere", FeatureWIP = 1, WorkTrackingSystemConnection = AConnection("Elsewhere") };
            context.Teams.AddRange(payments, ledger, elsewhere);

            var cardVault = AFeature("PAY-88", "1", (payments, 7), (ledger, 3));
            var refunds = AFeature("PAY-90", "2", (payments, 12));
            var settlement = AFeature("PAY-93", "3", (ledger, 5));
            var unrelated = AFeature("OPS-12", "1", (elsewhere, 4));

            var portfolio = new Portfolio { Name = "Payments Platform", WorkTrackingSystemConnection = AConnection("Payments Platform") };
            portfolio.UpdateFeatures([cardVault, refunds, settlement]);

            context.Portfolios.Add(portfolio);
            context.Features.Add(unrelated);
            await context.SaveChangesAsync();

            return portfolio.Id;
        }

        private static async Task<string?> FingerprintOf(ServiceProvider provider, int portfolioId)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var portfolio = new PortfolioRepository(context, NullLogger<PortfolioRepository>.Instance).GetById(portfolioId)
                ?? throw new InvalidOperationException("The Portfolio just written could not be read back.");

            var outcome = await AForecastService(context).UpdateForecastsForPortfolio(portfolio, TheInstanceDay);

            Assert.That(outcome, Is.TypeOf<ForecastRunOutcome.Computed>());
            return ((ForecastRunOutcome.Computed)outcome).FreshFingerprint;
        }

        private static ForecastService AForecastService(LighthouseAppContext context)
        {
            var throughput = new RunChartData(RunChartDataGenerator.GenerateRunChartData([1, 0, 2, 1, 0, 3]));
            var teamMetrics = new Mock<ITeamMetricsService>();
            teamMetrics
                .Setup(metrics => metrics.GetForecastThroughputStatus(It.IsAny<Team>(), It.IsAny<ThroughputFilterMode>()))
                .Returns(new ForecastThroughputStatus(throughput, false, null));

            var features = new FeatureRepository(context, FeatureOrderingTestHelper.FollowingTheTracker(), NullLogger<FeatureRepository>.Instance);

            return new ForecastService(
                new NotSoRandomNumberService(),
                NullLogger<ForecastService>.Instance,
                teamMetrics.Object,
                features,
                new PortfolioRepository(context, NullLogger<PortfolioRepository>.Instance),
                new NothingWaitsForAnything(),
                new ForecastSimulation(new DrawsTheSameNumberEveryTime(), ForecastSimulationLimits.Default),
                TimeProvider.System);
        }

        private static Feature AFeature(string referenceId, string order, params (Team Team, int Remaining)[] work)
            => new(work.Select(entry => (entry.Team, entry.Remaining, entry.Remaining)))
            {
                Name = referenceId,
                ReferenceId = referenceId,
                Order = order,
            };

        private static WorkTrackingSystemConnection AConnection(string owner)
            => new() { Name = $"{owner} Connection", WorkTrackingSystem = WorkTrackingSystems.Jira, AuthenticationMethodKey = "pat" };

        private static ServiceProvider BuildProvider(Action<DbContextOptionsBuilder> useDatabase)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ICryptoService, FakeCryptoService>();
            services.AddDbContext<LighthouseAppContext>(useDatabase);

            return services.BuildServiceProvider();
        }
    }
}
