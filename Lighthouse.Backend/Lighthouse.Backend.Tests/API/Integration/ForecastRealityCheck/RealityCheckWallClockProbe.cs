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

namespace Lighthouse.Backend.Tests.API.Integration.ForecastRealityCheck
{
    /// <summary>
    /// How long one reality check takes, and how many database round trips it costs, measured on the
    /// production ForecastRealityCheckService over the real metrics and forecast services, at the number
    /// of simulated runs the product ships with and over a year of real-sized history. Beside it, the
    /// same numbers for one shipped single backtest on the same Team, as the baseline a user already
    /// waits for.
    ///
    /// The check is designed to be synchronous and one-click, so a sweep that cannot finish inside a
    /// request would cost the interaction that is the feature. The budget it is judged against is a
    /// median of at most five seconds and a maximum of at most ten.
    ///
    /// The query count is reported beside the wall clock because the metrics cache is keyed by window
    /// while the query underneath is not: every window the sweep asks about costs one identical read of
    /// the Team's finished work, so the cold cost is the part that grows with the size of the Team.
    ///
    /// Deliberately not an assertion: a wall clock recorded on one machine says nothing on another, and
    /// a bound checked in from somebody's laptop fails CI for something that is not a defect. The query
    /// count is machine-independent and is asserted, by RealityCheckQueryCountTest.
    /// </summary>
    [TestFixture]
    [Category("epic-4172-forecast-reality-check")]
    [Category("slice-01")]
    public class RealityCheckWallClockProbe
    {
        private const string RunByHand = "Measures wall clock and query count. Run by hand, on one machine.";

        private const int HistoryDaysToSeed = 365;

        private const int SamplesPerCase = 12;

        private const int BacktestHorizonDays = 28;

        private const int BacktestSamplingWindowDays = 30;

        /// <param name="closedItemsToSeed">
        /// 615 is a real Team in this product's own dev database: a year of completed Work Items. The
        /// larger volumes are there because the cold cost scales with the Team, not with the sweep - a
        /// sweep that is affordable on a small Team and not on a large one would be invisible at one volume.
        /// </param>
        /// <param name="teamSamplingWindowDays">
        /// 30 is on the standard ladder, so the sweep checks sixteen cells. 45 is not, so the Team's own
        /// window is checked as a fifth and the sweep grows to twenty cells.
        /// </param>
        // @probe @us-01
        [TestCase(615, 30)]
        [TestCase(615, 45)]
        [TestCase(5_000, 30)]
        [TestCase(20_000, 30)]
        [Explicit(RunByHand)]
        public async Task MeasureRealityCheckWallClock(int closedItemsToSeed, int teamSamplingWindowDays)
        {
            var cellsChecked = 0;
            var cellsEvaluated = 0;

            var measurement = MeasureOnAFreshDatabase(closedItemsToSeed, teamSamplingWindowDays, (team, metrics, forecasts) =>
            {
                var result = new ForecastRealityCheckService(forecasts, metrics, NoBlackoutPeriods(), TestToday.Clock)
                    .Run(team, ThroughputFilterMode.SkipFilter);
                cellsChecked = result.Denominator.RunsAttempted;
                cellsEvaluated = result.Denominator.RunsEvaluated;
            });

            await Report(
                $"Reality check: {cellsChecked} cells ({cellsEvaluated} evaluated, each one simulated), " +
                $"Team window {teamSamplingWindowDays} days",
                closedItemsToSeed,
                measurement);

            Assert.That(measurement.Cold, Has.Count.EqualTo(SamplesPerCase));
        }

        /// <summary>
        /// One shipped backtest exactly as ForecastController.RunBacktest performs it: one read of the
        /// sampling window, one simulation, one read of what was actually delivered. Four weeks scored,
        /// the thirty days before them sampled.
        /// </summary>
        // @probe @us-01
        [Test]
        [Explicit(RunByHand)]
        public async Task MeasureSingleBacktestWallClock()
        {
            const int closedItemsToSeed = 615;

            var measurement = MeasureOnAFreshDatabase(closedItemsToSeed, BacktestSamplingWindowDays, (team, metrics, forecasts) =>
            {
                var periodEnd = TestToday.Clock.TodayAsUtcMidnight;
                var periodStart = periodEnd.AddDays(-BacktestHorizonDays);
                var history = metrics.GetBlackoutAwareThroughputForTeam(
                    team, periodStart.AddDays(-BacktestSamplingWindowDays), periodStart, ThroughputFilterMode.SkipFilter);
                var forecastDays = NoBlackoutPeriods()
                    .GetEffectiveBlackoutDays(periodStart, periodEnd)
                    .CountWorkingDays(periodStart, periodEnd);

                forecasts.HowMany(history, forecastDays);
                metrics.GetThroughputForTeam(team, periodStart, periodEnd, ThroughputFilterMode.SkipFilter);
            });

            await Report("Single backtest: 1 cell", closedItemsToSeed, measurement);

            Assert.That(measurement.Cold, Has.Count.EqualTo(SamplesPerCase));
        }

