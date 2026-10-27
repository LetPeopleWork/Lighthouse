using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
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
    /// Background forecast rounds run on threads of Lighthouse's own, and where a round runs must never change what
    /// it stores. The same seeded round is computed on SQLite the plain way and on Postgres on those threads, and
    /// each is read back through a fresh context, ordered, because a provider may hand rows back in any order.
    /// </summary>
    [TestFixture]
    [Category("epic-6400-fast-with-many-features")]
    [Category("slice-01")]
    public class ForecastOnTheLaneIdentityOnPostgresTests
    {
        /// <summary>
        /// Seven runs in ten finish, on the fifth day; the other three never do and leave no row. Recorded once,
        /// before the forecast threads existed, and held from then on.
        /// </summary>
        private const string TheForecastAsComputedBeforeTheForecastThreads = "400 runs [5:280]";

        private const int RunsInTenThatFinish = 7;

        private const string TheNameOfAForecastThread = "Lighthouse forecast";

        private static readonly ForecastSimulationLimits FourHundredRunsOnFourCores = new(400, 60) { CoresSeen = 4 };

        private static readonly int[] OneItemThenNothing = [1, 0];

        private static readonly DateOnly TheInstanceDay = new(2026, 10, 18);

        private static readonly int[] ThePercentilesShown = [50, 70, 85, 95];

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
        public async Task A_round_on_the_forecast_threads_stores_on_Postgres_what_the_same_round_stores_on_Sqlite_off_them()
        {
            var onSqlite = await WhatTheRoundStoresOnSqlite();

            postgres ??= await SharedPostgresContainer.StartAsync();
            var connectionString = await postgres.ConnectionStringToANewCreatedDatabaseAsync();
            await using var onPostgresDatabase = BuildProvider(options =>
                options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly("Lighthouse.Migrations.Postgres")));

            using var lane = new ForecastLane(FourHundredRunsOnFourCores);
            var threadsItRanOn = new List<string?>();
            var onPostgres = await WhatTheRoundStores(onPostgresDatabase, round => OnTheLane(lane, threadsItRanOn, round));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(threadsItRanOn, Is.Not.Empty.And.All.StartsWith(TheNameOfAForecastThread),
                    "The round did not run on the forecast threads, so this proved nothing about them.");
                Assert.That(onPostgres.Histogram, Is.EqualTo(TheForecastAsComputedBeforeTheForecastThreads),
                    "The round on the forecast threads stored another histogram on Postgres.");
                Assert.That(onSqlite.Histogram, Is.EqualTo(TheForecastAsComputedBeforeTheForecastThreads),
                    "The same round off the forecast threads no longer stores the recorded histogram on SQLite.");
                Assert.That(onPostgres.Percentiles, Is.EqualTo(onSqlite.Percentiles),
                    "The round on the forecast threads stored other percentiles on Postgres.");
                Assert.That(onPostgres.Fingerprint, Is.Not.Null.And.EqualTo(onSqlite.Fingerprint),
                    "The round on the forecast threads recorded another fingerprint of its inputs on Postgres, so the next quiet round would compute again.");
            }
        }

        private sealed record StoredForecast(string Histogram, IReadOnlyList<ForecastPercentile> Percentiles, string? Fingerprint);

        private static async Task<StoredForecast> WhatTheRoundStoresOnSqlite()
        {
            var databaseFile = Path.Combine(Path.GetTempPath(), $"lighthouse-lane-identity-{Guid.NewGuid():N}.db");
            try
            {
                await using var provider = BuildProvider(options =>
                    options.UseSqlite($"Data Source={databaseFile};Pooling=False", sqlite => sqlite.MigrationsAssembly("Lighthouse.Migrations.Sqlite")));

                return await WhatTheRoundStores(provider, round => round());
            }
            finally
            {
                if (File.Exists(databaseFile))
                {
                    File.Delete(databaseFile);
                }
            }
        }

        private static Task OnTheLane(ForecastLane lane, List<string?> threadsItRanOn, Func<Task> round)
            => Task.Factory.StartNew(
                async () =>
                {
                    threadsItRanOn.Add(Thread.CurrentThread.Name);
                    await round();
                    threadsItRanOn.Add(Thread.CurrentThread.Name);
                },
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                lane.Scheduler).Unwrap();

        private static async Task<StoredForecast> WhatTheRoundStores(ServiceProvider provider, Func<Func<Task>, Task> runTheRound)
        {
            var portfolioId = await APortfolioWithOneFeatureOfFiveItems(provider);

            using (var scope = provider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                await runTheRound(() => ForecastAndRecordWhatItWasComputedFrom(context, portfolioId));
            }

            return await ReadBackOnAFreshContext(provider, portfolioId);
        }

        /// <summary>What the forecast updater does with a round: forecast, then record what it was computed from.</summary>
        private static async Task ForecastAndRecordWhatItWasComputedFrom(LighthouseAppContext context, int portfolioId)
        {
            var portfolios = new PortfolioRepository(context, NullLogger<PortfolioRepository>.Instance);
            var portfolio = portfolios.GetById(portfolioId)
                ?? throw new InvalidOperationException("The Portfolio just written could not be read back.");

            var outcome = await AForecastService(context).UpdateForecastsForPortfolio(portfolio, TheInstanceDay);

            if (outcome is not ForecastRunOutcome.Computed { RunMark: { } runMark, FreshFingerprint: { } fresh })
            {
                throw new InvalidOperationException($"The round was meant to compute a forecast and fingerprint it, and answered {outcome}.");
            }

            await portfolios.TryRecordForecastInputs(portfolioId, runMark, fresh, CancellationToken.None);
        }

        private static async Task<StoredForecast> ReadBackOnAFreshContext(ServiceProvider provider, int portfolioId)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var forecast = await context.Features.AsNoTracking()
                .Where(feature => feature.Portfolios.Any(owner => owner.Id == portfolioId))
                .SelectMany(feature => feature.Forecasts)
                .Include(stored => stored.SimulationResults)
                .SingleAsync();

            var histogram = string.Join(", ", forecast.SimulationResults
                .OrderBy(day => day.Key)
                .Select(day => $"{day.Key}:{day.Value}"));

            var fingerprint = await context.Portfolios.AsNoTracking()
                .Where(portfolio => portfolio.Id == portfolioId)
                .Select(portfolio => portfolio.ForecastInputFingerprint)
                .SingleAsync();

            return new StoredForecast(
                $"{forecast.TotalTrials} runs [{histogram}]",
                [.. ThePercentilesShown.Select(forecast.GetPercentile)],
                fingerprint);
        }

        private static async Task<int> APortfolioWithOneFeatureOfFiveItems(ServiceProvider provider)
        {
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            await context.Database.MigrateAsync();

            var connection = new WorkTrackingSystemConnection { Name = "Payments Connection", WorkTrackingSystem = WorkTrackingSystems.Jira, AuthenticationMethodKey = "pat" };
            var team = new Team
            {
                Name = "Payments",
                WorkTrackingSystemConnection = connection,
                DoneItemsCutoffDays = 365,
                DataRetrievalValue = "project = TEST",
                WorkItemTypes = ["Story"],
                ToDoStates = ["New"],
                DoingStates = ["In Progress"],
                DoneStates = ["Done"],
                ThroughputHistory = 30,
            };
            context.Teams.Add(team);

            var portfolio = new Portfolio { Name = "Platform Modernisation", WorkTrackingSystemConnection = connection };
            portfolio.UpdateFeatures([new Feature(team, 5) { Name = "Billing Migration", ReferenceId = "PAY-1", Order = "10" }]);
            context.Portfolios.Add(portfolio);

            await context.SaveChangesAsync();

            return portfolio.Id;
        }

        private static ForecastService AForecastService(LighthouseAppContext context)
        {
            var throughput = new RunChartData(RunChartDataGenerator.GenerateRunChartData(OneItemThenNothing));
            var teamMetrics = new Mock<ITeamMetricsService>();
            teamMetrics
                .Setup(metrics => metrics.GetForecastThroughputStatus(It.IsAny<Team>(), It.IsAny<ThroughputFilterMode>()))
                .Returns(new ForecastThroughputStatus(throughput, false, null));

            return new ForecastService(
                new NotSoRandomNumberService(),
                NullLogger<ForecastService>.Instance,
                teamMetrics.Object,
                new FeatureRepository(context, FeatureOrderingTestHelper.FollowingTheTracker(), NullLogger<FeatureRepository>.Instance),
                new PortfolioRepository(context, NullLogger<PortfolioRepository>.Instance),
                new NothingWaitsForAnything(),
                new ForecastSimulation(new CountedDrawStream(RunsInTenThatFinish), FourHundredRunsOnFourCores),
                TimeProvider.System);
        }

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
