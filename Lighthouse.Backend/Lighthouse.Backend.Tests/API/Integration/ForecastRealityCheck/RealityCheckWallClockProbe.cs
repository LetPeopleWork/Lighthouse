using System.Data.Common;
using System.Diagnostics;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.ForecastRealityCheck
{
    /// <summary>
    /// How long a sixteen-run reality check takes, and how many database round trips it costs, at the
    /// number of simulated runs the product ships with and over a year of real-sized history.
    ///
    /// This answers R-1 of Epic 4172, which every other decision in that feature rests on: the check is
    /// designed to be synchronous and one-click, and a sweep that cannot finish inside a request would
    /// cost the interaction that IS the feature. The design reasoned the cost was affordable from the
    /// fact that the shipped single backtest is already a synchronous action contacting no work tracking
    /// system - reasoned, not measured, until this runs.
    ///
    /// It also prices the one cost the design found and could not settle. TeamMetricsService caches
    /// throughput under a key that carries the window, but the repository predicate underneath does not:
    /// every miss runs the same "all closed items for this Team" query. Sixteen distinct sampling windows
    /// plus four distinct scored periods therefore issue up to twenty identical queries on a cold cache.
    /// That is why the query count is reported beside the wall clock - a sweep that is fast on a small
    /// Team and slow on a large one shows up here as queries, not milliseconds.
    ///
    /// Deliberately not an assertion, following the two probes beside it: a wall clock recorded on one
    /// machine says nothing on another, and a bound checked in from somebody's laptop fails CI for
    /// something that is not a defect. The query count IS machine-independent, so if any number here
    /// ever becomes an assertion it should be that one.
    /// </summary>
    [TestFixture]
    [Category("epic-4172-forecast-reality-check")]
    [Category("slice-01")]
    public class RealityCheckWallClockProbe
    {
        /// <summary>Horizons the sweep scores, in days. Two, four, six and eight weeks.</summary>
        private static readonly int[] HorizonDays = [14, 28, 42, 56];

        /// <summary>Sampling windows the sweep tries, in days.</summary>
        private static readonly int[] SamplingWindowDays = [14, 30, 60, 90];

        private const int HistoryDaysToSeed = 365;

        private const int RunsPerMeasurement = 3;

        /// <param name="closedItemsToSeed">
        /// 615 is a real Team in this product's own dev database: a year of completed Work Items. The
        /// larger volumes are there because the twenty-query finding is a cost that scales with the
        /// Team, not with the sweep - a sweep that is affordable on a small Team and not on a large one
        /// would be invisible at one volume.
        /// </param>
        // @probe @us-01 @r-1 (AC-1.1)
        [TestCase(615)]
        [TestCase(5_000)]
        [TestCase(20_000)]
        [Explicit("Measures wall clock and query count. Run by hand, on one machine.")]
        public async Task MeasureSixteenRunRealityCheckWallClock(int closedItemsToSeed)
        {
            var databaseFile = Path.Combine(
                Path.GetTempPath(), $"reality-check-probe-{Path.GetRandomFileName().Replace(".", "")}.db");

            try
            {
                var cold = new List<long>();
                var warm = new List<long>();
                var coldQueries = new List<int>();
                var warmQueries = new List<int>();

                for (var run = 0; run < RunsPerMeasurement; run++)
                {
                    var measurement = MeasureOneSweep(databaseFile, run == 0, closedItemsToSeed);
                    cold.Add(measurement.ColdMilliseconds);
                    warm.Add(measurement.WarmMilliseconds);
                    coldQueries.Add(measurement.ColdQueries);
                    warmQueries.Add(measurement.WarmQueries);
                }

                await TestContext.Out.WriteLineAsync(
                    $"Reality check: {HorizonDays.Length * SamplingWindowDays.Length} cells, " +
                    $"{ForecastSimulationLimits.Default.Trials} trials per run, " +
                    $"{closedItemsToSeed} closed Work Items over {HistoryDaysToSeed} days.");
                await TestContext.Out.WriteLineAsync(
                    $"  COLD cache: {string.Join(" ms, ", cold)} ms " +
                    $"(median {Median(cold)} ms, max {cold.Max()} ms), " +
                    $"queries {string.Join("/", coldQueries)}");
                await TestContext.Out.WriteLineAsync(
                    $"  WARM cache: {string.Join(" ms, ", warm)} ms " +
                    $"(median {Median(warm)} ms, max {warm.Max()} ms), " +
                    $"queries {string.Join("/", warmQueries)}");
                await TestContext.Out.WriteLineAsync(
                    "  Budget under consideration: median <= 5000 ms, max <= 10000 ms.");

                Assert.That(cold, Is.Not.Empty);
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                TryDelete(databaseFile);
            }
        }

        private static SweepMeasurement MeasureOneSweep(string databaseFile, bool seed, int closedItemsToSeed)
        {
            var queryCounter = new QueryCounter();

            using var context = BuildContext(databaseFile, queryCounter);
            context.Database.EnsureCreated();

            if (seed)
            {
                SeedRealisticHistory(context, closedItemsToSeed);
            }

            var team = context.Teams.First();

            // A fresh service means a cold cache, which is the state a first click lands in.
            var metricsService = BuildMetricsService(context);
            var forecastService = BuildForecastService();

            queryCounter.Reset();
            var coldStopwatch = Stopwatch.StartNew();
            RunTheSweep(team, metricsService, forecastService);
            coldStopwatch.Stop();
            var coldQueries = queryCounter.Count;

            queryCounter.Reset();
            var warmStopwatch = Stopwatch.StartNew();
            RunTheSweep(team, metricsService, forecastService);
            warmStopwatch.Stop();

            return new SweepMeasurement(
                coldStopwatch.ElapsedMilliseconds, warmStopwatch.ElapsedMilliseconds,
                coldQueries, queryCounter.Count);
        }

        /// <summary>
        /// The sweep exactly as ForecastController.RunBacktest performs one cell, sixteen times, with
        /// today as the end anchor: every cell ends today and reaches back by its own horizon, and its
        /// sampling window sits immediately before it.
        /// </summary>
        private static void RunTheSweep(Team team, TeamMetricsService metricsService, ForecastService forecastService)
        {
            var today = DateTime.UtcNow.Date;

            foreach (var horizon in HorizonDays)
            {
                var periodStart = today.AddDays(-horizon);
                var periodEnd = today;

                foreach (var window in SamplingWindowDays)
                {
                    var historyEnd = periodStart;
                    var historyStart = periodStart.AddDays(-window);

                    var historicalThroughput = metricsService.GetBlackoutAwareThroughputForTeam(
                        team, historyStart, historyEnd, ThroughputFilterMode.SkipFilter);

                    forecastService.HowMany(historicalThroughput, horizon);
                }

                // Per horizon, not per cell - four reads shared four ways. An implementation that reads
                // the actual inside the inner loop is already 25% more expensive than it needs to be.
                metricsService.GetThroughputForTeam(team, periodStart, periodEnd, ThroughputFilterMode.SkipFilter);
            }
        }

        private static void SeedRealisticHistory(LighthouseAppContext context, int closedItemsToSeed)
        {
            // A Team carries a required connection FK it inherits from WorkTrackingSystemOptionsOwner.
            // Nothing in the sweep reads it - no work tracking system is contacted at any point - but the
            // row has to exist for the Team to persist at all.
            var connection = new WorkTrackingSystemConnection
            {
                Name = "Probe Connection",
                WorkTrackingSystem = WorkTrackingSystems.Csv,
            };
            context.WorkTrackingSystemConnections.Add(connection);
            context.SaveChanges();

            var team = new Team
            {
                Name = "Probe Team",
                ThroughputHistory = 90,
                WorkTrackingSystemConnectionId = connection.Id,
            };
            context.Teams.Add(team);
            context.SaveChanges();

            var today = DateTime.UtcNow.Date;
            var random = new Random(4172);
            var items = new List<WorkItem>();

            for (var index = 0; index < closedItemsToSeed; index++)
            {
                var closedOn = today.AddDays(-random.Next(0, HistoryDaysToSeed));

                items.Add(new WorkItem
                {
                    ReferenceId = $"PROBE-{index + 1}",
                    Name = $"Probe Work Item {index + 1}",
                    Type = "Work Item",
                    State = "Done",
                    StateCategory = StateCategories.Done,
                    TeamId = team.Id,
                    Url = string.Empty,
                    ParentReferenceId = string.Empty,
                    Order = $"{index + 1}",
                    StartedDate = closedOn.AddDays(-random.Next(1, 20)),
                    ClosedDate = closedOn,
                });
            }

            context.WorkItems.AddRange(items);
            context.SaveChanges();
        }

        private static TeamMetricsService BuildMetricsService(LighthouseAppContext context)
        {
            var appSettingService = new Mock<IAppSettingService>();
            appSettingService.Setup(service => service.GetTeamDataRefreshSettings())
                .Returns(new RefreshSettings { Interval = 1 });

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(provider => provider.GetService(typeof(Lighthouse.Backend.Cache.Cache<string, object>)))
                .Returns(new Lighthouse.Backend.Cache.Cache<string, object>());

            var blackoutPeriodService = new Mock<IBlackoutPeriodService>();
            blackoutPeriodService
                .Setup(service => service.GetEffectiveBlackoutDays(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns([]);

            var filterRuleService = new Mock<IForecastFilterRuleService>();
            filterRuleService
                .Setup(service => service.GetEffectiveRuleSet(It.IsAny<Team>()))
                .Returns((Lighthouse.Backend.Models.WorkItemRules.WorkItemRuleSet?)null);

            return new TeamMetricsService(
                Mock.Of<ILogger<TeamMetricsService>>(),
                new WorkItemRepository(context, Mock.Of<ILogger<WorkItemRepository>>()),
                Mock.Of<IRepository<Feature>>(),
                appSettingService.Object,
                serviceProvider.Object,
                blackoutPeriodService.Object,
                filterRuleService.Object,
                Mock.Of<IWorkItemStateTransitionRepository>(),
                TestToday.Clock);
        }

        private static ForecastService BuildForecastService()
        {
            return new ForecastService(
                new RandomNumberService(),
                Mock.Of<ILogger<ForecastService>>(),
                Mock.Of<ITeamMetricsService>(),
                Mock.Of<IRepository<Feature>>(),
                new NothingWaitsForAnything(),
                new DrawsAfreshEveryTime(),
                ForecastSimulationLimits.Default);
        }

        private static LighthouseAppContext BuildContext(string databaseFile, QueryCounter queryCounter)
        {
            var optionsBuilder = new DbContextOptionsBuilder<LighthouseAppContext>();
            optionsBuilder.UseSqlite(
                $"DataSource={databaseFile};Pooling=False",
                sqlite => sqlite.MigrationsAssembly("Lighthouse.Migrations.Sqlite"));
            optionsBuilder.AddInterceptors(queryCounter);

            return new LighthouseAppContext(
                optionsBuilder.Options, Mock.Of<ICryptoService>(), Mock.Of<ILogger<LighthouseAppContext>>());
        }

        private static long Median(List<long> values) => values.Order().ElementAt(values.Count / 2);

        private static void TryDelete(string path)
        {
            foreach (var file in new[] { path, $"{path}-shm", $"{path}-wal" })
            {
                try
                {
                    File.Delete(file);
                }
                catch (IOException)
                {
                    // A probe that cannot tidy up after itself still produced its number.
                }
            }
        }

        private sealed record SweepMeasurement(
            long ColdMilliseconds, long WarmMilliseconds, int ColdQueries, int WarmQueries);

        /// <summary>
        /// Counts executed commands. This is the machine-independent half of the measurement: the wall
        /// clock belongs to whoever ran it, but twenty queries are twenty queries anywhere.
        /// </summary>
        private sealed class QueryCounter : DbCommandInterceptor
        {
            private int count;

            public int Count => count;

            public void Reset() => Interlocked.Exchange(ref count, 0);

            public override InterceptionResult<DbDataReader> ReaderExecuting(
                DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
            {
                Interlocked.Increment(ref count);
                return base.ReaderExecuting(command, eventData, result);
            }
        }
    }
}
