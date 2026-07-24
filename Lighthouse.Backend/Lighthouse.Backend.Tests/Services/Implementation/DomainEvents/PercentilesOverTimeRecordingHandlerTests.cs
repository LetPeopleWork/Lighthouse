using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation.DomainEvents;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Services.Implementation.DomainEvents
{
    [TestFixture]
    [Category("epic-5427-percentiles-over-time")]
    public class PercentilesOverTimeRecordingHandlerTests
    {
        private static readonly int[] Horizons = [30, 60, 90];

        private DbContextOptions<LighthouseAppContext> options = null!;
        private Mock<ICryptoService> cryptoServiceMock = null!;
        private Mock<ILogger<LighthouseAppContext>> appContextLoggerMock = null!;

        private Mock<ITeamMetricsService> teamMetricsServiceMock = null!;
        private Mock<IPortfolioMetricsService> portfolioMetricsServiceMock = null!;
        private Mock<IRepository<Team>> teamRepositoryMock = null!;
        private Mock<IRepository<Portfolio>> portfolioRepositoryMock = null!;
        private Mock<ILogger<PercentilesOverTimeRecordingHandler>> handlerLoggerMock = null!;

        [SetUp]
        public void SetUp()
        {
            options = new DbContextOptionsBuilder<LighthouseAppContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            cryptoServiceMock = new Mock<ICryptoService>();
            appContextLoggerMock = new Mock<ILogger<LighthouseAppContext>>();

            teamMetricsServiceMock = new Mock<ITeamMetricsService>();
            portfolioMetricsServiceMock = new Mock<IPortfolioMetricsService>();
            teamRepositoryMock = new Mock<IRepository<Team>>();
            portfolioRepositoryMock = new Mock<IRepository<Portfolio>>();
            handlerLoggerMock = new Mock<ILogger<PercentilesOverTimeRecordingHandler>>();
        }

        private LighthouseAppContext CreateContext()
        {
            return new LighthouseAppContext(options, cryptoServiceMock.Object, appContextLoggerMock.Object);
        }

        private PercentilesOverTimeSnapshotRepository CreateSnapshotRepository(LighthouseAppContext context)
        {
            return new PercentilesOverTimeSnapshotRepository(context, Mock.Of<ILogger<PercentilesOverTimeSnapshotRepository>>());
        }

        private PercentilesOverTimeRecordingHandler CreateSubject(
            LighthouseAppContext context, IPercentilesOverTimeSnapshotRepository? snapshotRepository = null)
        {
            var snapshotRepo = snapshotRepository ?? CreateSnapshotRepository(context);
            return new PercentilesOverTimeRecordingHandler(
                teamMetricsServiceMock.Object,
                portfolioMetricsServiceMock.Object,
                teamRepositoryMock.Object,
                portfolioRepositoryMock.Object,
                snapshotRepo,
                handlerLoggerMock.Object);
        }

        private static Team CreateTeam(int id = 1)
        {
            return new Team
            {
                Id = id,
                Name = $"Test Team {id}",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    Name = "Connection",
                    WorkTrackingSystem = WorkTrackingSystems.Jira,
                },
            };
        }

        private static Portfolio CreatePortfolio(int id = 1)
        {
            return new Portfolio
            {
                Id = id,
                Name = $"Test Portfolio {id}",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    Name = "Connection",
                    WorkTrackingSystem = WorkTrackingSystems.Jira,
                },
            };
        }

        private static List<PercentileValue> Percentiles(int p50, int p70, int p85, int p95)
        {
            return
            [
                new PercentileValue(50, p50),
                new PercentileValue(70, p70),
                new PercentileValue(85, p85),
                new PercentileValue(95, p95),
            ];
        }

        private static DateTime TodayDate => DateTime.Today;

        private DateOnly Today => DateOnly.FromDateTime(TodayDate);

        private async Task<PercentilesOverTimeSnapshot?> FindSnapshot(
            LighthouseAppContext context, int ownerId, OwnerType ownerType, int horizon, DateOnly recordedAt)
        {
            return await context.PercentilesOverTimeSnapshots
                .SingleOrDefaultAsync(s =>
                    s.OwnerId == ownerId &&
                    s.OwnerType == ownerType &&
                    s.MetricType == MetricType.CycleTime &&
                    s.Horizon == horizon &&
                    s.RecordedAt == recordedAt);
        }

        // -----------------------------------------------------------------
        // Scenario 2 — a refresh records today's CT percentiles for every horizon
        // -----------------------------------------------------------------
        [Test]
        public async Task TeamDataRefreshed_RecordsOneRowPerHorizon_WithHorizonScopedPercentiles()
        {
            var team = CreateTeam(42);
            teamRepositoryMock.Setup(x => x.GetById(team.Id)).Returns(team);

            // Each horizon reads an as-of-today window [today-H, today] and yields
            // horizon-specific percentile values so we can prove the right window is used.
            teamMetricsServiceMock
                .Setup(x => x.GetCycleTimePercentilesForTeam(team, TodayDate.AddDays(-30), TodayDate))
                .Returns(Percentiles(11, 12, 13, 14));
            teamMetricsServiceMock
                .Setup(x => x.GetCycleTimePercentilesForTeam(team, TodayDate.AddDays(-60), TodayDate))
                .Returns(Percentiles(21, 22, 23, 24));
            teamMetricsServiceMock
                .Setup(x => x.GetCycleTimePercentilesForTeam(team, TodayDate.AddDays(-90), TodayDate))
                .Returns(Percentiles(31, 32, 33, 34));

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None);

            var total = await context.PercentilesOverTimeSnapshots.CountAsync();
            Assert.That(total, Is.EqualTo(3), "one row per horizon {30,60,90} must be recorded");

            var h30 = await FindSnapshot(context, team.Id, OwnerType.Team, 30, Today);
            var h60 = await FindSnapshot(context, team.Id, OwnerType.Team, 60, Today);
            var h90 = await FindSnapshot(context, team.Id, OwnerType.Team, 90, Today);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(h30, Is.Not.Null);
                Assert.That(h30!.P50, Is.EqualTo(11));
                Assert.That(h30.P70, Is.EqualTo(12));
                Assert.That(h30.P85, Is.EqualTo(13));
                Assert.That(h30.P95, Is.EqualTo(14));
                Assert.That(h30.RecordedAt, Is.EqualTo(Today));

                Assert.That(h60, Is.Not.Null);
                Assert.That(h60!.P50, Is.EqualTo(21));
                Assert.That(h60.P95, Is.EqualTo(24));

                Assert.That(h90, Is.Not.Null);
                Assert.That(h90!.P50, Is.EqualTo(31));
                Assert.That(h90.P95, Is.EqualTo(34));
            }
        }

        [Test]
        public async Task PortfolioFeaturesRefreshed_RecordsOneRowPerHorizon_WithHorizonScopedPercentiles()
        {
            var portfolio = CreatePortfolio(7);
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);

            teamMetricsServiceMock.Reset();
            portfolioMetricsServiceMock
                .Setup(x => x.GetCycleTimePercentilesForPortfolio(portfolio, TodayDate.AddDays(-30), TodayDate))
                .Returns(Percentiles(1, 2, 3, 4));
            portfolioMetricsServiceMock
                .Setup(x => x.GetCycleTimePercentilesForPortfolio(portfolio, TodayDate.AddDays(-60), TodayDate))
                .Returns(Percentiles(5, 6, 7, 8));
            portfolioMetricsServiceMock
                .Setup(x => x.GetCycleTimePercentilesForPortfolio(portfolio, TodayDate.AddDays(-90), TodayDate))
                .Returns(Percentiles(9, 10, 11, 12));

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new PortfolioFeaturesRefreshed(portfolio.Id), CancellationToken.None);

            var total = await context.PercentilesOverTimeSnapshots.CountAsync();
            Assert.That(total, Is.EqualTo(3));

            var h60 = await FindSnapshot(context, portfolio.Id, OwnerType.Portfolio, 60, Today);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(h60, Is.Not.Null);
                Assert.That(h60!.OwnerType, Is.EqualTo(OwnerType.Portfolio));
                Assert.That(h60.MetricType, Is.EqualTo(MetricType.CycleTime));
                Assert.That(h60.P50, Is.EqualTo(5));
                Assert.That(h60.P95, Is.EqualTo(8));
            }
        }

        // -----------------------------------------------------------------
        // Scenario 3 — same-day re-refresh overwrites in place (one row per key)
        // -----------------------------------------------------------------
        [Test]
        public async Task TeamDataRefreshed_SameDayReRefresh_OverwritesInPlace()
        {
            var team = CreateTeam(1);
            teamRepositoryMock.Setup(x => x.GetById(team.Id)).Returns(team);

            SetupTeamPercentiles(team, h30: Percentiles(1, 1, 1, 1), h60: Percentiles(2, 2, 2, 2), h90: Percentiles(3, 3, 3, 3));

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None);

            // Second refresh — same day, changed values.
            SetupTeamPercentiles(team, h30: Percentiles(10, 10, 10, 10), h60: Percentiles(20, 20, 20, 20), h90: Percentiles(30, 30, 30, 30));
            await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None);

            var total = await context.PercentilesOverTimeSnapshots.CountAsync();
            Assert.That(total, Is.EqualTo(3), "exactly one row per (owner, metric, horizon, day) after re-refresh");

            var h30 = await FindSnapshot(context, team.Id, OwnerType.Team, 30, Today);
            Assert.That(h30!.P50, Is.EqualTo(10), "the same-day row must carry the latest values");
        }

        // -----------------------------------------------------------------
        // Scenario 4 — forward-only: only today is recorded, no earlier days
        // -----------------------------------------------------------------
        [Test]
        public async Task TeamDataRefreshed_RecordsOnlyToday_NeverFabricatesEarlierDays()
        {
            var team = CreateTeam(1);
            teamRepositoryMock.Setup(x => x.GetById(team.Id)).Returns(team);
            SetupTeamPercentiles(team, h30: Percentiles(1, 1, 1, 1), h60: Percentiles(2, 2, 2, 2), h90: Percentiles(3, 3, 3, 3));

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None);

            var recordedDays = await context.PercentilesOverTimeSnapshots
                .Select(s => s.RecordedAt)
                .Distinct()
                .ToListAsync();
            Assert.That(recordedDays, Is.EquivalentTo(new[] { Today }),
                "the recorder is forward-only — it may only write today's row, never backfill earlier days");
        }

        // -----------------------------------------------------------------
        // Scenario 5 — failure isolation: recording error does not break the
        // refresh path and emits a structured recording-failed Error log.
        // -----------------------------------------------------------------
        [Test]
        public void TeamDataRefreshed_MetricsReadThrows_DoesNotRethrow_AndLogsStructuredError()
        {
            var team = CreateTeam(1);
            teamRepositoryMock.Setup(x => x.GetById(team.Id)).Returns(team);
            teamMetricsServiceMock
                .Setup(x => x.GetCycleTimePercentilesForTeam(It.IsAny<Team>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Throws(new InvalidOperationException("metrics boom"));

            using var context = CreateContext();
            var subject = CreateSubject(context);

            Assert.DoesNotThrowAsync(
                async () => await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None),
                "a recording failure must not break the refresh path — the dispatcher swallows nothing for us");

            handlerLoggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce,
                "a structured recording-failed Error must be logged when recording throws");
        }

        [Test]
        public void PortfolioFeaturesRefreshed_RepositoryThrows_DoesNotRethrow_AndLogsStructuredError()
        {
            var portfolio = CreatePortfolio(7);
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);
            portfolioMetricsServiceMock
                .Setup(x => x.GetCycleTimePercentilesForPortfolio(It.IsAny<Portfolio>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(Percentiles(1, 2, 3, 4));

            var throwingRepo = new Mock<IPercentilesOverTimeSnapshotRepository>();
            throwingRepo
                .Setup(x => x.GetByPredicate(It.IsAny<Func<PercentilesOverTimeSnapshot, bool>>()))
                .Throws(new InvalidOperationException("repo boom"));

            using var context = CreateContext();
            var subject = CreateSubject(context, throwingRepo.Object);

            Assert.DoesNotThrowAsync(
                async () => await subject.HandleAsync(new PortfolioFeaturesRefreshed(portfolio.Id), CancellationToken.None));

            handlerLoggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        // -----------------------------------------------------------------
        // Null-owner guards — mirror the sibling recorder's contract.
        // -----------------------------------------------------------------
        [Test]
        public async Task TeamDataRefreshed_NullTeam_ReturnsWithoutRecording()
        {
            teamRepositoryMock.Setup(x => x.GetById(999)).Returns((Team?)null);

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(999), CancellationToken.None);

            var count = await context.PercentilesOverTimeSnapshots.CountAsync();
            Assert.That(count, Is.Zero);
        }

        [Test]
        public async Task PortfolioFeaturesRefreshed_NullPortfolio_ReturnsWithoutRecording()
        {
            portfolioRepositoryMock.Setup(x => x.GetById(999)).Returns((Portfolio?)null);

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new PortfolioFeaturesRefreshed(999), CancellationToken.None);

            var count = await context.PercentilesOverTimeSnapshots.CountAsync();
            Assert.That(count, Is.Zero);
        }

        // -----------------------------------------------------------------
        // ValueFor must tolerate a percentile that the metrics reader omits —
        // the missing percentile is recorded as 0, never throwing. (Guards the
        // FirstOrDefault-vs-First distinction in the percentile lookup.)
        // -----------------------------------------------------------------
        [Test]
        public async Task TeamDataRefreshed_ReadingOmitsAPercentile_RecordsZeroForIt_WithoutThrowing()
        {
            var team = CreateTeam(3);
            teamRepositoryMock.Setup(x => x.GetById(team.Id)).Returns(team);

            // The reader returns only P50/P70/P85 for every horizon — P95 is absent.
            List<PercentileValue> PartialReading() =>
            [
                new PercentileValue(50, 8),
                new PercentileValue(70, 9),
                new PercentileValue(85, 10),
            ];
            teamMetricsServiceMock
                .Setup(x => x.GetCycleTimePercentilesForTeam(team, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(PartialReading());

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None);

            var total = await context.PercentilesOverTimeSnapshots.CountAsync();
            var h30 = await FindSnapshot(context, team.Id, OwnerType.Team, 30, Today);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(total, Is.EqualTo(3), "recording must complete for all horizons even when a percentile is missing");
                Assert.That(h30, Is.Not.Null);
                Assert.That(h30!.P50, Is.EqualTo(8));
                Assert.That(h30.P85, Is.EqualTo(10));
                Assert.That(h30.P95, Is.Zero, "an omitted percentile is stored as 0, not an error");
            }
        }

        // -----------------------------------------------------------------
        // The same-day upsert key is the FULL (owner, type, metric, horizon, day)
        // tuple: a foreign owner's row for the same day/horizon must NOT be picked
        // up as "the existing row". Guards the conjunction in the upsert predicate.
        // -----------------------------------------------------------------
        [Test]
        public async Task TeamDataRefreshed_ForeignOwnerRowSameDayAndHorizon_AddsOwnRow_LeavesForeignRowUntouched()
        {
            var team = CreateTeam(42);
            teamRepositoryMock.Setup(x => x.GetById(team.Id)).Returns(team);

            const int foreignOwnerId = 999;
            using (var seedContext = CreateContext())
            {
                // A different team already has a today/horizon-30 cycle-time row.
                seedContext.PercentilesOverTimeSnapshots.Add(new PercentilesOverTimeSnapshot
                {
                    OwnerId = foreignOwnerId,
                    OwnerType = OwnerType.Team,
                    MetricType = MetricType.CycleTime,
                    Horizon = 30,
                    RecordedAt = Today,
                    P50 = 500,
                    P70 = 500,
                    P85 = 500,
                    P95 = 500,
                });
                await seedContext.SaveChangesAsync();
            }

            SetupTeamPercentiles(team, h30: Percentiles(1, 1, 1, 1), h60: Percentiles(2, 2, 2, 2), h90: Percentiles(3, 3, 3, 3));

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None);

            var ownRow = await FindSnapshot(context, team.Id, OwnerType.Team, 30, Today);
            var foreignRow = await FindSnapshot(context, foreignOwnerId, OwnerType.Team, 30, Today);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ownRow, Is.Not.Null, "the recorder must add its own row, not reuse a foreign owner's row");
                Assert.That(ownRow!.P50, Is.EqualTo(1));
                Assert.That(foreignRow, Is.Not.Null);
                Assert.That(foreignRow!.P50, Is.EqualTo(500), "the foreign owner's row must be left untouched");
            }
        }

        // -----------------------------------------------------------------
        // Reading the point-in-time percentiles warms the shared metrics cache
        // under the same (owner, window) keys the Flow Overview widgets read.
        // The recorder MUST invalidate that cache afterwards so the UI recomputes
        // on settled data instead of serving the recorder's mid-refresh snapshot
        // (regression: NamedCycleTimePercentiles E2E — the Default percentile was
        // served the recorder's stale, partially-seeded value).
        // -----------------------------------------------------------------
        [Test]
        public async Task TeamDataRefreshed_AfterRecording_InvalidatesTeamMetricsCache()
        {
            var team = CreateTeam(1);
            teamRepositoryMock.Setup(x => x.GetById(team.Id)).Returns(team);
            SetupTeamPercentiles(team, h30: Percentiles(1, 1, 1, 1), h60: Percentiles(2, 2, 2, 2), h90: Percentiles(3, 3, 3, 3));

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None);

            teamMetricsServiceMock.Verify(
                x => x.InvalidateTeamMetrics(team),
                Times.Once,
                "reading percentiles warms the UI metrics cache — the recorder must invalidate it so the UI recomputes on settled data");
        }

        [Test]
        public async Task TeamDataRefreshed_MetricsReadThrows_StillInvalidatesTeamMetricsCache()
        {
            var team = CreateTeam(1);
            teamRepositoryMock.Setup(x => x.GetById(team.Id)).Returns(team);
            teamMetricsServiceMock
                .Setup(x => x.GetCycleTimePercentilesForTeam(It.IsAny<Team>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Throws(new InvalidOperationException("metrics boom"));

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None);

            teamMetricsServiceMock.Verify(
                x => x.InvalidateTeamMetrics(team),
                Times.Once,
                "cache cleanup runs in a finally — a mid-loop failure must still leave the warmed cache invalidated");
        }

        [Test]
        public async Task PortfolioFeaturesRefreshed_AfterRecording_InvalidatesPortfolioMetricsCache()
        {
            var portfolio = CreatePortfolio(7);
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);
            portfolioMetricsServiceMock
                .Setup(x => x.GetCycleTimePercentilesForPortfolio(portfolio, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(Percentiles(1, 2, 3, 4));

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new PortfolioFeaturesRefreshed(portfolio.Id), CancellationToken.None);

            portfolioMetricsServiceMock.Verify(
                x => x.InvalidatePortfolioMetrics(portfolio),
                Times.Once,
                "the portfolio recorder must invalidate the portfolio metrics cache it warmed");
        }

        private void SetupTeamPercentiles(
            Team team, List<PercentileValue> h30, List<PercentileValue> h60, List<PercentileValue> h90)
        {
            teamMetricsServiceMock
                .Setup(x => x.GetCycleTimePercentilesForTeam(team, TodayDate.AddDays(-30), TodayDate))
                .Returns(h30);
            teamMetricsServiceMock
                .Setup(x => x.GetCycleTimePercentilesForTeam(team, TodayDate.AddDays(-60), TodayDate))
                .Returns(h60);
            teamMetricsServiceMock
                .Setup(x => x.GetCycleTimePercentilesForTeam(team, TodayDate.AddDays(-90), TodayDate))
                .Returns(h90);
        }
    }
}
