using System.Linq.Expressions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.BackgroundServices;
using Lighthouse.Backend.Services.Implementation.DatabaseManagement;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.BackgroundServices;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
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
                .AddSingleton(new DatabaseMaintenanceGate(Mock.Of<IUpdateStatusStore>()))
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
