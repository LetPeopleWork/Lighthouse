using System.Linq.Expressions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation.BackgroundServices;
using Lighthouse.Backend.Services.Implementation.DatabaseManagement;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.BackgroundServices;
using Lighthouse.Backend.Services.Interfaces.DatabaseManagement;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices
{
    /// <summary>
    /// The filler on its own, with no background reader running, so that what is waiting and what a
    /// pass did are decided by the test alone. Everything a pass reaches for is a port answered here;
    /// the queue, the walk and what it tells the log are the real ones.
    /// </summary>
    [Category("story-6053-reconstruct-over-time-history")]
    public class OverTimeHistoryFillerTest
    {
        /// <summary>
        /// How many owners the queue holds. Restated rather than read off the filler: were the queue to
        /// grow, the owner these tests expect to be turned away would be taken instead and a pass would
        /// run for it twice, which fails loudly rather than passing for the wrong reason.
        /// </summary>
        private const int OwnersTheQueueHolds = 256;

        private const int TeamId = 7;

        private const string TheStoreRefusedTheDay = "the store refused the day";

        private static readonly LogLevel[] OnlyAnError = [LogLevel.Error];

        private static readonly DateTimeOffset Now = new(2026, 9, 22, 9, 0, 0, TimeSpan.Zero);

        private static readonly DateOnly Today = new(2026, 9, 22);

        private readonly List<WorkItem> storedItems = [];

        private Mock<IRepository<Team>> teamRepositoryMock = null!;

        private Mock<ITeamMetricsService> teamMetricsMock = null!;

        private Mock<IWorkItemRepository> workItemRepositoryMock = null!;

        private Mock<IPercentileSnapshotWriter> percentileWriterMock = null!;

        private Mock<IProcessBehaviorSnapshotWriter> limitWriterMock = null!;

        private Mock<IOverTimeHistoryFillSwitch> fillSwitchMock = null!;

        private RecordingLogger<OverTimeHistoryFiller> logger = null!;

        private ServiceProvider services = null!;

        private OverTimeHistoryFiller subject = null!;

        [SetUp]
        public void Setup()
        {
            storedItems.Clear();

            teamRepositoryMock = new Mock<IRepository<Team>>();
            teamMetricsMock = new Mock<ITeamMetricsService>();
            workItemRepositoryMock = new Mock<IWorkItemRepository>();
            percentileWriterMock = new Mock<IPercentileSnapshotWriter>();
            limitWriterMock = new Mock<IProcessBehaviorSnapshotWriter>();
            fillSwitchMock = new Mock<IOverTimeHistoryFillSwitch>();
            logger = new RecordingLogger<OverTimeHistoryFiller>();

            workItemRepositoryMock
                .Setup(repository => repository.GetAllByPredicate(It.IsAny<Expression<Func<WorkItem, bool>>>()))
                .Returns((Expression<Func<WorkItem, bool>> predicate) => storedItems.AsQueryable().Where(predicate));
            limitWriterMock.Setup(writer => writer.FamiliesFor(It.IsAny<Team>())).Returns([]);
            fillSwitchMock.Setup(fillSwitch => fillSwitch.IsSwitchedOn()).Returns(true);

            services = new ServiceCollection()
                .AddSingleton(teamRepositoryMock.Object)
                .AddSingleton(teamMetricsMock.Object)
                .AddSingleton(workItemRepositoryMock.Object)
                .AddSingleton(percentileWriterMock.Object)
                .AddSingleton(limitWriterMock.Object)
                .AddSingleton(fillSwitchMock.Object)
                .AddSingleton<ILighthouseClock>(new FakeLighthouseClock(Now))
                .AddSingleton(_ => new DatabaseMaintenanceGate(Mock.Of<IUpdateStatusStore>(), subject))
                .AddSingleton<ReconstructionMemo>()
                .BuildServiceProvider();

            subject = new OverTimeHistoryFiller(services.GetRequiredService<IServiceScopeFactory>(), logger);
        }

        [TearDown]
        public void TearDown()
        {
            subject.Dispose();
            services.Dispose();
        }

        [Test]
        public async Task AnOwnerTurnedAwayByAFullQueue_IsTakenOnceTheQueueHasRoomAgain()
        {
            for (var ownerId = 1; ownerId <= OwnersTheQueueHolds; ownerId++)
            {
                subject.AskFor(RequestFor(ownerId, Today));
            }

            const int turnedAway = OwnersTheQueueHolds + 1;
            subject.AskFor(RequestFor(turnedAway, Today));
            await subject.DrainAsync(CancellationToken.None);

            subject.AskFor(RequestFor(turnedAway, Today));
            await subject.DrainAsync(CancellationToken.None);

            teamRepositoryMock.Verify(repository => repository.GetById(turnedAway), Times.Once);
        }

        /// <summary>
        /// Emptying the owner's metrics cache makes the dashboard recompute everything it shows, which
        /// is only worth paying for when the pass itself read through that cache - and a pass that
        /// worked out no day read nothing.
        /// </summary>
        [Test]
        public async Task APassThatWorksOutNoDay_LeavesTheOwnersCachedMetricsInPlace()
        {
            var team = GivenATeamLastObservedOn(Today.AddDays(-10));
            GivenTheTeamFinishedAnItemOn(Today.AddDays(-60));

            subject.AskFor(RequestFor(TeamId, Today.AddDays(-70), Today.AddDays(-1)));
            await subject.DrainAsync(CancellationToken.None);

            teamMetricsMock.Verify(metrics => metrics.InvalidateTeamMetrics(team), Times.Never);
        }

        [Test]
        public async Task APassThatWorksOutADay_EmptiesTheOwnersCachedMetrics()
        {
            var team = GivenATeamLastObservedOn(Today);
            GivenTheTeamFinishedAnItemOn(Today.AddDays(-60));

            subject.AskFor(RequestFor(TeamId, Today.AddDays(-1)));
            await subject.DrainAsync(CancellationToken.None);

            teamMetricsMock.Verify(metrics => metrics.InvalidateTeamMetrics(team), Times.Once);
        }

        /// <summary>
        /// Shutdown drains the queue on a token the host cancels once its shutdown allowance is spent.
        /// Past that point every further pass is time the host no longer has.
        /// </summary>
        [Test]
        public async Task ADrainToldToStopPartWay_StartsNoFurtherPass()
        {
            const int stillWaiting = TeamId + 1;
            using var shutdown = new CancellationTokenSource();
            teamRepositoryMock
                .Setup(repository => repository.GetById(TeamId))
                .Callback(() => shutdown.Cancel())
                .Returns((Team?)null);

            subject.AskFor(RequestFor(TeamId, Today));
            subject.AskFor(RequestFor(stillWaiting, Today));
            await subject.DrainAsync(shutdown.Token);

            teamRepositoryMock.Verify(repository => repository.GetById(stillWaiting), Times.Never);
        }

        /// <summary>
        /// Everything at Warning or worse lands in the Task Manager's Recent Problems. A fault that
        /// breaks every day would otherwise put a line there per day, on every chart load.
        /// </summary>
        [Test]
        public async Task DaysThatCannotBeWritten_AreReportedOnceWithTheCauseAndOnceAsATally()
        {
            GivenATeamLastObservedOn(Today);
            GivenTheTeamFinishedAnItemOn(Today.AddDays(-60));
            percentileWriterMock
                .Setup(writer => writer.SaveFilledDay())
                .ThrowsAsync(new InvalidOperationException(TheStoreRefusedTheDay));

            subject.AskFor(RequestFor(
                TeamId, Today.AddDays(-5), Today.AddDays(-4), Today.AddDays(-3), Today.AddDays(-2), Today.AddDays(-1)));
            await subject.DrainAsync(CancellationToken.None);

            var problems = logger.Everything.Where(entry => entry.Level >= LogLevel.Warning).ToList();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(problems, Has.Count.LessThanOrEqualTo(2),
                    "Five failed days were reported line by line instead of once with the cause and once as a count.");
                Assert.That(problems.Exists(entry => entry.Level == LogLevel.Error && entry.Failure is InvalidOperationException), Is.True,
                    "The first failure lost its exception, so nobody reading the log can tell what went wrong.");
                Assert.That(problems.Exists(entry => entry.Message.Contains("could not write 5 days", StringComparison.Ordinal)), Is.True,
                    "No line says how many days the pass could not write.");
            }
        }

        /// <summary>
        /// A restore is refused for as long as a pass runs, and the queue starts the next pass the moment
        /// one ends - so an operator turned away once could be turned away again and again for as long
        /// as owners are waiting. Once someone has been turned away, the pass under way stops at its
        /// next day, no further pass starts, and trying again gets in.
        /// </summary>
        [Test]
        public async Task ARestoreTurnedAwayByAPass_StopsThePassAtItsNextDayAndStartsNoOther_AndIsLetInOnItsNextTry()
        {
            const int stillWaiting = TeamId + 1;
            GivenATeamLastObservedOn(Today);
            GivenTheTeamFinishedAnItemOn(Today.AddDays(-60));
            var gate = services.GetRequiredService<DatabaseMaintenanceGate>();
            GateAcquisitionResult? turnedAway = null;
            percentileWriterMock
                .Setup(writer => writer.SaveFilledDay())
                .Callback(() => turnedAway ??= gate.TryAcquire(DatabaseOperationType.Restore, "restore-pressed-mid-pass"))
                .Returns(Task.CompletedTask);

            subject.AskFor(RequestFor(TeamId, Today.AddDays(-2), Today.AddDays(-1)));
            subject.AskFor(RequestFor(stillWaiting, Today));
            await subject.DrainAsync(CancellationToken.None);
            var triedAgain = gate.TryAcquire(DatabaseOperationType.Restore, "restore-pressed-again");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(turnedAway?.Acquired, Is.False,
                    "The restore was let in while a pass was writing, so nothing here was ever waiting.");
                percentileWriterMock.Verify(writer => writer.SaveFilledDay(), Times.Once,
                    "The pass carried on to its next day while the operator was waiting.");
                teamRepositoryMock.Verify(repository => repository.GetById(stillWaiting), Times.Never,
                    "The next waiting pass started, and loaded its owner, while the operator was waiting.");
                Assert.That(triedAgain.Acquired, Is.True,
                    $"Trying again after the pass stood down was refused: {triedAgain.BlockedReason}");
            }
        }

        [Test]
        public async Task TwoAsksForTheSameOwnerBeforeItsPass_RunOnePass()
        {
            subject.AskFor(RequestFor(TeamId, Today));
            subject.AskFor(RequestFor(TeamId, Today.AddDays(-1)));
            await subject.DrainAsync(CancellationToken.None);

            teamRepositoryMock.Verify(repository => repository.GetById(TeamId), Times.Once);
        }

        [Test]
        public async Task AnOwnerAskedForAgainOnceItsPassIsOver_IsTakenAgain()
        {
            subject.AskFor(RequestFor(TeamId, Today));
            await subject.DrainAsync(CancellationToken.None);

            subject.AskFor(RequestFor(TeamId, Today));
            await subject.DrainAsync(CancellationToken.None);

            teamRepositoryMock.Verify(repository => repository.GetById(TeamId), Times.Exactly(2));
        }

        [Test]
        public async Task AWaitingPass_WhileFillingInPastDaysIsSwitchedOff_LoadsNoOwner()
        {
            fillSwitchMock.Setup(fillSwitch => fillSwitch.IsSwitchedOn()).Returns(false);
            GivenATeamLastObservedOn(Today);

            subject.AskFor(RequestFor(TeamId, Today));
            await subject.DrainAsync(CancellationToken.None);

            teamRepositoryMock.Verify(repository => repository.GetById(TeamId), Times.Never);
        }

        [Test]
        public async Task APassThatFails_IsLoggedWithItsCause_AndTheNextOwnerWaitingIsStillTaken()
        {
            const int stillWaiting = TeamId + 1;
            teamRepositoryMock
                .Setup(repository => repository.GetById(TeamId))
                .Throws(new InvalidOperationException("the owner could not be loaded"));

            subject.AskFor(RequestFor(TeamId, Today));
            subject.AskFor(RequestFor(stillWaiting, Today));
            await subject.DrainAsync(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(logger.Everything.Any(entry => entry.Level == LogLevel.Error && entry.Failure is InvalidOperationException), Is.True,
                    "The failed pass left nothing in the log to say why.");
                teamRepositoryMock.Verify(repository => repository.GetById(stillWaiting), Times.Once,
                    "One failed pass stopped the owners queued behind it from being filled.");
            }
        }

        [Test]
        public async Task APass_WhileABackupRuns_LoadsNoOwner()
        {
            GivenATeamLastObservedOn(Today);
            var backup = services.GetRequiredService<DatabaseMaintenanceGate>().TryAcquire(DatabaseOperationType.Backup, "backup-under-way");

            subject.AskFor(RequestFor(TeamId, Today));
            await subject.DrainAsync(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(backup.Acquired, Is.True);
                teamRepositoryMock.Verify(repository => repository.GetById(TeamId), Times.Never);
            }
        }

        /// <summary>
        /// Every day before the first finished item is a day no pass can ever write, so once a pass
        /// has found out where that is, a chart load stops asking for those days.
        /// </summary>
        [Test]
        public async Task AfterAPass_TheDaysBeforeTheOwnersFirstFinishedItemAreKnownToBeUnwritable()
        {
            GivenATeamLastObservedOn(Today);
            GivenTheTeamFinishedAnItemOn(Today.AddDays(-60));

            subject.AskFor(RequestFor(TeamId, Today.AddDays(-1)));
            await subject.DrainAsync(CancellationToken.None);

            Assert.That(Memo().NoPassCanWrite(TeamId, OwnerType.Team, Today.AddDays(-61)), Is.True);
        }

        [Test]
        public async Task TheFirstAndTheLastDayTheStoredItemsSupport_AreBothWorkedOut_AndTheDaysEitherSideAreNot()
        {
            var firstFinished = Today.AddDays(-30);
            var lastObserved = Today.AddDays(-5);
            GivenATeamLastObservedOn(lastObserved);
            GivenTheTeamFinishedAnItemOn(firstFinished);

            subject.AskFor(RequestFor(TeamId, firstFinished.AddDays(-1), firstFinished, lastObserved, lastObserved.AddDays(1)));
            await subject.DrainAsync(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                VerifyCycleTimeWorkedOutOn(firstFinished, Times.Once());
                VerifyCycleTimeWorkedOutOn(lastObserved, Times.Once());
                VerifyCycleTimeWorkedOutOn(firstFinished.AddDays(-1), Times.Never());
                VerifyCycleTimeWorkedOutOn(lastObserved.AddDays(1), Times.Never());
            }
        }

        [Test]
        public async Task ADayWorkedOut_IsWorkedOutForTheOwnersLimitsToo()
        {
            var family = new ProcessBehaviorFamilyReader(ProcessBehaviorMetricType.Throughput, 30, (_, _, _) => new ProcessBehaviourChart());
            var team = GivenATeamLastObservedOn(Today);
            GivenTheTeamFinishedAnItemOn(Today.AddDays(-60));
            limitWriterMock.Setup(writer => writer.FamiliesFor(team)).Returns([family]);

            subject.AskFor(RequestFor(TeamId, Today.AddDays(-1)));
            await subject.DrainAsync(CancellationToken.None);

            limitWriterMock.Verify(writer => writer.FillDayIfAbsent(
                TeamId, OwnerType.Team, It.Is<ProcessBehaviorFamilyReader>(f => f.MetricType == ProcessBehaviorMetricType.Throughput), Today.AddDays(-1)),
                Times.Once);
        }

        [Test]
        public async Task ADayWrittenInFull_IsNotAskedForAgain()
        {
            GivenATeamLastObservedOn(Today);
            GivenTheTeamFinishedAnItemOn(Today.AddDays(-60));

            subject.AskFor(RequestFor(TeamId, Today.AddDays(-1)));
            await subject.DrainAsync(CancellationToken.None);

            Assert.That(Memo().NoPassCanWrite(TeamId, OwnerType.Team, Today.AddDays(-1)), Is.True);
        }

        /// <summary>
        /// A day is one day across both kinds of chart: the percentiles getting through is not enough
        /// for it to count as settled when its limits did not.
        /// </summary>
        [Test]
        public async Task ADayWhoseLimitsCouldNotBeWritten_IsReportedWithTheCause_AndLeftToBeTriedAgain()
        {
            GivenATeamLastObservedOn(Today);
            GivenTheTeamFinishedAnItemOn(Today.AddDays(-60));
            limitWriterMock
                .Setup(writer => writer.SaveFilledDay())
                .ThrowsAsync(new InvalidOperationException("the store refused the limits"));

            subject.AskFor(RequestFor(TeamId, Today.AddDays(-1)));
            await subject.DrainAsync(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(logger.Everything.Any(entry => entry.Level == LogLevel.Error && entry.Failure is InvalidOperationException), Is.True,
                    "The day whose limits failed was not reported.");
                Assert.That(Memo().NoPassCanWrite(TeamId, OwnerType.Team, Today.AddDays(-1)), Is.False,
                    "A day lost to a fault was remembered as settled and will never be tried again.");
            }
        }

        /// <summary>
        /// A day whose two halves both fail is still one failed day, reported once, and the cause it
        /// carries is the half written first.
        /// </summary>
        [Test]
        public async Task ADayWhoseTwoHalvesBothFail_IsReportedWithThePercentilesFailure()
        {
            GivenATeamLastObservedOn(Today);
            GivenTheTeamFinishedAnItemOn(Today.AddDays(-60));
            var percentilesFailure = new InvalidOperationException(TheStoreRefusedTheDay);
            percentileWriterMock.Setup(writer => writer.SaveFilledDay()).ThrowsAsync(percentilesFailure);
            limitWriterMock.Setup(writer => writer.SaveFilledDay()).ThrowsAsync(new InvalidOperationException("the store refused the limits"));

            subject.AskFor(RequestFor(TeamId, Today.AddDays(-1)));
            await subject.DrainAsync(CancellationToken.None);

            Assert.That(logger.Everything.Where(entry => entry.Level == LogLevel.Error).Select(entry => entry.Failure),
                Is.EqualTo(new List<Exception?> { percentilesFailure }));
        }

        /// <summary>
        /// An owner deleted between the ask and its pass is simply gone: there is nothing to fill and
        /// nothing an operator needs to hear about.
        /// </summary>
        [Test]
        public async Task AnOwnerThatNoLongerExists_IsLetGoWithoutAProblemBeingReported()
        {
            subject.AskFor(RequestFor(TeamId, Today));
            await subject.DrainAsync(CancellationToken.None);

            Assert.That(logger.Everything.Where(entry => entry.Level >= LogLevel.Warning), Is.Empty);
        }

        [Test]
        public async Task OneDayThatCannotBeWritten_IsReportedOnce_WithItsCause_AndNoTally()
        {
            GivenATeamLastObservedOn(Today);
            GivenTheTeamFinishedAnItemOn(Today.AddDays(-60));
            percentileWriterMock
                .Setup(writer => writer.SaveFilledDay())
                .ThrowsAsync(new InvalidOperationException(TheStoreRefusedTheDay));

            subject.AskFor(RequestFor(TeamId, Today.AddDays(-1)));
            await subject.DrainAsync(CancellationToken.None);

            Assert.That(logger.Everything.Where(entry => entry.Level >= LogLevel.Warning).Select(entry => entry.Level),
                Is.EqualTo(OnlyAnError));
        }

        [Test]
        public async Task TwoDaysThatCannotBeWritten_AreTalliedOnceTheCauseHasBeenReported()
        {
            GivenATeamLastObservedOn(Today);
            GivenTheTeamFinishedAnItemOn(Today.AddDays(-60));
            percentileWriterMock
                .Setup(writer => writer.SaveFilledDay())
                .ThrowsAsync(new InvalidOperationException(TheStoreRefusedTheDay));

            subject.AskFor(RequestFor(TeamId, Today.AddDays(-2), Today.AddDays(-1)));
            await subject.DrainAsync(CancellationToken.None);

            Assert.That(logger.Warnings.Any(message => message.Contains("could not write 2 days", StringComparison.Ordinal)), Is.True);
        }

        /// <summary>
        /// A pass out of time gives the rest of its window back rather than hurrying it, but never
        /// before it has tried a day: a pass that stopped at nothing would never fill anything.
        /// </summary>
        [Test]
        public async Task APassOutOfTime_StillWorksOutItsFirstDay_AndGivesTheRestBack()
        {
            subject.Dispose();
            subject = new OverTimeHistoryFiller(services.GetRequiredService<IServiceScopeFactory>(), logger, TimeSpan.Zero);
            GivenATeamLastObservedOn(Today);
            GivenTheTeamFinishedAnItemOn(Today.AddDays(-60));

            subject.AskFor(RequestFor(TeamId, Today.AddDays(-3), Today.AddDays(-2), Today.AddDays(-1)));
            await subject.DrainAsync(CancellationToken.None);

            percentileWriterMock.Verify(writer => writer.SaveFilledDay(), Times.Once);
        }

        [Test]
        public async Task APassWorksOutNinetyDaysAtMost()
        {
            const int mostDaysOnePassWorksOut = 90;
            GivenATeamLastObservedOn(Today);
            GivenTheTeamFinishedAnItemOn(Today.AddDays(-200));
            var hundredDays = Enumerable.Range(1, 100).Select(daysAgo => Today.AddDays(-daysAgo)).ToArray();

            subject.AskFor(RequestFor(TeamId, hundredDays));
            await subject.DrainAsync(CancellationToken.None);

            percentileWriterMock.Verify(writer => writer.SaveFilledDay(), Times.Exactly(mostDaysOnePassWorksOut));
        }

        [Test]
        public async Task StoppingTheFiller_WorksThroughTheOwnersStillWaiting()
        {
            subject.AskFor(RequestFor(TeamId, Today));

            await subject.StopAsync(CancellationToken.None);

            teamRepositoryMock.Verify(repository => repository.GetById(TeamId), Times.Once);
        }

        [Test]
        public async Task ARunningFiller_TakesAnAskOnItsOwn_AndStopsItsReaderWhenStopped()
        {
            var taken = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            teamRepositoryMock
                .Setup(repository => repository.GetById(TeamId))
                .Callback(() => taken.TrySetResult())
                .Returns((Team?)null);

            await subject.StartAsync(CancellationToken.None);
            subject.AskFor(RequestFor(TeamId, Today));
            var tookIt = await Task.WhenAny(taken.Task, Task.Delay(TimeSpan.FromSeconds(10))) == taken.Task;
            await subject.StopAsync(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(tookIt, Is.True, "The running filler never took the ask off the queue.");
                Assert.That(subject.ExecuteTask?.IsCompleted, Is.True, "Stopping the filler left its reader running.");
            }
        }

        private ReconstructionMemo Memo() => services.GetRequiredService<ReconstructionMemo>();

        private void VerifyCycleTimeWorkedOutOn(DateOnly day, Times times)
            => percentileWriterMock.Verify(writer => writer.FillDayIfAbsent(
                TeamId, OwnerType.Team, MetricType.CycleTime, day, It.IsAny<Func<DateTime, DateTime, IEnumerable<PercentileValue>>>()), times);

        private Team GivenATeamLastObservedOn(DateOnly day)
        {
            var team = new Team { Id = TeamId, UpdateTime = day.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc) };
            teamRepositoryMock.Setup(repository => repository.GetById(TeamId)).Returns(team);

            return team;
        }

        private void GivenTheTeamFinishedAnItemOn(DateOnly day)
            => storedItems.Add(new WorkItem { TeamId = TeamId, ClosedDate = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) });

        private static OverTimeFillRequest RequestFor(int teamId, params DateOnly[] days)
            => new(teamId, OwnerType.Team, days);
    }
}
