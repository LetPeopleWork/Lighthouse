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
    public class ProcessBehaviorRecordingHandlerTests
    {
        // ADR-107 observability contract: operator alerting filters on the metric FAMILY, never the
        // metric type. The NPL triple ships its own family, separate from "Percentiles".
        private const string ProcessBehaviorFamily = "ProcessBehavior";

        // The day grain the recorder snapshots against, mirroring the point-in-time throughputPbc
        // widget: BaseMetricsView asks for [today - defaultDateRange, today]. For a team the
        // default range is the span of its own throughput history window (Team.ThroughputHistory
        // defaults to 30 => a 29-day span); PortfolioMetricsView hard-codes 90.
        private const int DefaultTeamLookbackDays = 29;
        private const int PortfolioLookbackDays = 90;

        // A team pinning fixed throughput dates has no as-of-today range of its own, so the recorder
        // falls back to a plain 30-day window ending today.
        private const int FixedDatesTeamLookbackDays = 30;

        private DbContextOptions<LighthouseAppContext> options = null!;
        private Mock<ICryptoService> cryptoServiceMock = null!;
        private Mock<ILogger<LighthouseAppContext>> appContextLoggerMock = null!;

        private Mock<ITeamMetricsService> teamMetricsServiceMock = null!;
        private Mock<IPortfolioMetricsService> portfolioMetricsServiceMock = null!;
        private Mock<IRepository<Team>> teamRepositoryMock = null!;
        private Mock<IRepository<Portfolio>> portfolioRepositoryMock = null!;
        private Mock<ILogger<ProcessBehaviorRecordingHandler>> handlerLoggerMock = null!;

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
            handlerLoggerMock = new Mock<ILogger<ProcessBehaviorRecordingHandler>>();
        }

        private static DateTime TodayDate => DateTime.Today;

        private static DateOnly Today => DateOnly.FromDateTime(TodayDate);

        private LighthouseAppContext CreateContext()
        {
            return new LighthouseAppContext(options, cryptoServiceMock.Object, appContextLoggerMock.Object);
        }

        private ProcessBehaviorRecordingHandler CreateSubject(
            LighthouseAppContext context, IProcessBehaviorSnapshotRepository? snapshotRepository = null)
        {
            var snapshotRepo = snapshotRepository ?? new ProcessBehaviorSnapshotRepository(
                context, Mock.Of<ILogger<ProcessBehaviorSnapshotRepository>>());

            return new ProcessBehaviorRecordingHandler(
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

        private static ProcessBehaviourChart ReadyChart(int unpl, int average, int lnpl)
        {
            return new ProcessBehaviourChart
            {
                Status = BaselineStatus.Ready,
                XAxisKind = XAxisKind.Date,
                Average = average,
                UpperNaturalProcessLimit = unpl,
                LowerNaturalProcessLimit = lnpl,
            };
        }

        private void SetupTeamThroughputChart(Team team, ProcessBehaviourChart chart)
        {
            teamMetricsServiceMock
                .Setup(x => x.GetThroughputProcessBehaviourChart(
                    team, TodayDate.AddDays(-DefaultTeamLookbackDays), TodayDate))
                .Returns(chart);
        }

        private void SetupPortfolioThroughputChart(Portfolio portfolio, ProcessBehaviourChart chart)
        {
            portfolioMetricsServiceMock
                .Setup(x => x.GetThroughputProcessBehaviourChart(
                    portfolio, TodayDate.AddDays(-PortfolioLookbackDays), TodayDate))
                .Returns(chart);
        }

        private static Task<ProcessBehaviorSnapshot?> FindSnapshot(
            LighthouseAppContext context, int ownerId, OwnerType ownerType, DateOnly recordedAt)
        {
            return context.ProcessBehaviorSnapshots
                .SingleOrDefaultAsync(s =>
                    s.OwnerId == ownerId &&
                    s.OwnerType == ownerType &&
                    s.MetricType == ProcessBehaviorMetricType.Throughput &&
                    s.RecordedAt == recordedAt);
        }

        private static string? MetricFamilyOf(object state)
        {
            if (state is not IEnumerable<KeyValuePair<string, object?>> properties)
            {
                return null;
            }

            return properties.FirstOrDefault(p => p.Key == "MetricFamily").Value as string;
        }

        private void VerifyRecordingFailureLoggedWithProcessBehaviorFamily(string because)
        {
            handlerLoggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, _) => MetricFamilyOf(state) == ProcessBehaviorFamily),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce,
                because);
        }

        private void VerifyNoRecordingFailureLoggedUnderAnyOtherFamily()
        {
            handlerLoggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, _) => MetricFamilyOf(state) != ProcessBehaviorFamily),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never,
                "the metric TYPE (Throughput) must never leak into {MetricFamily} — operator alert rules filter on the family");
        }

        // -----------------------------------------------------------------
        // Milestone-3 Scenario 11 — the shared daily pipeline records today's
        // Throughput natural process limits on the very refresh events that
        // already drive the percentile families.
        // -----------------------------------------------------------------
        [Test]
        public async Task TeamDataRefreshed_ReadyBaseline_RecordsTodaysThroughputLimits()
        {
            var team = CreateTeam(42);
            teamRepositoryMock.Setup(x => x.GetById(team.Id)).Returns(team);
            SetupTeamThroughputChart(team, ReadyChart(unpl: 14, average: 9, lnpl: 4));

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None);

            var snapshot = await FindSnapshot(context, team.Id, OwnerType.Team, Today);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(snapshot, Is.Not.Null, "the same pipeline that records percentiles must record the NPL triple");
                Assert.That(snapshot!.Unpl, Is.EqualTo(14));
                Assert.That(snapshot.Average, Is.EqualTo(9));
                Assert.That(snapshot.Lnpl, Is.EqualTo(4));
                Assert.That(snapshot.OwnerType, Is.EqualTo(OwnerType.Team));
                Assert.That(snapshot.MetricType, Is.EqualTo(ProcessBehaviorMetricType.Throughput));
                Assert.That(snapshot.RecordedAt, Is.EqualTo(Today), "the snapshot is stamped with real today, never a harness sync day");
            }
        }

        [Test]
        public async Task PortfolioFeaturesRefreshed_ReadyBaseline_RecordsTodaysThroughputLimits()
        {
            var portfolio = CreatePortfolio(7);
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);
            SetupPortfolioThroughputChart(portfolio, ReadyChart(unpl: 21, average: 13, lnpl: 5));

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new PortfolioFeaturesRefreshed(portfolio.Id), CancellationToken.None);

            var snapshot = await FindSnapshot(context, portfolio.Id, OwnerType.Portfolio, Today);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(snapshot, Is.Not.Null);
                Assert.That(snapshot!.Unpl, Is.EqualTo(21));
                Assert.That(snapshot.Average, Is.EqualTo(13));
                Assert.That(snapshot.Lnpl, Is.EqualTo(5));
                Assert.That(snapshot.OwnerType, Is.EqualTo(OwnerType.Portfolio));
            }
        }

        // -----------------------------------------------------------------
        // HONESTY GATE — ProcessBehaviourChart.NotReady returns Average = UNPL =
        // LNPL = 0. Persisting that triple would render three flat lines pinned at
        // zero: a process the team never had. Nothing is written unless Ready.
        // -----------------------------------------------------------------
        [TestCase(BaselineStatus.BaselineMissing)]
        [TestCase(BaselineStatus.BaselineInvalid)]
        [TestCase(BaselineStatus.InsufficientData)]
        public async Task TeamDataRefreshed_BaselineNotReady_WritesNoRow(BaselineStatus status)
        {
            var team = CreateTeam(1);
            teamRepositoryMock.Setup(x => x.GetById(team.Id)).Returns(team);
            SetupTeamThroughputChart(team, ProcessBehaviourChart.NotReady(status, "not ready"));

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None);

            var rows = await context.ProcessBehaviorSnapshots.CountAsync();
            Assert.That(rows, Is.Zero, $"a {status} chart carries zeroed limits — recording them would fabricate a process");
        }

        [TestCase(BaselineStatus.BaselineMissing)]
        [TestCase(BaselineStatus.BaselineInvalid)]
        [TestCase(BaselineStatus.InsufficientData)]
        public async Task PortfolioFeaturesRefreshed_BaselineNotReady_WritesNoRow(BaselineStatus status)
        {
            var portfolio = CreatePortfolio(7);
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);
            SetupPortfolioThroughputChart(portfolio, ProcessBehaviourChart.NotReady(status, "not ready"));

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new PortfolioFeaturesRefreshed(portfolio.Id), CancellationToken.None);

            var rows = await context.ProcessBehaviorSnapshots.CountAsync();
            Assert.That(rows, Is.Zero, $"a {status} chart carries zeroed limits — recording them would fabricate a process");
        }

        // -----------------------------------------------------------------
        // HONESTY GATE, part 2 (US-05 AC4 / D6) — Ready is not the same as "has a
        // process". A valid baseline window that happens to contain no closed items
        // yields all-zero values, so XmRCalculator.Calculate returns
        // Average = UNPL = LNPL = 0 while the builder still stamps Status = Ready.
        // Persisting that triple would draw three flat lines pinned at zero, a process
        // the owner never had. An absent row is the honest empty state the widget
        // renders as "builds forward from today — no snapshots recorded yet".
        // -----------------------------------------------------------------
        [Test]
        public async Task TeamDataRefreshed_ReadyChartWithFullyCollapsedBand_WritesNoRow()
        {
            var team = CreateTeam(1);
            teamRepositoryMock.Setup(x => x.GetById(team.Id)).Returns(team);
            SetupTeamThroughputChart(team, ReadyChart(unpl: 0, average: 0, lnpl: 0));

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None);

            var rows = await context.ProcessBehaviorSnapshots.CountAsync();
            Assert.That(rows, Is.Zero, "a Ready chart whose whole band collapsed to zero describes no process — recording it would fabricate one");
        }

        [Test]
        public async Task PortfolioFeaturesRefreshed_ReadyChartWithFullyCollapsedBand_WritesNoRow()
        {
            var portfolio = CreatePortfolio(7);
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);
            SetupPortfolioThroughputChart(portfolio, ReadyChart(unpl: 0, average: 0, lnpl: 0));

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new PortfolioFeaturesRefreshed(portfolio.Id), CancellationToken.None);

            var rows = await context.ProcessBehaviorSnapshots.CountAsync();
            Assert.That(rows, Is.Zero, "the gate lives in the shared per-metric-type recording step, so both owner types inherit it");
        }

        // The gate stays as narrow as the honesty claim: it fires only when the WHOLE band collapsed.
        // XmRCalculator clamps a negative lower limit to zero for zero-bounded data, so a real, busy
        // process routinely reports Lnpl == 0 — folding Lnpl into the predicate would stop recording
        // real data. A live upper limit with a zero centre line is likewise still a band.
        [TestCase(6, 3, 0, TestName = "TeamDataRefreshed_ReadyChartWithClampedLowerLimit_StillRecords")]
        [TestCase(5, 0, 0, TestName = "TeamDataRefreshed_ReadyChartWithZeroCentreLineButLiveUpperLimit_StillRecords")]
        public async Task TeamDataRefreshed_ReadyChartWithPartiallyZeroedBand_StillRecords(int unpl, int average, int lnpl)
        {
            var team = CreateTeam(5);
            teamRepositoryMock.Setup(x => x.GetById(team.Id)).Returns(team);
            SetupTeamThroughputChart(team, ReadyChart(unpl, average, lnpl));

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None);

            var snapshot = await FindSnapshot(context, team.Id, OwnerType.Team, Today);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(snapshot, Is.Not.Null, "only a fully collapsed band is the empty state — a partially zeroed band is real data");
                Assert.That(snapshot!.Unpl, Is.EqualTo(unpl));
                Assert.That(snapshot.Average, Is.EqualTo(average));
                Assert.That(snapshot.Lnpl, Is.EqualTo(lnpl));
            }
        }

        // Same-day re-run: a refresh that reads back a collapsed band must not overwrite a real
        // reading with zeros. The gate returns before the upsert, so today's row keeps its values.
        [Test]
        public async Task TeamDataRefreshed_SameDayReRefreshReadsBackACollapsedBand_KeepsTheRecordedReading()
        {
            var team = CreateTeam(1);
            teamRepositoryMock.Setup(x => x.GetById(team.Id)).Returns(team);
            SetupTeamThroughputChart(team, ReadyChart(unpl: 12, average: 8, lnpl: 4));

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None);

            SetupTeamThroughputChart(team, ReadyChart(unpl: 0, average: 0, lnpl: 0));
            await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None);

            var rows = await context.ProcessBehaviorSnapshots.CountAsync();
            var snapshot = await FindSnapshot(context, team.Id, OwnerType.Team, Today);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(rows, Is.EqualTo(1), "a collapsed re-read adds no row either");
                Assert.That(snapshot!.Unpl, Is.EqualTo(12), "a zeroed re-read must not overwrite a real reading");
                Assert.That(snapshot.Average, Is.EqualTo(8));
                Assert.That(snapshot.Lnpl, Is.EqualTo(4));
            }
        }

        // -----------------------------------------------------------------
        // Idempotency (DDD-5) — one row per (owner, type, metric, day).
        // -----------------------------------------------------------------
        [Test]
        public async Task TeamDataRefreshed_SameDayReRefresh_OverwritesInPlace()
        {
            var team = CreateTeam(1);
            teamRepositoryMock.Setup(x => x.GetById(team.Id)).Returns(team);
            SetupTeamThroughputChart(team, ReadyChart(unpl: 10, average: 6, lnpl: 2));

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None);

            SetupTeamThroughputChart(team, ReadyChart(unpl: 30, average: 20, lnpl: 10));
            await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None);

            var rows = await context.ProcessBehaviorSnapshots.CountAsync();
            var snapshot = await FindSnapshot(context, team.Id, OwnerType.Team, Today);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(rows, Is.EqualTo(1), "exactly one row per (owner, metric, day) after a same-day re-refresh");
                Assert.That(snapshot!.Unpl, Is.EqualTo(30), "the surviving row carries the latest reading");
                Assert.That(snapshot.Average, Is.EqualTo(20));
                Assert.That(snapshot.Lnpl, Is.EqualTo(10));
            }
        }

        [Test]
        public async Task TeamDataRefreshed_ForeignOwnerRowSameDay_AddsOwnRow_LeavesForeignRowUntouched()
        {
            var team = CreateTeam(42);
            teamRepositoryMock.Setup(x => x.GetById(team.Id)).Returns(team);

            const int foreignOwnerId = 999;
            using (var seedContext = CreateContext())
            {
                seedContext.ProcessBehaviorSnapshots.Add(new ProcessBehaviorSnapshot
                {
                    OwnerId = foreignOwnerId,
                    OwnerType = OwnerType.Team,
                    MetricType = ProcessBehaviorMetricType.Throughput,
                    RecordedAt = Today,
                    Unpl = 500,
                    Average = 500,
                    Lnpl = 500,
                });
                await seedContext.SaveChangesAsync();
            }

            SetupTeamThroughputChart(team, ReadyChart(unpl: 3, average: 2, lnpl: 1));

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None);

            var ownRow = await FindSnapshot(context, team.Id, OwnerType.Team, Today);
            var foreignRow = await FindSnapshot(context, foreignOwnerId, OwnerType.Team, Today);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ownRow, Is.Not.Null, "the recorder must add its own row, not reuse a foreign owner's row");
                Assert.That(ownRow!.Unpl, Is.EqualTo(3));
                Assert.That(foreignRow, Is.Not.Null);
                Assert.That(foreignRow!.Unpl, Is.EqualTo(500), "the foreign owner's row must be left untouched");
            }
        }

        // -----------------------------------------------------------------
        // Failure isolation (US-02 AC4 / DDD-6) — the refresh path stays green and
        // the structured Error names the ProcessBehavior FAMILY.
        // -----------------------------------------------------------------
        [Test]
        public void TeamDataRefreshed_ChartReadThrows_DoesNotRethrow_AndLogsProcessBehaviorFamily()
        {
            var team = CreateTeam(1);
            teamRepositoryMock.Setup(x => x.GetById(team.Id)).Returns(team);
            teamMetricsServiceMock
                .Setup(x => x.GetThroughputProcessBehaviourChart(
                    It.IsAny<Team>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Throws(new InvalidOperationException("pbc boom"));

            using var context = CreateContext();
            var subject = CreateSubject(context);

            Assert.DoesNotThrowAsync(
                async () => await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None),
                "a recording failure must not break the refresh path — the handler owns its own observability");

            VerifyRecordingFailureLoggedWithProcessBehaviorFamily(
                "the recording-failed Error carries MetricFamily = ProcessBehavior per the ADR-107 contract");
            VerifyNoRecordingFailureLoggedUnderAnyOtherFamily();
        }

        [Test]
        public void PortfolioFeaturesRefreshed_RepositoryThrows_DoesNotRethrow_AndLogsProcessBehaviorFamily()
        {
            var portfolio = CreatePortfolio(7);
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);
            SetupPortfolioThroughputChart(portfolio, ReadyChart(unpl: 5, average: 3, lnpl: 1));

            var throwingRepo = new Mock<IProcessBehaviorSnapshotRepository>();
            throwingRepo
                .Setup(x => x.GetByPredicate(It.IsAny<Func<ProcessBehaviorSnapshot, bool>>()))
                .Throws(new InvalidOperationException("repo boom"));

            using var context = CreateContext();
            var subject = CreateSubject(context, throwingRepo.Object);

            Assert.DoesNotThrowAsync(
                async () => await subject.HandleAsync(new PortfolioFeaturesRefreshed(portfolio.Id), CancellationToken.None));

            VerifyRecordingFailureLoggedWithProcessBehaviorFamily(
                "a repository failure is reported under the ProcessBehavior family too");
            VerifyNoRecordingFailureLoggedUnderAnyOtherFamily();
        }

        // The inner per-metric-type try only wraps the compute + stage step. A failure raised while
        // FLUSHING the staged rows happens after that scope has closed, so it can only be caught by
        // the outer handler — and it must be reported under the same family, or a persistence outage
        // becomes an unalerted silent data gap.
        [Test]
        public void TeamDataRefreshed_SnapshotFlushThrows_DoesNotRethrow_AndLogsProcessBehaviorFamily()
        {
            var team = CreateTeam(1);
            teamRepositoryMock.Setup(x => x.GetById(team.Id)).Returns(team);
            SetupTeamThroughputChart(team, ReadyChart(unpl: 9, average: 6, lnpl: 3));

            var throwingRepo = new Mock<IProcessBehaviorSnapshotRepository>();
            throwingRepo
                .Setup(x => x.GetByPredicate(It.IsAny<Func<ProcessBehaviorSnapshot, bool>>()))
                .Returns((ProcessBehaviorSnapshot?)null);
            throwingRepo
                .Setup(x => x.Save())
                .ThrowsAsync(new InvalidOperationException("flush boom"));

            using var context = CreateContext();
            var subject = CreateSubject(context, throwingRepo.Object);

            Assert.DoesNotThrowAsync(
                async () => await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None),
                "a flush failure must not break the refresh path either");

            VerifyRecordingFailureLoggedWithProcessBehaviorFamily(
                "a failure raised outside the per-metric-type scope is still reported under the ProcessBehavior family");
            VerifyNoRecordingFailureLoggedUnderAnyOtherFamily();
        }

        // -----------------------------------------------------------------
        // Standing slice-01 regression guard — reading the point-in-time chart warms
        // the shared metrics cache under the same (owner, window) keys the widgets
        // read, so the recorder must invalidate it in a finally: on the success path
        // AND on the failure path.
        // -----------------------------------------------------------------
        [TestCase(true, TestName = "TeamDataRefreshed_ChartReadThrows_StillInvalidatesTeamMetricsCache")]
        [TestCase(false, TestName = "TeamDataRefreshed_AfterRecording_InvalidatesTeamMetricsCache")]
        public async Task TeamDataRefreshed_InvalidatesTeamMetricsCache(bool chartReadThrows)
        {
            var team = CreateTeam(1);
            teamRepositoryMock.Setup(x => x.GetById(team.Id)).Returns(team);

            if (chartReadThrows)
            {
                teamMetricsServiceMock
                    .Setup(x => x.GetThroughputProcessBehaviourChart(
                        It.IsAny<Team>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                    .Throws(new InvalidOperationException("pbc boom"));
            }
            else
            {
                SetupTeamThroughputChart(team, ReadyChart(unpl: 7, average: 4, lnpl: 1));
            }

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None);

            teamMetricsServiceMock.Verify(
                x => x.InvalidateTeamMetrics(team),
                Times.Once,
                "cache cleanup runs in a finally — both the success and the failure path must leave the warmed cache invalidated");
        }

        [TestCase(true, TestName = "PortfolioFeaturesRefreshed_ChartReadThrows_StillInvalidatesPortfolioMetricsCache")]
        [TestCase(false, TestName = "PortfolioFeaturesRefreshed_AfterRecording_InvalidatesPortfolioMetricsCache")]
        public async Task PortfolioFeaturesRefreshed_InvalidatesPortfolioMetricsCache(bool chartReadThrows)
        {
            var portfolio = CreatePortfolio(7);
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);

            if (chartReadThrows)
            {
                portfolioMetricsServiceMock
                    .Setup(x => x.GetThroughputProcessBehaviourChart(
                        It.IsAny<Portfolio>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                    .Throws(new InvalidOperationException("pbc boom"));
            }
            else
            {
                SetupPortfolioThroughputChart(portfolio, ReadyChart(unpl: 7, average: 4, lnpl: 1));
            }

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new PortfolioFeaturesRefreshed(portfolio.Id), CancellationToken.None);

            portfolioMetricsServiceMock.Verify(
                x => x.InvalidatePortfolioMetrics(portfolio),
                Times.Once,
                "the portfolio recorder must invalidate the portfolio metrics cache it warmed, on both paths");
        }

        // -----------------------------------------------------------------
        // Null-owner guards — mirror the sibling recorders' contract.
        // -----------------------------------------------------------------
        [Test]
        public async Task TeamDataRefreshed_NullTeam_ReturnsWithoutRecording()
        {
            teamRepositoryMock.Setup(x => x.GetById(999)).Returns((Team?)null);

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(999), CancellationToken.None);

            var rows = await context.ProcessBehaviorSnapshots.CountAsync();
            Assert.That(rows, Is.Zero);
        }

        [Test]
        public async Task PortfolioFeaturesRefreshed_NullPortfolio_ReturnsWithoutRecording()
        {
            portfolioRepositoryMock.Setup(x => x.GetById(999)).Returns((Portfolio?)null);

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new PortfolioFeaturesRefreshed(999), CancellationToken.None);

            var rows = await context.ProcessBehaviorSnapshots.CountAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(rows, Is.Zero);
                portfolioMetricsServiceMock.Verify(
                    x => x.GetThroughputProcessBehaviourChart(
                        It.IsAny<Portfolio>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                    Times.Never,
                    "a vanished portfolio must short-circuit before the recorder reads a chart for a null owner");
                portfolioMetricsServiceMock.Verify(
                    x => x.InvalidatePortfolioMetrics(It.IsAny<Portfolio>()),
                    Times.Never,
                    "nothing was read, so there is no warmed cache entry to invalidate");
            }
        }

        // -----------------------------------------------------------------
        // Day grain — the recorder asks for the same as-of-today window the
        // point-in-time throughputPbc widget asks for, so the recorded triple
        // equals what the user sees today.
        // -----------------------------------------------------------------
        [Test]
        public async Task TeamDataRefreshed_ReadsTheSameAsOfTodayWindowTheWidgetUses()
        {
            var team = CreateTeam(3);
            teamRepositoryMock.Setup(x => x.GetById(team.Id)).Returns(team);
            SetupTeamThroughputChart(team, ReadyChart(unpl: 2, average: 1, lnpl: 0));

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None);

            teamMetricsServiceMock.Verify(
                x => x.GetThroughputProcessBehaviourChart(
                    team, TodayDate.AddDays(-DefaultTeamLookbackDays), TodayDate),
                Times.Once,
                "the team window mirrors BaseMetricsView's default range — the span of the team's own throughput history, ending today");
        }

        [Test]
        public async Task PortfolioFeaturesRefreshed_ReadsTheSameAsOfTodayWindowTheWidgetUses()
        {
            var portfolio = CreatePortfolio(4);
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);
            SetupPortfolioThroughputChart(portfolio, ReadyChart(unpl: 2, average: 1, lnpl: 0));

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new PortfolioFeaturesRefreshed(portfolio.Id), CancellationToken.None);

            portfolioMetricsServiceMock.Verify(
                x => x.GetThroughputProcessBehaviourChart(
                    portfolio, TodayDate.AddDays(-PortfolioLookbackDays), TodayDate),
                Times.Once,
                "PortfolioMetricsView hard-codes defaultDateRange={90} — the recorder must match it");
        }

        // A team that pins fixed throughput dates has an arbitrary window that may sit entirely in
        // the past. Recording "today" against a past window would stamp today's date on limits the
        // team does not have today, so the recorder falls back to a plain as-of-today window instead
        // of the team's pinned span. The pinned span here is deliberately far from the fallback, so a
        // recorder that derived the window from the settings reads a visibly different range.
        [Test]
        public async Task TeamDataRefreshed_TeamPinsFixedThroughputDates_FallsBackToAnAsOfTodayWindow()
        {
            var team = CreateTeam(11);
            team.UseFixedDatesForThroughput = true;
            team.ThroughputHistoryStartDate = TodayDate.AddDays(-90);
            team.ThroughputHistoryEndDate = TodayDate.AddDays(-10);
            teamRepositoryMock.Setup(x => x.GetById(team.Id)).Returns(team);

            teamMetricsServiceMock
                .Setup(x => x.GetThroughputProcessBehaviourChart(
                    team, TodayDate.AddDays(-FixedDatesTeamLookbackDays), TodayDate))
                .Returns(ReadyChart(unpl: 8, average: 5, lnpl: 2));

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None);

            var snapshot = await FindSnapshot(context, team.Id, OwnerType.Team, Today);
            using (Assert.EnterMultipleScope())
            {
                teamMetricsServiceMock.Verify(
                    x => x.GetThroughputProcessBehaviourChart(
                        team, TodayDate.AddDays(-FixedDatesTeamLookbackDays), TodayDate),
                    Times.Once,
                    "a fixed past window is not an as-of-today window — the recorder uses the 30-day fallback range");
                Assert.That(snapshot, Is.Not.Null, "the fallback window still yields a recorded day");
                Assert.That(snapshot!.Average, Is.EqualTo(5));
            }
        }
    }
}
