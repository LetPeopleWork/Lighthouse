using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.DomainEvents;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Services.Implementation.DomainEvents
{
    [TestFixture]
    [Category("epic-5427-percentiles-over-time")]
    public class DemoPercentilesBackfillHandlerTests
    {
        private DbContextOptions<LighthouseAppContext> options = null!;
        private Mock<ICryptoService> cryptoServiceMock = null!;
        private Mock<ILogger<LighthouseAppContext>> appContextLoggerMock = null!;

        private Mock<Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<Team>> teamRepositoryMock = null!;
        private Mock<Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<Portfolio>> portfolioRepositoryMock = null!;
        private Mock<Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<WorkTrackingSystemConnection>> connectionRepositoryMock = null!;

        private static readonly int[] ExpectedHorizons = [30, 60, 90];

        [SetUp]
        public void SetUp()
        {
            options = new DbContextOptionsBuilder<LighthouseAppContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            cryptoServiceMock = new Mock<ICryptoService>();
            appContextLoggerMock = new Mock<ILogger<LighthouseAppContext>>();

            teamRepositoryMock = new Mock<Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<Team>>();
            portfolioRepositoryMock = new Mock<Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<Portfolio>>();
            connectionRepositoryMock = new Mock<Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<WorkTrackingSystemConnection>>();
        }

        private LighthouseAppContext CreateContext()
        {
            return new LighthouseAppContext(options, cryptoServiceMock.Object, appContextLoggerMock.Object);
        }

        private DemoPercentilesBackfillHandler CreateSubject(LighthouseAppContext context)
        {
            var snapshotRepo = new PercentilesOverTimeSnapshotRepository(
                context, Mock.Of<ILogger<PercentilesOverTimeSnapshotRepository>>());

            return new DemoPercentilesBackfillHandler(
                teamRepositoryMock.Object,
                portfolioRepositoryMock.Object,
                connectionRepositoryMock.Object,
                snapshotRepo,
                Mock.Of<ILogger<DemoPercentilesBackfillHandler>>());
        }

        private void ArrangeConnection(int connectionId, bool isDemo)
        {
            var connection = new WorkTrackingSystemConnection
            {
                Id = connectionId,
                Name = "Connection",
                WorkTrackingSystem = WorkTrackingSystems.Csv,
            };

            if (isDemo)
            {
                connection.Options.Add(new WorkTrackingSystemConnectionOption
                {
                    Key = CsvWorkTrackingOptionNames.SynthesizeStateJourneyForDemo,
                    Value = bool.TrueString,
                });
            }

            connectionRepositoryMock.Setup(x => x.GetById(connectionId)).Returns(connection);
        }

        private Team ArrangeTeam(int teamId, int connectionId)
        {
            var team = new Team { Id = teamId, Name = $"Team {teamId}", WorkTrackingSystemConnectionId = connectionId };
            teamRepositoryMock.Setup(x => x.GetById(teamId)).Returns(team);
            return team;
        }

        private Portfolio ArrangePortfolio(int portfolioId, int connectionId)
        {
            var portfolio = new Portfolio { Id = portfolioId, Name = $"Portfolio {portfolioId}", WorkTrackingSystemConnectionId = connectionId };
            portfolioRepositoryMock.Setup(x => x.GetById(portfolioId)).Returns(portfolio);
            return portfolio;
        }

        [Test]
        public async Task HandleTeamRefreshed_DemoOwner_BackdatesCycleTimePercentileHistoryForAllHorizons()
        {
            const int teamId = 7;
            const int connectionId = 1886;
            ArrangeConnection(connectionId, isDemo: true);
            ArrangeTeam(teamId, connectionId);

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(teamId), CancellationToken.None);

            var todayDate = DateOnly.FromDateTime(DateTime.Today);
            var snapshots = context.PercentilesOverTimeSnapshots
                .Where(s => s.OwnerId == teamId && s.OwnerType == OwnerType.Team && s.MetricType == MetricType.CycleTime)
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(snapshots, Has.Count.EqualTo(42), "14-day window x 3 horizons");
                Assert.That(snapshots.All(s => s.RecordedAt < todayDate), Is.True, "backdated rows only; today stays forward-only");
                Assert.That(snapshots.Select(s => s.Horizon).Distinct().OrderBy(h => h), Is.EqualTo(ExpectedHorizons));
                Assert.That(snapshots.Select(s => s.RecordedAt).Distinct().Count(), Is.EqualTo(14), "one row per horizon per day across the window");
                Assert.That(snapshots.All(s => s.P50 <= s.P70 && s.P70 <= s.P85 && s.P85 <= s.P95), Is.True, "percentiles are monotone non-decreasing");
                Assert.That(snapshots.All(s => s.P50 > 0), Is.True, "demo values are plausible, non-zero cycle times");
            }
        }

        [Test]
        public async Task HandlePortfolioRefreshed_DemoOwner_BackdatesCycleTimePercentileHistoryForAllHorizons()
        {
            const int portfolioId = 12;
            const int connectionId = 1886;
            ArrangeConnection(connectionId, isDemo: true);
            ArrangePortfolio(portfolioId, connectionId);

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new PortfolioFeaturesRefreshed(portfolioId), CancellationToken.None);

            var ownerRows = context.PercentilesOverTimeSnapshots
                .Where(s => s.OwnerId == portfolioId && s.OwnerType == OwnerType.Portfolio)
                .ToList();
            var cycleTimeRows = ownerRows.Where(s => s.MetricType == MetricType.CycleTime).ToList();
            var ageRows = ownerRows.Where(s => s.MetricType == MetricType.WorkItemAge).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(cycleTimeRows, Has.Count.EqualTo(42), "14-day window x 3 horizons");
                Assert.That(cycleTimeRows.Select(s => s.Horizon).Distinct().OrderBy(h => h), Is.EqualTo(ExpectedHorizons));
                Assert.That(ageRows, Has.Count.EqualTo(14), "portfolios get the same 14-day age history as teams");
            }
        }

        [Test]
        public async Task HandleTeamRefreshed_NonDemoOwner_DoesNothing()
        {
            const int teamId = 7;
            const int connectionId = 55;
            ArrangeConnection(connectionId, isDemo: false);
            ArrangeTeam(teamId, connectionId);

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(teamId), CancellationToken.None);

            Assert.That(context.PercentilesOverTimeSnapshots.ToList(), Is.Empty, "real tenants stay forward-only and are never backdated");
        }

        [Test]
        public async Task HandlePortfolioRefreshed_NonDemoOwner_DoesNothing()
        {
            const int portfolioId = 12;
            const int connectionId = 55;
            ArrangeConnection(connectionId, isDemo: false);
            ArrangePortfolio(portfolioId, connectionId);

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new PortfolioFeaturesRefreshed(portfolioId), CancellationToken.None);

            Assert.That(context.PercentilesOverTimeSnapshots.ToList(), Is.Empty, "real portfolios stay forward-only and are never backdated");
        }

        [Test]
        public async Task HandlePortfolioRefreshed_NullPortfolio_DoesNothing()
        {
            portfolioRepositoryMock.Setup(x => x.GetById(404)).Returns((Portfolio?)null);

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new PortfolioFeaturesRefreshed(404), CancellationToken.None);

            Assert.That(context.PercentilesOverTimeSnapshots.ToList(), Is.Empty, "a missing portfolio must not trigger a backfill or throw");
        }

        // Idempotency keys off a *backdated* row (RecordedAt < today). A pre-existing
        // row for *today* only (the forward-only recorder's output) must NOT be read
        // as "already backfilled" — the backfill still runs and the today row is left
        // untouched by the backdated upserts.
        [Test]
        public async Task HandleTeamRefreshed_OnlyTodayRowExists_StillBackfillsBackdatedHistory_AndLeavesTodayRowUntouched()
        {
            const int teamId = 7;
            const int connectionId = 1886;
            ArrangeConnection(connectionId, isDemo: true);
            ArrangeTeam(teamId, connectionId);

            var todayDate = DateOnly.FromDateTime(DateTime.Today);
            using (var seedContext = CreateContext())
            {
                // The forward-only recorder already wrote today's horizon-30 row.
                seedContext.PercentilesOverTimeSnapshots.Add(new PercentilesOverTimeSnapshot
                {
                    OwnerId = teamId,
                    OwnerType = OwnerType.Team,
                    MetricType = MetricType.CycleTime,
                    Horizon = 30,
                    RecordedAt = todayDate,
                    P50 = 999,
                    P70 = 999,
                    P85 = 999,
                    P95 = 999,
                });
                await seedContext.SaveChangesAsync();
            }

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(teamId), CancellationToken.None);

            var ownerRows = context.PercentilesOverTimeSnapshots
                .Where(s => s.OwnerId == teamId && s.OwnerType == OwnerType.Team)
                .ToList();
            var backdated = ownerRows
                .Where(s => s.RecordedAt < todayDate && s.MetricType == MetricType.CycleTime)
                .ToList();
            var todayRow = ownerRows.Single(s => s.RecordedAt == todayDate && s.Horizon == 30);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(backdated, Has.Count.EqualTo(42), "a today-only row must not be mistaken for an already-run backfill");
                Assert.That(todayRow.P50, Is.EqualTo(999), "the backdated upserts must never touch the pre-existing today row");
                Assert.That(backdated.All(s => s.RecordedAt < todayDate), Is.True);
            }
        }

        // The synthesized demo wave is deterministic: p50 = 4 + (dayIndex % 5) + horizon/30,
        // where dayIndex = HistoryWindowDays - daysAgo, and each higher percentile steps
        // by a fixed offset. Pinning exact values guards the arithmetic.
        [Test]
        public async Task HandleTeamRefreshed_SynthesizesDeterministicPercentileValuesPerDayAndHorizon()
        {
            const int teamId = 7;
            const int connectionId = 1886;
            ArrangeConnection(connectionId, isDemo: true);
            ArrangeTeam(teamId, connectionId);

            var todayDate = DateOnly.FromDateTime(DateTime.Today);

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(teamId), CancellationToken.None);

            PercentilesOverTimeSnapshot Row(int daysAgo, int horizon) =>
                context.PercentilesOverTimeSnapshots.Single(s =>
                    s.OwnerId == teamId
                    && s.OwnerType == OwnerType.Team
                    && s.Horizon == horizon
                    && s.RecordedAt == todayDate.AddDays(-daysAgo));

            // Oldest day: daysAgo=14 -> dayIndex=0 -> (0 % 5)=0.
            var oldestH30 = Row(14, 30); // 4 + 0 + 30/30 = 5
            var oldestH90 = Row(14, 90); // 4 + 0 + 90/30 = 7
            // Newest backdated day: daysAgo=1 -> dayIndex=13 -> (13 % 5)=3.
            var newestH30 = Row(1, 30);  // 4 + 3 + 1 = 8
            var newestH90 = Row(1, 90);  // 4 + 3 + 3 = 10

            using (Assert.EnterMultipleScope())
            {
                Assert.That(oldestH30.P50, Is.EqualTo(5));
                Assert.That(oldestH30.P70, Is.EqualTo(8), "p70 = p50 + 3");
                Assert.That(oldestH30.P85, Is.EqualTo(12), "p85 = p70 + 4");
                Assert.That(oldestH30.P95, Is.EqualTo(17), "p95 = p85 + 5");
                Assert.That(oldestH90.P50, Is.EqualTo(7), "horizon offset = horizon / 30");
                Assert.That(newestH30.P50, Is.EqualTo(8), "dayIndex modulo drives the wave; 13 % 5 = 3");
                Assert.That(newestH90.P50, Is.EqualTo(10));
            }
        }

        // Work item age has no horizon dimension (age is always "as of today"), so its backdated
        // rows carry the NoHorizon sentinel rather than the 30/60/90 fan the cycle-time family uses.
        [Test]
        public async Task HandleTeamRefreshed_DemoOwner_BackdatesWorkItemAgePercentileHistoryAtNoHorizon()
        {
            const int teamId = 7;
            const int connectionId = 1886;
            ArrangeConnection(connectionId, isDemo: true);
            ArrangeTeam(teamId, connectionId);

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(teamId), CancellationToken.None);

            var todayDate = DateOnly.FromDateTime(DateTime.Today);
            var ageRows = context.PercentilesOverTimeSnapshots
                .Where(s => s.OwnerId == teamId && s.OwnerType == OwnerType.Team && s.MetricType == MetricType.WorkItemAge)
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ageRows, Has.Count.EqualTo(14), "the same 14-day window as cycle-time, one row per day");
                Assert.That(ageRows.All(s => s.Horizon == PercentilesOverTimeSnapshot.NoHorizon), Is.True, "age rows carry the NoHorizon sentinel");
                Assert.That(ageRows.All(s => s.RecordedAt < todayDate), Is.True, "backdated rows only; today stays forward-only");
                Assert.That(ageRows.Select(s => s.RecordedAt).Distinct().Count(), Is.EqualTo(14), "exactly one row per day");
                Assert.That(ageRows.All(s => s.P50 <= s.P70 && s.P70 <= s.P85 && s.P85 <= s.P95), Is.True, "percentiles are monotone non-decreasing");
                Assert.That(ageRows.All(s => s.P50 > 0), Is.True, "demo ages are plausible, non-zero day counts");
            }
        }

        // Idempotency is evaluated PER METRIC FAMILY. A demo owner backfilled by an earlier release
        // (cycle-time only) must STILL gain its work-item-age history on the next refresh — a shared
        // "any backdated row exists" guard would make the age backfill a permanent silent no-op on
        // every environment that already ran the cycle-time one. A further refresh adds nothing.
        [Test]
        public async Task HandleTeamRefreshed_CycleTimeHistoryAlreadyPresent_BackfillsWorkItemAgeOnceAndLeavesCycleTimeUntouched()
        {
            const int teamId = 7;
            const int connectionId = 1886;
            ArrangeConnection(connectionId, isDemo: true);
            ArrangeTeam(teamId, connectionId);

            using var seedContext = CreateContext();
            seedContext.PercentilesOverTimeSnapshots.Add(new PercentilesOverTimeSnapshot
            {
                OwnerId = teamId,
                OwnerType = OwnerType.Team,
                MetricType = MetricType.CycleTime,
                Horizon = 30,
                RecordedAt = DateOnly.FromDateTime(DateTime.Today.AddDays(-3)),
                P50 = 5,
                P70 = 7,
                P85 = 9,
                P95 = 11,
            });
            await seedContext.SaveChangesAsync();

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(teamId), CancellationToken.None);
            await subject.HandleAsync(new TeamDataRefreshed(teamId), CancellationToken.None);

            var ownerRows = context.PercentilesOverTimeSnapshots
                .Where(s => s.OwnerId == teamId && s.OwnerType == OwnerType.Team)
                .ToList();
            var cycleTimeRows = ownerRows.Where(s => s.MetricType == MetricType.CycleTime).ToList();
            var ageRows = ownerRows.Where(s => s.MetricType == MetricType.WorkItemAge).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(cycleTimeRows, Has.Count.EqualTo(1), "the already-backfilled cycle-time family must not be re-run");
                Assert.That(ageRows, Has.Count.EqualTo(14), "the missing age family is backfilled, and the second refresh adds nothing on top");
            }
        }
    }
}