        /// <summary>
        /// Seeds one database, then takes one unrecorded sample so the first recorded one is not paying
        /// for the process compiling the code, then twelve recorded samples. Each sample builds fresh
        /// services, so its first run meets an empty cache - the state a first click lands in - and its
        /// second run, on the same services, meets the cache the first one filled.
        /// </summary>
        private static Measurement MeasureOnAFreshDatabase(
            int closedItemsToSeed, int teamSamplingWindowDays, Action<Team, TeamMetricsService, ForecastService> operation)
        {
            var databaseFile = Path.Combine(
                Path.GetTempPath(), $"reality-check-probe-{Path.GetRandomFileName().Replace(".", "", StringComparison.Ordinal)}.db");

            try
            {
                using (var context = BuildContext(databaseFile, new QueryCounter()))
                {
                    context.Database.EnsureCreated();
                    SeedRealisticHistory(context, closedItemsToSeed, teamSamplingWindowDays);
                }

                var measurement = new Measurement(MeasureOneSample(databaseFile, operation).ColdMilliseconds);

                for (var sample = 0; sample < SamplesPerCase; sample++)
                {
                    measurement.Add(MeasureOneSample(databaseFile, operation));
                }

                return measurement;
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                TryDelete(databaseFile);
            }
        }

        private static Sample MeasureOneSample(string databaseFile, Action<Team, TeamMetricsService, ForecastService> operation)
        {
            var queryCounter = new QueryCounter();
            using var context = BuildContext(databaseFile, queryCounter);
            var team = context.Teams.First();

            var metricsService = BuildMetricsService(context);
            var forecastService = BuildForecastService();

            queryCounter.Reset();
            var coldStopwatch = Stopwatch.StartNew();
            operation(team, metricsService, forecastService);
            coldStopwatch.Stop();
            var coldQueries = queryCounter.Count;

            queryCounter.Reset();
            var warmStopwatch = Stopwatch.StartNew();
            operation(team, metricsService, forecastService);
            warmStopwatch.Stop();

            return new Sample(coldStopwatch.ElapsedMilliseconds, warmStopwatch.ElapsedMilliseconds, coldQueries, queryCounter.Count);
        }

        private static async Task Report(string what, int closedItemsToSeed, Measurement measurement)
        {
            await TestContext.Out.WriteLineAsync(
                $"{what}; {ForecastSimulationLimits.Default.Trials} trials per run; " +
                $"{closedItemsToSeed} closed Work Items over {HistoryDaysToSeed} days; {SamplesPerCase} samples.");
            await TestContext.Out.WriteLineAsync(
                $"  Unrecorded first sample (compiling the code): {measurement.FirstSampleMilliseconds} ms");
            await TestContext.Out.WriteLineAsync(
                $"  COLD cache: {string.Join(", ", measurement.Cold)} ms " +
                $"(median {Median(measurement.Cold)} ms, max {measurement.Cold.Max()} ms), " +
                $"queries {string.Join("/", measurement.ColdQueries.Distinct())}");
            await TestContext.Out.WriteLineAsync(
                $"  WARM cache: {string.Join(", ", measurement.Warm)} ms " +
                $"(median {Median(measurement.Warm)} ms, max {measurement.Warm.Max()} ms), " +
                $"queries {string.Join("/", measurement.WarmQueries.Distinct())}");
            await TestContext.Out.WriteLineAsync("  Budget: median <= 5000 ms, max <= 10000 ms.");
        }

        private static void SeedRealisticHistory(LighthouseAppContext context, int closedItemsToSeed, int teamSamplingWindowDays)
        {
            // A Team carries a required connection it inherits from WorkTrackingSystemOptionsOwner. Nothing
            // measured here reads it and no work tracking system is contacted, but the row has to exist for
            // the Team to persist at all.
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
                ThroughputHistory = teamSamplingWindowDays,
                WorkTrackingSystemConnectionId = connection.Id,
            };
            context.Teams.Add(team);
            context.SaveChanges();

            var today = TestToday.Clock.TodayAsUtcMidnight;
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
                NoBlackoutPeriods(),
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

        private static IBlackoutPeriodService NoBlackoutPeriods()
        {
            var blackoutPeriodService = new Mock<IBlackoutPeriodService>();
            blackoutPeriodService
                .Setup(service => service.GetEffectiveBlackoutDays(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns([]);
            return blackoutPeriodService.Object;
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

        /// <summary>With an even number of samples, the mean of the middle two.</summary>
        private static double Median(List<long> values)
        {
            var ordered = values.Order().ToList();
            var middle = ordered.Count / 2;

            return ordered.Count % 2 == 1 ? ordered[middle] : (ordered[middle - 1] + ordered[middle]) / 2.0;
        }

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

        private sealed record Sample(long ColdMilliseconds, long WarmMilliseconds, int ColdQueries, int WarmQueries);

        private sealed class Measurement(long firstSampleMilliseconds)
        {
            public long FirstSampleMilliseconds { get; } = firstSampleMilliseconds;

            public List<long> Cold { get; } = [];

            public List<long> Warm { get; } = [];

            public List<int> ColdQueries { get; } = [];

            public List<int> WarmQueries { get; } = [];

            public void Add(Sample sample)
            {
                Cold.Add(sample.ColdMilliseconds);
                Warm.Add(sample.WarmMilliseconds);
                ColdQueries.Add(sample.ColdQueries);
                WarmQueries.Add(sample.WarmQueries);
            }
        }

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
