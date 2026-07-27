using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.DomainEvents;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestDoubles;
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

        // Bug #5567 root cause D - the subject is handed a clock parked on a fixed day and the
        // day-dependent expectations below are literals anchored to it. Re-deriving DateTime.Today
        // here would agree with the subject by construction, whatever day the subject picked.
        private static readonly DateTimeOffset FixedInstant = new(2026, 3, 10, 9, 0, 0, TimeSpan.Zero);

        private static readonly DateOnly FixedToday = new(2026, 3, 10);

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

            var processBehaviorRepo = new ProcessBehaviorSnapshotRepository(
                context, Mock.Of<ILogger<ProcessBehaviorSnapshotRepository>>());

            return new DemoPercentilesBackfillHandler(
                teamRepositoryMock.Object,
                portfolioRepositoryMock.Object,
                connectionRepositoryMock.Object,
                snapshotRepo,
                processBehaviorRepo,
                new FakeLighthouseClock(FixedInstant),
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

            var todayDate = FixedToday;
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

            var todayDate = FixedToday;
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

            var todayDate = FixedToday;

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

            var todayDate = FixedToday;
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
                RecordedAt = FixedToday.AddDays(-3),
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

        // Slice 03: the demo backfill covers BOTH over-time families. Throughput natural process
        // limits are backdated across the SAME 14-day window the percentile families use, so the two
        // widgets captured on one demo screenshot never disagree about their date range.
        [Test]
        public async Task HandleTeamRefreshed_DemoOwner_BackdatesThroughputProcessBehaviorLimitsOverTheSameWindow()
        {
            const int teamId = 7;
            const int connectionId = 1886;
            ArrangeConnection(connectionId, isDemo: true);
            ArrangeTeam(teamId, connectionId);

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(teamId), CancellationToken.None);

            var todayDate = FixedToday;
            var throughputRows = context.ProcessBehaviorSnapshots
                .Where(s => s.OwnerId == teamId && s.OwnerType == OwnerType.Team)
                .ToList();
            var throughputDates = throughputRows.Select(s => s.RecordedAt).Distinct().OrderBy(d => d).ToList();
            var percentileDates = context.PercentilesOverTimeSnapshots
                .Where(s => s.OwnerId == teamId && s.OwnerType == OwnerType.Team)
                .Select(s => s.RecordedAt)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(throughputRows, Has.Count.EqualTo(14), "one NPL row per day of the window; limits have no horizon dimension");
                Assert.That(throughputRows.All(s => s.MetricType == ProcessBehaviorMetricType.Throughput), Is.True, "Throughput is the only process-behaviour family so far");
                Assert.That(throughputRows.All(s => s.RecordedAt < todayDate), Is.True, "backdated rows only; today stays forward-only");
                Assert.That(throughputDates, Is.EqualTo(percentileDates), "both over-time families span an identically dated window");
            }
        }

        [Test]
        public async Task HandlePortfolioRefreshed_DemoOwner_BackdatesThroughputProcessBehaviorLimits()
        {
            const int portfolioId = 12;
            const int connectionId = 1886;
            ArrangeConnection(connectionId, isDemo: true);
            ArrangePortfolio(portfolioId, connectionId);

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new PortfolioFeaturesRefreshed(portfolioId), CancellationToken.None);

            var throughputRows = context.ProcessBehaviorSnapshots
                .Where(s => s.OwnerId == portfolioId && s.OwnerType == OwnerType.Portfolio)
                .ToList();

            Assert.That(throughputRows, Has.Count.EqualTo(14), "portfolios get the same 14-day throughput NPL history as teams");
        }

        // A degenerate (flat) or inverted triple would render a visibly broken chart in the docs
        // screenshots, so EVERY backdated day must satisfy LNPL <= Average <= UNPL.
        [Test]
        public async Task HandleTeamRefreshed_BackdatedThroughputLimits_AreInternallyConsistentOnEveryDay([Range(1, 14)] int daysAgo)
        {
            const int teamId = 7;
            const int connectionId = 1886;
            ArrangeConnection(connectionId, isDemo: true);
            ArrangeTeam(teamId, connectionId);

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(teamId), CancellationToken.None);

            var recordedAt = FixedToday.AddDays(-daysAgo);
            var row = context.ProcessBehaviorSnapshots.Single(s =>
                s.OwnerId == teamId
                && s.OwnerType == OwnerType.Team
                && s.MetricType == ProcessBehaviorMetricType.Throughput
                && s.RecordedAt == recordedAt);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(row.Lnpl, Is.LessThanOrEqualTo(row.Average), "LNPL <= Average");
                Assert.That(row.Average, Is.LessThanOrEqualTo(row.Unpl), "Average <= UNPL");
                Assert.That(row.Lnpl, Is.LessThan(row.Unpl), "a degenerate triple would draw the three lines on top of each other");
                Assert.That(row.Average, Is.Positive, "a zero average is the NotReady sentinel, never a plausible demo throughput");
            }
        }

        public enum DemoBackfillSeed
        {
            Nothing,
            PercentilesOnly,
            PercentilesAndThroughput,
        }

        // The idempotency guard is PER METRIC FAMILY, never per owner. An owner-scoped guard would
        // make every newly added family a permanent no-op on every environment an earlier release
        // already backfilled for the percentile families — exactly the bug slice-02 fixed. The
        // handler is invoked twice in each case, so a passing row count also proves idempotency.
        [TestCase(DemoBackfillSeed.Nothing, true, 14)]
        [TestCase(DemoBackfillSeed.PercentilesOnly, true, 14)]
        [TestCase(DemoBackfillSeed.PercentilesAndThroughput, true, 1)]
        [TestCase(DemoBackfillSeed.Nothing, false, 0)]
        public async Task HandleTeamRefreshed_ThroughputBackfillGuardIsScopedToItsOwnFamily(
            DemoBackfillSeed seed, bool isDemo, int expectedThroughputRows)
        {
            const int teamId = 7;
            const int connectionId = 1886;
            ArrangeConnection(connectionId, isDemo);
            ArrangeTeam(teamId, connectionId);

            await SeedBackdatedHistoryAsync(teamId, seed);

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(teamId), CancellationToken.None);
            await subject.HandleAsync(new TeamDataRefreshed(teamId), CancellationToken.None);

            var throughputRows = context.ProcessBehaviorSnapshots
                .Where(s => s.OwnerId == teamId && s.OwnerType == OwnerType.Team)
                .ToList();

            Assert.That(throughputRows, Has.Count.EqualTo(expectedThroughputRows));
        }

        public enum ForeignThroughputRow
        {
            /// <summary>Another owner entirely, same owner kind.</summary>
            OtherOwnerId,

            /// <summary>The same numeric id, but a portfolio rather than a team.</summary>
            OtherOwnerType,
        }

        // Both the "have I already backfilled?" guard and the per-day upsert address a row by its
        // full natural key. A row belonging to a NEIGHBOUR — another owner id, or the same id under
        // the other owner kind — must neither satisfy our guard (which would suppress our history
        // entirely) nor be picked up as "our" row for that day (which would overwrite a neighbour's
        // limits with ours). The neighbour row is seeded INSIDE the backfill window so both seams are
        // exercised on the same day the loop writes.
        [TestCase(ForeignThroughputRow.OtherOwnerId)]
        [TestCase(ForeignThroughputRow.OtherOwnerType)]
        public async Task HandleTeamRefreshed_NeighbourThroughputRowInsideTheWindow_NeitherSuppressesNorAbsorbsOurBackfill(
            ForeignThroughputRow neighbour)
        {
            const int teamId = 7;
            const int connectionId = 1886;
            const int foreignOwnerId = 999;
            const int SentinelLimit = 500;
            ArrangeConnection(connectionId, isDemo: true);
            ArrangeTeam(teamId, connectionId);

            var insideTheWindow = FixedToday.AddDays(-5);
            var neighbourOwnerId = neighbour == ForeignThroughputRow.OtherOwnerId ? foreignOwnerId : teamId;
            var neighbourOwnerType = neighbour == ForeignThroughputRow.OtherOwnerType
                ? OwnerType.Portfolio
                : OwnerType.Team;

            using (var seedContext = CreateContext())
            {
                seedContext.ProcessBehaviorSnapshots.Add(new ProcessBehaviorSnapshot
                {
                    OwnerId = neighbourOwnerId,
                    OwnerType = neighbourOwnerType,
                    MetricType = ProcessBehaviorMetricType.Throughput,
                    RecordedAt = insideTheWindow,
                    Unpl = SentinelLimit,
                    Average = SentinelLimit,
                    Lnpl = SentinelLimit,
                });
                await seedContext.SaveChangesAsync();
            }

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(teamId), CancellationToken.None);

            var ourRows = context.ProcessBehaviorSnapshots
                .Where(s => s.OwnerId == teamId && s.OwnerType == OwnerType.Team)
                .ToList();
            var neighbourRow = context.ProcessBehaviorSnapshots.Single(s =>
                s.OwnerId == neighbourOwnerId
                && s.OwnerType == neighbourOwnerType
                && s.RecordedAt == insideTheWindow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ourRows, Has.Count.EqualTo(14), "a neighbour's history must not satisfy our own backfill guard");
                Assert.That(neighbourRow.Unpl, Is.EqualTo(SentinelLimit), "the neighbour's row is not ours to overwrite");
                Assert.That(neighbourRow.Average, Is.EqualTo(SentinelLimit));
                Assert.That(neighbourRow.Lnpl, Is.EqualTo(SentinelLimit));
            }
        }

        // The guard asks whether PAST days were already backfilled. A row recorded TODAY is what the
        // forward-only recorder writes on every refresh, so treating it as evidence of a completed
        // backfill would leave every live demo owner permanently without history. The same row must
        // also not be mistaken for the row of any backdated day the loop writes.
        [Test]
        public async Task HandleTeamRefreshed_OwnThroughputRowRecordedToday_StillGetsItsBackdatedHistory()
        {
            const int teamId = 7;
            const int connectionId = 1886;
            const int SentinelLimit = 500;
            ArrangeConnection(connectionId, isDemo: true);
            ArrangeTeam(teamId, connectionId);

            var todayDate = FixedToday;

            using (var seedContext = CreateContext())
            {
                seedContext.ProcessBehaviorSnapshots.Add(new ProcessBehaviorSnapshot
                {
                    OwnerId = teamId,
                    OwnerType = OwnerType.Team,
                    MetricType = ProcessBehaviorMetricType.Throughput,
                    RecordedAt = todayDate,
                    Unpl = SentinelLimit,
                    Average = SentinelLimit,
                    Lnpl = SentinelLimit,
                });
                await seedContext.SaveChangesAsync();
            }

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(teamId), CancellationToken.None);

            var ourRows = context.ProcessBehaviorSnapshots
                .Where(s => s.OwnerId == teamId && s.OwnerType == OwnerType.Team)
                .ToList();
            var todayRow = ourRows.Single(s => s.RecordedAt == todayDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    ourRows.Count(s => s.RecordedAt < todayDate),
                    Is.EqualTo(14),
                    "today's forward-only row is not a backfilled history — the past window is still owed");
                Assert.That(todayRow.Average, Is.EqualTo(SentinelLimit), "the recorder's own row for today is not a backdated day and stays as recorded");
            }
        }

        private async Task SeedBackdatedHistoryAsync(int teamId, DemoBackfillSeed seed)
        {
            if (seed == DemoBackfillSeed.Nothing)
            {
                return;
            }

            var backdated = FixedToday.AddDays(-3);

            using var seedContext = CreateContext();

            seedContext.PercentilesOverTimeSnapshots.Add(new PercentilesOverTimeSnapshot
            {
                OwnerId = teamId,
                OwnerType = OwnerType.Team,
                MetricType = MetricType.CycleTime,
                Horizon = 30,
                RecordedAt = backdated,
                P50 = 5,
                P70 = 7,
                P85 = 9,
                P95 = 11,
            });

            seedContext.PercentilesOverTimeSnapshots.Add(new PercentilesOverTimeSnapshot
            {
                OwnerId = teamId,
                OwnerType = OwnerType.Team,
                MetricType = MetricType.WorkItemAge,
                Horizon = PercentilesOverTimeSnapshot.NoHorizon,
                RecordedAt = backdated,
                P50 = 5,
                P70 = 7,
                P85 = 9,
                P95 = 11,
            });

            if (seed == DemoBackfillSeed.PercentilesAndThroughput)
            {
                seedContext.ProcessBehaviorSnapshots.Add(new ProcessBehaviorSnapshot
                {
                    OwnerId = teamId,
                    OwnerType = OwnerType.Team,
                    MetricType = ProcessBehaviorMetricType.Throughput,
                    RecordedAt = backdated,
                    Unpl = 20,
                    Average = 12,
                    Lnpl = 4,
                });
            }

            await seedContext.SaveChangesAsync();
        }
    }
}
