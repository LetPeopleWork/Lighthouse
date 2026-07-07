using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.DomainEvents;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Services.Implementation.DomainEvents
{
    [TestFixture]
    [Category("epic-5074-blocked-items")]
    public class BlockedCountSnapshotRecordingHandlerTests
    {
        private DbContextOptions<LighthouseAppContext> options = null!;
        private Mock<ICryptoService> cryptoServiceMock = null!;
        private Mock<ILogger<LighthouseAppContext>> appContextLoggerMock = null!;

        private Mock<ITeamMetricsService> teamMetricsServiceMock = null!;
        private Mock<IPortfolioMetricsService> portfolioMetricsServiceMock = null!;
        private Mock<IRepository<Team>> teamRepositoryMock = null!;
        private Mock<IRepository<Portfolio>> portfolioRepositoryMock = null!;
        private Mock<IBlockedItemService> blockedItemServiceMock = null!;
        private Mock<ILogger<BlockedCountSnapshotRecordingHandler>> handlerLoggerMock = null!;

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
            blockedItemServiceMock = new Mock<IBlockedItemService>();
            handlerLoggerMock = new Mock<ILogger<BlockedCountSnapshotRecordingHandler>>();
        }

        private LighthouseAppContext CreateContext()
        {
            return new LighthouseAppContext(options, cryptoServiceMock.Object, appContextLoggerMock.Object);
        }

        private BlockedCountSnapshotRepository CreateSnapshotRepository(LighthouseAppContext context)
        {
            return new BlockedCountSnapshotRepository(context, Mock.Of<ILogger<BlockedCountSnapshotRepository>>());
        }

        private BlockedCountSnapshotRecordingHandler CreateSubject(LighthouseAppContext context)
        {
            var snapshotRepo = CreateSnapshotRepository(context);
            return new BlockedCountSnapshotRecordingHandler(
                teamMetricsServiceMock.Object,
                portfolioMetricsServiceMock.Object,
                teamRepositoryMock.Object,
                portfolioRepositoryMock.Object,
                blockedItemServiceMock.Object,
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

        private static WorkItem CreateWorkItem(int id, int teamId)
        {
            var team = CreateTeam(teamId);
            var item = new WorkItem(new WorkItemBase
            {
                ReferenceId = $"WI-{id}",
                Name = $"Work Item {id}",
                State = "Active",
                StateCategory = StateCategories.Doing,
                Type = "Story",
                Url = $"https://letpeople.work/{id}",
            }, team);
            item.Id = id;
            return item;
        }

        private static Feature CreateFeature(int id, List<Portfolio>? portfolios = null)
        {
            var feature = new Feature
            {
                Name = $"Feature {id}",
                ReferenceId = $"FEAT-{id}",
                Order = id.ToString(),
            };
            feature.Id = id;
            if (portfolios != null)
            {
                foreach (var portfolio in portfolios)
                {
                    feature.Portfolios.Add(portfolio);
                }
            }
            return feature;
        }

        private DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

        private async Task<BlockedCountSnapshot?> FindSnapshot(
            LighthouseAppContext context, int ownerId, OwnerType ownerType, DateOnly recordedAt)
        {
            return await context.BlockedCountSnapshots
                .SingleOrDefaultAsync(s =>
                    s.OwnerId == ownerId &&
                    s.OwnerType == ownerType &&
                    s.RecordedAt == recordedAt);
        }

        private async Task<int> SnapshotRowCountForOwner(
            LighthouseAppContext context, int ownerId, OwnerType ownerType, DateOnly recordedAt)
        {
            return await context.BlockedCountSnapshots
                .CountAsync(s =>
                    s.OwnerId == ownerId &&
                    s.OwnerType == ownerType &&
                    s.RecordedAt == recordedAt);
        }

        // -----------------------------------------------------------------
        // FRESHNESS probe
        // -----------------------------------------------------------------
        [Test]
        public async Task Freshness_RecordedCountEqualsLiveCountOfIsBlocked_AtRecordTime()
        {
            // Arrange
            var team = CreateTeam(1);
            var blockedItem = CreateWorkItem(10, team.Id);
            var unblockedItem = CreateWorkItem(20, team.Id);
            var workItems = new List<WorkItem> { blockedItem, unblockedItem };

            teamRepositoryMock.Setup(x => x.GetById(team.Id)).Returns(team);
            teamMetricsServiceMock
                .Setup(x => x.GetWipSnapshotForTeam(It.IsAny<Team>(), It.IsAny<DateTime>()))
                .Returns(workItems);
            blockedItemServiceMock
                .Setup(x => x.IsBlocked(It.IsAny<WorkItem>(), It.IsAny<Team>()))
                .Returns((WorkItem item, Team _) => item.Id == blockedItem.Id);

            using var context = CreateContext();
            var subject = CreateSubject(context);

            // Act
            await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None);

            // Assert
            var snapshot = await FindSnapshot(context, team.Id, OwnerType.Team, Today);
            Assert.That(snapshot, Is.Not.Null);
            Assert.That(snapshot!.BlockedCount, Is.EqualTo(1),
                "BlockedCount must match the live count from IBlockedItemService");
        }

        // -----------------------------------------------------------------
        // IDEMPOTENCY probe
        // -----------------------------------------------------------------
        [Test]
        public async Task Idempotency_SameDayReRunProducesExactlyOneRow()
        {
            // Arrange
            var team = CreateTeam(1);
            var workItem = CreateWorkItem(10, team.Id);
            var workItems = new List<WorkItem> { workItem };

            teamRepositoryMock.Setup(x => x.GetById(team.Id)).Returns(team);
            teamMetricsServiceMock
                .Setup(x => x.GetWipSnapshotForTeam(It.IsAny<Team>(), It.IsAny<DateTime>()))
                .Returns(workItems);
            blockedItemServiceMock
                .Setup(x => x.IsBlocked(It.IsAny<WorkItem>(), team))
                .Returns(true);

            using var context = CreateContext();
            var subject = CreateSubject(context);

            // Act — run twice on the same day
            await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None);
            await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None);

            // Assert
            var rowCount = await SnapshotRowCountForOwner(context, team.Id, OwnerType.Team, Today);
            Assert.That(rowCount, Is.EqualTo(1),
                "same-day re-run must produce exactly one row for the owner and date");
        }

        // -----------------------------------------------------------------
        // SINGLE-DEFINITION probe
        // -----------------------------------------------------------------
        [Test]
        public async Task SingleDefinition_CountDerivesOnlyFromIBlockedItemService()
        {
            // Arrange
            var team = CreateTeam(1);
            var itemA = CreateWorkItem(10, team.Id);
            var itemB = CreateWorkItem(20, team.Id);
            var itemC = CreateWorkItem(30, team.Id);
            var workItems = new List<WorkItem> { itemA, itemB, itemC };

            teamRepositoryMock.Setup(x => x.GetById(team.Id)).Returns(team);
            teamMetricsServiceMock
                .Setup(x => x.GetWipSnapshotForTeam(It.IsAny<Team>(), It.IsAny<DateTime>()))
                .Returns(workItems);

            // Only itemA and itemC are blocked
            blockedItemServiceMock
                .Setup(x => x.IsBlocked(itemA, team))
                .Returns(true);
            blockedItemServiceMock
                .Setup(x => x.IsBlocked(itemB, team))
                .Returns(false);
            blockedItemServiceMock
                .Setup(x => x.IsBlocked(itemC, team))
                .Returns(true);

            using var context = CreateContext();
            var subject = CreateSubject(context);

            // Act
            await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None);

            // Assert
            var snapshot = await FindSnapshot(context, team.Id, OwnerType.Team, Today);
            Assert.That(snapshot, Is.Not.Null);
            Assert.That(snapshot!.BlockedCount, Is.EqualTo(2),
                "the count must derive exclusively from IBlockedItemService.IsBlocked — " +
                "not from WorkItem.States, not from WorkItem.Tags, not from any other path");
        }

        // -----------------------------------------------------------------
        // TeamDataRefreshed -> team snapshot
        // -----------------------------------------------------------------
        [Test]
        public async Task TeamDataRefreshed_RecordsTeamSnapshot()
        {
            // Arrange
            var team = CreateTeam(42);
            var workItem = CreateWorkItem(10, team.Id);
            var workItems = new List<WorkItem> { workItem };

            teamRepositoryMock.Setup(x => x.GetById(team.Id)).Returns(team);
            teamMetricsServiceMock
                .Setup(x => x.GetWipSnapshotForTeam(It.IsAny<Team>(), It.IsAny<DateTime>()))
                .Returns(workItems);
            blockedItemServiceMock
                .Setup(x => x.IsBlocked(It.IsAny<WorkItem>(), team))
                .Returns(true);

            using var context = CreateContext();
            var subject = CreateSubject(context);

            // Act
            await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None);

            // Assert
            var snapshot = await FindSnapshot(context, team.Id, OwnerType.Team, Today);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(snapshot, Is.Not.Null);
                Assert.That(snapshot!.OwnerId, Is.EqualTo(team.Id));
                Assert.That(snapshot.OwnerType, Is.EqualTo(OwnerType.Team));
                Assert.That(snapshot.RecordedAt, Is.EqualTo(Today));
                Assert.That(snapshot.BlockedCount, Is.EqualTo(1));
            }
        }

        // -----------------------------------------------------------------
        // PortfolioFeaturesRefreshed -> portfolio snapshot
        // -----------------------------------------------------------------
        [Test]
        public async Task PortfolioFeaturesRefreshed_RecordsPortfolioSnapshot()
        {
            // Arrange
            var portfolio = CreatePortfolio(7);
            var blockedFeature = CreateFeature(100, portfolios: [portfolio]);
            var unblockedFeature = CreateFeature(200, portfolios: [portfolio]);
            var features = new List<Feature> { blockedFeature, unblockedFeature };

            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);
            portfolioMetricsServiceMock
                .Setup(x => x.GetInProgressFeaturesForPortfolio(It.IsAny<Portfolio>(), It.IsAny<DateTime>()))
                .Returns(features);
            blockedItemServiceMock
                .Setup(x => x.IsBlocked(It.IsAny<Feature>(), portfolio))
                .Returns((Feature item, Portfolio _) => item.Id == blockedFeature.Id);

            using var context = CreateContext();
            var subject = CreateSubject(context);

            // Act
            await subject.HandleAsync(new PortfolioFeaturesRefreshed(portfolio.Id), CancellationToken.None);

            // Assert
            var snapshot = await FindSnapshot(context, portfolio.Id, OwnerType.Portfolio, Today);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(snapshot, Is.Not.Null);
                Assert.That(snapshot!.OwnerId, Is.EqualTo(portfolio.Id));
                Assert.That(snapshot.OwnerType, Is.EqualTo(OwnerType.Portfolio));
                Assert.That(snapshot.RecordedAt, Is.EqualTo(Today));
                Assert.That(snapshot.BlockedCount, Is.EqualTo(1));
            }
        }

        [Test]
        public async Task TeamDataRefreshed_NullTeam_ReturnsWithoutRecording()
        {
            teamRepositoryMock.Setup(x => x.GetById(999)).Returns((Team?)null);

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(999), CancellationToken.None);

            var count = await context.BlockedCountSnapshots.CountAsync();
            Assert.That(count, Is.Zero, "no snapshot should be recorded when team is null");
        }

        [Test]
        public async Task PortfolioFeaturesRefreshed_NullPortfolio_ReturnsWithoutRecording()
        {
            portfolioRepositoryMock.Setup(x => x.GetById(999)).Returns((Portfolio?)null);

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new PortfolioFeaturesRefreshed(999), CancellationToken.None);

            var count = await context.BlockedCountSnapshots.CountAsync();
            Assert.That(count, Is.Zero, "no snapshot should be recorded when portfolio is null");
        }

        [Test]
        public async Task TeamDataRefreshed_ZeroBlockedItems_RecordsZeroCount()
        {
            var team = CreateTeam(1);
            var workItems = new List<WorkItem>
            {
                CreateWorkItem(10, team.Id),
                CreateWorkItem(20, team.Id),
            };

            teamRepositoryMock.Setup(x => x.GetById(team.Id)).Returns(team);
            teamMetricsServiceMock
                .Setup(x => x.GetWipSnapshotForTeam(It.IsAny<Team>(), It.IsAny<DateTime>()))
                .Returns(workItems);
            blockedItemServiceMock
                .Setup(x => x.IsBlocked(It.IsAny<WorkItem>(), team))
                .Returns(false);

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None);

            var snapshot = await FindSnapshot(context, team.Id, OwnerType.Team, Today);
            Assert.That(snapshot, Is.Not.Null, "a zero-count snapshot must still be recorded");
            Assert.That(snapshot!.BlockedCount, Is.Zero);
        }

        [Test]
        public async Task TeamDataRefreshed_AllItemsBlocked_RecordsFullSetCount()
        {
            var team = CreateTeam(1);
            var workItems = Enumerable.Range(1, 10).Select(i => CreateWorkItem(i, team.Id)).ToList();

            teamRepositoryMock.Setup(x => x.GetById(team.Id)).Returns(team);
            teamMetricsServiceMock
                .Setup(x => x.GetWipSnapshotForTeam(It.IsAny<Team>(), It.IsAny<DateTime>()))
                .Returns(workItems);
            blockedItemServiceMock
                .Setup(x => x.IsBlocked(It.IsAny<WorkItem>(), team))
                .Returns(true);

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None);

            var snapshot = await FindSnapshot(context, team.Id, OwnerType.Team, Today);
            Assert.That(snapshot, Is.Not.Null);
            Assert.That(snapshot!.BlockedCount, Is.EqualTo(10));
        }

        [Test]
        public async Task TeamDataRefreshed_NoWorkItems_RecordsZeroCount()
        {
            var team = CreateTeam(1);
            var workItems = new List<WorkItem>();

            teamRepositoryMock.Setup(x => x.GetById(team.Id)).Returns(team);
            teamMetricsServiceMock
                .Setup(x => x.GetWipSnapshotForTeam(It.IsAny<Team>(), It.IsAny<DateTime>()))
                .Returns(workItems);

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None);

            var snapshot = await FindSnapshot(context, team.Id, OwnerType.Team, Today);
            Assert.That(snapshot, Is.Not.Null, "a zero-count snapshot must be recorded for empty team");
            Assert.That(snapshot!.BlockedCount, Is.Zero);
        }

        [Test]
        public async Task PortfolioFeaturesRefreshed_NoFeatures_RecordsZeroCount()
        {
            var portfolio = CreatePortfolio(7);
            var features = new List<Feature>();

            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);
            portfolioMetricsServiceMock
                .Setup(x => x.GetInProgressFeaturesForPortfolio(It.IsAny<Portfolio>(), It.IsAny<DateTime>()))
                .Returns(features);

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new PortfolioFeaturesRefreshed(portfolio.Id), CancellationToken.None);

            var snapshot = await FindSnapshot(context, portfolio.Id, OwnerType.Portfolio, Today);
            Assert.That(snapshot, Is.Not.Null, "a zero-count snapshot must be recorded for empty portfolio");
            Assert.That(snapshot!.BlockedCount, Is.Zero);
        }

        [Test]
        public async Task TeamDataRefreshed_OnlyMatchesCorrectOwnerInPredicate()
        {
            var team1 = CreateTeam(1);
            var team2 = CreateTeam(2);
            var workItem = CreateWorkItem(10, team1.Id);

            teamRepositoryMock.Setup(x => x.GetById(team1.Id)).Returns(team1);
            teamMetricsServiceMock
                .Setup(x => x.GetWipSnapshotForTeam(It.IsAny<Team>(), It.IsAny<DateTime>()))
                .Returns(new List<WorkItem> { workItem });
            blockedItemServiceMock
                .Setup(x => x.IsBlocked(It.IsAny<WorkItem>(), team1))
                .Returns(true);

            using var context = CreateContext();
            var otherDay = Today.AddDays(-1);
            context.BlockedCountSnapshots.Add(new BlockedCountSnapshot
            {
                OwnerId = team1.Id, OwnerType = OwnerType.Team,
                RecordedAt = otherDay, BlockedCount = 99,
            });
            context.BlockedCountSnapshots.Add(new BlockedCountSnapshot
            {
                OwnerId = team2.Id, OwnerType = OwnerType.Team,
                RecordedAt = Today, BlockedCount = 99,
            });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(team1.Id), CancellationToken.None);

            var rowCount = await SnapshotRowCountForOwner(context, team2.Id, OwnerType.Team, Today);
            Assert.That(rowCount, Is.EqualTo(1),
                "team2's snapshot must not be overwritten");
            var team2Snapshot = await FindSnapshot(context, team2.Id, OwnerType.Team, Today);
            Assert.That(team2Snapshot!.BlockedCount, Is.EqualTo(99),
                "non-matching owner's count must remain unchanged");
        }

        [Test]
        public async Task TeamDataRefreshed_ChangedBlockedCount_UpdatesExistingSnapshot()
        {
            var team = CreateTeam(1);
            var workItem1 = CreateWorkItem(10, team.Id);
            var workItem2 = CreateWorkItem(20, team.Id);
            var workItem3 = CreateWorkItem(30, team.Id);

            teamRepositoryMock.Setup(x => x.GetById(team.Id)).Returns(team);
            teamMetricsServiceMock
                .Setup(x => x.GetWipSnapshotForTeam(It.IsAny<Team>(), It.IsAny<DateTime>()))
                .Returns(new List<WorkItem> { workItem1, workItem2, workItem3 });

            using var context = CreateContext();
            var subject = CreateSubject(context);

            // First run: only w1 is blocked
            blockedItemServiceMock
                .Setup(x => x.IsBlocked(It.IsAny<WorkItem>(), team))
                .Returns((WorkItem item, Team _) => item.Id == workItem1.Id);
            await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None);

            var first = await FindSnapshot(context, team.Id, OwnerType.Team, Today);
            Assert.That(first!.BlockedCount, Is.EqualTo(1));

            // Second run: w1 AND w2 are blocked — count changes to 2
            blockedItemServiceMock
                .Setup(x => x.IsBlocked(It.IsAny<WorkItem>(), team))
                .Returns((WorkItem item, Team _) => item.Id == workItem1.Id || item.Id == workItem2.Id);
            await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None);

            var updated = await FindSnapshot(context, team.Id, OwnerType.Team, Today);
            Assert.That(updated!.BlockedCount, Is.EqualTo(2),
                "same-day re-run with a changed blocked count must update the existing snapshot");
            var rowCount = await SnapshotRowCountForOwner(context, team.Id, OwnerType.Team, Today);
            Assert.That(rowCount, Is.EqualTo(1));
        }

        // -----------------------------------------------------------------
        // BUG 1 regression: the blocked-count trend must match the overview
        // widget, which counts blocked items over the WIP set only. A Done
        // item that still matches the blocked rule must NOT inflate the
        // snapshot, because it is excluded from GetWipSnapshotForTeam.
        // -----------------------------------------------------------------
        [Test]
        public async Task TeamDataRefreshed_CountsBlockedOverWipSetOnly_ExcludesDoneItems()
        {
            var team = CreateTeam(1);
            var inProgressBlocked = CreateWorkItem(10, team.Id);

            teamRepositoryMock.Setup(x => x.GetById(team.Id)).Returns(team);

            // The WIP snapshot already excludes Done items — a Done blocked item
            // never reaches the recorder, so the count derives from WIP alone.
            teamMetricsServiceMock
                .Setup(x => x.GetWipSnapshotForTeam(It.IsAny<Team>(), It.IsAny<DateTime>()))
                .Returns(new List<WorkItem> { inProgressBlocked });
            blockedItemServiceMock
                .Setup(x => x.IsBlocked(It.IsAny<WorkItem>(), team))
                .Returns(true);

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None);

            var snapshot = await FindSnapshot(context, team.Id, OwnerType.Team, Today);
            Assert.That(snapshot!.BlockedCount, Is.EqualTo(1),
                "the snapshot must count blocked items over the WIP set (matching the overview), not over all items");
            teamMetricsServiceMock.Verify(
                x => x.GetWipSnapshotForTeam(team, It.IsAny<DateTime>()), Times.Once,
                "the recorder must source its items from the WIP snapshot, not from the full item repository");
        }

        [Test]
        public async Task PortfolioFeaturesRefreshed_CountsBlockedOverInProgressFeaturesOnly()
        {
            var portfolio = CreatePortfolio(7);
            var inProgressBlocked = CreateFeature(100, portfolios: [portfolio]);

            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);

            portfolioMetricsServiceMock
                .Setup(x => x.GetInProgressFeaturesForPortfolio(It.IsAny<Portfolio>(), It.IsAny<DateTime>()))
                .Returns(new List<Feature> { inProgressBlocked });
            blockedItemServiceMock
                .Setup(x => x.IsBlocked(It.IsAny<Feature>(), portfolio))
                .Returns(true);

            using var context = CreateContext();
            var subject = CreateSubject(context);

            await subject.HandleAsync(new PortfolioFeaturesRefreshed(portfolio.Id), CancellationToken.None);

            var snapshot = await FindSnapshot(context, portfolio.Id, OwnerType.Portfolio, Today);
            Assert.That(snapshot!.BlockedCount, Is.EqualTo(1),
                "the snapshot must count blocked features over the in-progress set (matching the overview), not over all features");
            portfolioMetricsServiceMock.Verify(
                x => x.GetInProgressFeaturesForPortfolio(portfolio, It.IsAny<DateTime>()), Times.Once,
                "the recorder must source its features from the in-progress set, not from the full feature repository");
        }

        // -----------------------------------------------------------------
        // REGRESSION (verifypostgres flake): the snapshot Save MUST be awaited.
        // A fire-and-forget snapshotRepository.Save() returns before its
        // SaveChangesAsync finishes; the DomainEventDispatcher scope then disposes
        // the DbContext mid-write on the shared Npgsql connection, desyncing the
        // wire protocol ("BindComplete while expecting ReadyForQueryMessage").
        // The InMemory-based probes above never caught it because InMemory's
        // SaveChangesAsync completes synchronously. Gate Save on a TCS and assert
        // HandleAsync does not complete until the save resolves.
        // -----------------------------------------------------------------
        [Test]
        public async Task HandleAsync_DoesNotCompleteUntilTheSnapshotSaveCompletes()
        {
            var team = CreateTeam(1);
            teamRepositoryMock.Setup(x => x.GetById(team.Id)).Returns(team);
            teamMetricsServiceMock
                .Setup(x => x.GetWipSnapshotForTeam(It.IsAny<Team>(), It.IsAny<DateTime>()))
                .Returns(new List<WorkItem>());

            var saveGate = new TaskCompletionSource();
            var snapshotRepositoryMock = new Mock<IBlockedCountSnapshotRepository>();
            snapshotRepositoryMock
                .Setup(x => x.GetByPredicate(It.IsAny<Func<BlockedCountSnapshot, bool>>()))
                .Returns((BlockedCountSnapshot?)null);
            snapshotRepositoryMock.Setup(x => x.Save()).Returns(saveGate.Task);

            var subject = new BlockedCountSnapshotRecordingHandler(
                teamMetricsServiceMock.Object,
                portfolioMetricsServiceMock.Object,
                teamRepositoryMock.Object,
                portfolioRepositoryMock.Object,
                blockedItemServiceMock.Object,
                snapshotRepositoryMock.Object,
                handlerLoggerMock.Object);

            var handleTask = subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None);

            Assert.That(handleTask.IsCompleted, Is.False,
                "HandleAsync must await snapshotRepository.Save(); a fire-and-forget Save races DbContext "
                + "disposal on the shared connection and desyncs Npgsql (the verifypostgres flake).");

            saveGate.SetResult();
            await handleTask;
            snapshotRepositoryMock.Verify(x => x.Save(), Times.Once);
        }
    }
}
