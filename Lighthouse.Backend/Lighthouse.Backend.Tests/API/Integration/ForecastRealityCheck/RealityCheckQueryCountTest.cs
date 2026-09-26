using System.Data.Common;
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
    /// How many times one reality check reads the Team's finished work from the database, on a cold
    /// cache, through the production sweep. These are the reads of the Team's finished work, not every
    /// round trip the check makes: the blackout days are read through a service this test stands in for,
    /// so their reads are not in the count.
    ///
    /// The metrics cache is keyed by window but the query underneath is not, so every window the sweep
    /// asks about costs one identical read: one per sampling window per horizon, plus one per horizon for
    /// what the Team actually delivered - twenty for a Team on the standard ladder, twenty-four for a Team
    /// whose own window adds a fifth. The count is the one number here that is the same on every machine,
    /// and it is the only automated check that notices the actual being read once per check instead of
    /// once per horizon, which shows up as a count one or more too high.
    ///
    /// No clock time is asserted anywhere. The hand-run probe beside this file keeps the wall clock, which
    /// only means something on the machine it was taken on. The forecast is scripted, because the number
    /// of reads does not depend on what the simulation says and the simulation is the expensive part.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-4172-forecast-backtest-sweep")]
    [Category("slice-01")]
    public class RealityCheckQueryCountTest
    {
        private const int DaysOfFinishedWork = 200;

        private string databaseFile = string.Empty;

        [SetUp]
        public void CreateTheDatabaseFile()
        {
            databaseFile = Path.Combine(
                Path.GetTempPath(), $"reality-check-queries-{Path.GetRandomFileName().Replace(".", "", StringComparison.Ordinal)}.db");
        }

        [TearDown]
        public void RemoveTheDatabaseFile()
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

            foreach (var file in new[] { databaseFile, $"{databaseFile}-shm", $"{databaseFile}-wal" })
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
        }

        // @driving_port @us-01 @real-io @sqlite @kpi-OUT-4172-answer-in-seconds @contract-shape:bounded-change
        [TestCase(30, 20)]
        [TestCase(45, 24)]
        public void One_check_on_a_cold_cache_reads_the_Teams_finished_work_once_per_window_it_asks_about(int samplingWindowDays, int expectedReads)
        {
            var reads = new CountsEveryQuery();
            using var context = BuildContext(reads);
            context.Database.EnsureCreated();
            var team = GivenATeamThatFinishedWorkEveryDay(context, samplingWindowDays);

            reads.StartCountingFromZero();
            TheProductionSweepRunsOnce(team, BuildMetricsService(context), ForecastsThatNeverSimulate());

            Assert.That(reads.Count, Is.EqualTo(expectedReads),
                $"a Team at {samplingWindowDays} days is checked at {expectedReads - 4} windows over four horizons, and what it " +
                "delivered is read once per horizon - any more and the actual is being read once per check");
        }

        private static void TheProductionSweepRunsOnce(Team team, TeamMetricsService metrics, IForecastService forecasts)
        {
            var sweep = new ForecastRealityCheckService(forecasts, metrics, NoBlackoutPeriods(), TestToday.Clock);

            sweep.Run(team, ThroughputFilterMode.SkipFilter);
        }

        private static Team GivenATeamThatFinishedWorkEveryDay(LighthouseAppContext context, int samplingWindowDays)
        {
            var connection = new WorkTrackingSystemConnection { Name = "Query count", WorkTrackingSystem = WorkTrackingSystems.Csv };
            context.WorkTrackingSystemConnections.Add(connection);
            context.SaveChanges();

            var team = new Team
            {
                Name = "Ocean Explorer",
                ThroughputHistory = samplingWindowDays,
                WorkTrackingSystemConnectionId = connection.Id,
            };
            context.Teams.Add(team);
            context.SaveChanges();

            var today = TestToday.Clock.TodayAsUtcMidnight;
            context.WorkItems.AddRange(Enumerable.Range(0, DaysOfFinishedWork).Select(daysAgo => new WorkItem
            {
                ReferenceId = $"QC-{daysAgo + 1}",
                Name = $"Finished Work Item {daysAgo + 1}",
                Type = "User Story",
                State = "Done",
                StateCategory = StateCategories.Done,
                TeamId = team.Id,
                Url = string.Empty,
                ParentReferenceId = string.Empty,
                Order = $"{daysAgo + 1}",
                StartedDate = today.AddDays(-daysAgo - 3).AddHours(12),
                ClosedDate = today.AddDays(-daysAgo).AddHours(12),
            }));
            context.SaveChanges();

            return team;
        }

        private static TeamMetricsService BuildMetricsService(LighthouseAppContext context)
        {
            var appSettingService = new Mock<IAppSettingService>();
            appSettingService.Setup(service => service.GetTeamDataRefreshSettings()).Returns(new RefreshSettings { Interval = 1 });

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

        private static IBlackoutPeriodService NoBlackoutPeriods()
        {
            var blackoutPeriodService = new Mock<IBlackoutPeriodService>();
            blackoutPeriodService
                .Setup(service => service.GetEffectiveBlackoutDays(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns([]);
            return blackoutPeriodService.Object;
        }

        private static IForecastService ForecastsThatNeverSimulate()
        {
            var forecasts = new Mock<IForecastService>();
            forecasts
                .Setup(service => service.HowMany(It.IsAny<RunChartData>(), It.IsAny<int>()))
                .Returns((RunChartData _, int days) => new HowManyForecast(new Dictionary<int, int> { [1] = 100 }, days));
            return forecasts.Object;
        }

        private LighthouseAppContext BuildContext(CountsEveryQuery reads)
        {
            var options = new DbContextOptionsBuilder<LighthouseAppContext>();
            options.UseSqlite(
                $"DataSource={databaseFile};Pooling=False",
                sqlite => sqlite.MigrationsAssembly("Lighthouse.Migrations.Sqlite"));
            options.AddInterceptors(reads);

            return new LighthouseAppContext(options.Options, Mock.Of<ICryptoService>(), Mock.Of<ILogger<LighthouseAppContext>>());
        }

        private sealed class CountsEveryQuery : DbCommandInterceptor
        {
            private int count;

            public int Count => count;

            public void StartCountingFromZero() => Interlocked.Exchange(ref count, 0);

            public override InterceptionResult<DbDataReader> ReaderExecuting(
                DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
            {
                Interlocked.Increment(ref count);
                return base.ReaderExecuting(command, eventData, result);
            }
        }
    }
}
