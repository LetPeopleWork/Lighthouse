using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.DomainEvents;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Services.Implementation.DomainEvents
{
    [TestFixture]
    [Category("epic-5074-blocked-items")]
    public class DemoBlockedHistoryBackfillHandlerTests
    {
        private DbContextOptions<LighthouseAppContext> options = null!;
        private Mock<ICryptoService> cryptoServiceMock = null!;
        private Mock<ILogger<LighthouseAppContext>> appContextLoggerMock = null!;

        private Mock<ITeamMetricsService> teamMetricsServiceMock = null!;
        private Mock<IPortfolioMetricsService> portfolioMetricsServiceMock = null!;
        private Mock<Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<Team>> teamRepositoryMock = null!;
        private Mock<Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<Portfolio>> portfolioRepositoryMock = null!;
        private Mock<Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<WorkTrackingSystemConnection>> connectionRepositoryMock = null!;
        private Mock<IBlockedItemService> blockedItemServiceMock = null!;

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
            teamRepositoryMock = new Mock<Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<Team>>();
            portfolioRepositoryMock = new Mock<Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<Portfolio>>();
            connectionRepositoryMock = new Mock<Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<WorkTrackingSystemConnection>>();
            blockedItemServiceMock = new Mock<IBlockedItemService>();

            blockedItemServiceMock
                .Setup(x => x.IsBlocked(It.IsAny<WorkItem>(), It.IsAny<Team>()))
                .Returns(true);
        }

        private LighthouseAppContext CreateContext()
        {
            return new LighthouseAppContext(options, cryptoServiceMock.Object, appContextLoggerMock.Object);
        }

        private DemoBlockedHistoryBackfillHandler CreateSubject(LighthouseAppContext context)
        {
            var snapshotRepo = new BlockedCountSnapshotRepository(context, Mock.Of<ILogger<BlockedCountSnapshotRepository>>());
            var transitionRepo = new WorkItemBlockedTransitionRepository(context, Mock.Of<ILogger<WorkItemBlockedTransitionRepository>>());

            return new DemoBlockedHistoryBackfillHandler(
                teamMetricsServiceMock.Object,
                portfolioMetricsServiceMock.Object,
                teamRepositoryMock.Object,
                portfolioRepositoryMock.Object,
                connectionRepositoryMock.Object,
                blockedItemServiceMock.Object,
                transitionRepo,
                snapshotRepo,
                Mock.Of<ILogger<DemoBlockedHistoryBackfillHandler>>());
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

        private Team ArrangeTeam(int teamId, int connectionId, IReadOnlyList<WorkItem> blockedItems)
        {
            var team = new Team { Id = teamId, Name = $"Team {teamId}", WorkTrackingSystemConnectionId = connectionId };
            teamRepositoryMock.Setup(x => x.GetById(teamId)).Returns(team);
            teamMetricsServiceMock.Setup(x => x.GetBlockedEligibleItemsForTeam(team)).Returns(blockedItems);
            return team;
        }

        private static WorkItem CreateBlockedItem(int id, DateTime? startedDate)
        {
            var item = new WorkItem(new WorkItemBase
            {
                ReferenceId = $"WI-{id}",
                Name = $"Item {id}",
                State = "Active",
                StateCategory = StateCategories.Doing,
                Type = "Story",
                Url = $"https://letpeople.work/{id}",
                StartedDate = startedDate,
            }, new Team { Id = 1, Name = "T" });
            item.Id = id;
            return item;
        }

        [Test]
        public async Task HandleTeamRefreshed_DemoOwnerWithBlockedItems_WritesBackdatedTransitionsAndRisingSnapshotHistory()
        {
            const int teamId = 7;
            const int connectionId = 1886;
            ArrangeConnection(connectionId, isDemo: true);

            var blockedItems = new List<WorkItem>
            {
                CreateBlockedItem(101, DateTime.Today.AddDays(-40)),
                CreateBlockedItem(102, DateTime.Today.AddDays(-40)),
                CreateBlockedItem(103, DateTime.Today.AddDays(-40)),
            };
            ArrangeTeam(teamId, connectionId, blockedItems);

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(teamId), CancellationToken.None);

            var transitions = context.WorkItemBlockedTransitions.ToList();
            var snapshots = context.BlockedCountSnapshots
                .Where(s => s.OwnerId == teamId && s.OwnerType == OwnerType.Team)
                .OrderBy(s => s.RecordedAt)
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(transitions, Has.Count.EqualTo(3), "one backdated transition per blocked item");
                Assert.That(transitions.All(t => t.LeftAt == null), Is.True, "demo blocked items are still blocked (open interval)");
                Assert.That(transitions.Select(t => t.EnteredAt.Date).Distinct().Count(), Is.GreaterThan(1), "entered dates are spread, not identical");

                Assert.That(snapshots, Is.Not.Empty);
                Assert.That(snapshots[0].BlockedCount, Is.LessThan(snapshots[^1].BlockedCount), "blocked count rises across the window");
                Assert.That(snapshots[^1].BlockedCount, Is.EqualTo(3), "all three are blocked by today");
                Assert.That(snapshots[^1].RecordedAt, Is.EqualTo(DateOnly.FromDateTime(DateTime.Today)));
            }
        }

        [Test]
        public async Task HandleTeamRefreshed_NonDemoOwner_DoesNothing()
        {
            const int teamId = 7;
            const int connectionId = 55;
            ArrangeConnection(connectionId, isDemo: false);
            ArrangeTeam(teamId, connectionId, new List<WorkItem> { CreateBlockedItem(101, DateTime.Today.AddDays(-10)) });

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(teamId), CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(context.WorkItemBlockedTransitions.ToList(), Is.Empty);
                Assert.That(context.BlockedCountSnapshots.ToList(), Is.Empty);
            }
        }

        [Test]
        public async Task HandleTeamRefreshed_AlreadyBackfilled_IsIdempotent()
        {
            const int teamId = 7;
            const int connectionId = 1886;
            ArrangeConnection(connectionId, isDemo: true);
            ArrangeTeam(teamId, connectionId, new List<WorkItem> { CreateBlockedItem(101, DateTime.Today.AddDays(-20)) });

            using var seedContext = CreateContext();
            // Several backdated rows — the idempotency guard must treat "any history exists" as
            // already-backfilled without tripping over more than one match.
            foreach (var daysAgo in new[] { 2, 3, 4 })
            {
                seedContext.BlockedCountSnapshots.Add(new BlockedCountSnapshot
                {
                    OwnerId = teamId,
                    OwnerType = OwnerType.Team,
                    RecordedAt = DateOnly.FromDateTime(DateTime.Today.AddDays(-daysAgo)),
                    BlockedCount = 1,
                });
            }
            await seedContext.SaveChangesAsync();

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(teamId), CancellationToken.None);

            Assert.That(context.WorkItemBlockedTransitions.ToList(), Is.Empty, "second run must not re-write history");
        }
    }
}
