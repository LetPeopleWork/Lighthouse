using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkItems;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkItems
{
    public class WorkItemServiceTransitionFallbackIntegrationTest : IntegrationTestBase
    {
        [Test]
        public async Task UpdateWorkItemsForTeam_CsvConnectorWithoutHistory_StateChangesAcrossSyncs_DerivesCurrentStateEnteredAtFromSyncDelta()
        {
            var team = await GivenPersistedTeam(rawStates: ["New", "Active"], mappedToDo: "To Do", mappedDoing: "In Progress");

            var connector = ConnectorWithoutHistory();
            var subject = CreateSubject(team, connector);

            connector.Setup(x => x.GetWorkItemsForTeam(team))
                .ReturnsAsync([IncomingItem(team, referenceId: "CSV-1", rawState: "New", category: StateCategories.ToDo, syncedTransitions: [])]);
            await subject.UpdateWorkItemsForTeam(team);

            connector.Setup(x => x.GetWorkItemsForTeam(team))
                .ReturnsAsync([IncomingItem(team, referenceId: "CSV-1", rawState: "Active", category: StateCategories.Doing, syncedTransitions: [])]);
            await subject.UpdateWorkItemsForTeam(team);

            var persistedItem = LoadWorkItem("CSV-1");
            var persistedTransitions = LoadTransitions();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(persistedItem, Is.Not.Null);
                Assert.That(persistedItem!.State, Is.EqualTo("In Progress"));
                Assert.That(persistedItem.CurrentStateEnteredAt, Is.Not.Null,
                    "A CSV reload that detected a state change must synthesise a sync-delta transition and derive CurrentStateEnteredAt.");
                Assert.That(persistedTransitions, Has.Some.Matches<WorkItemStateTransition>(t => t.ToState == "In Progress"),
                    "A WorkItemStateTransition into the new mapped state must be captured on the change-detecting sync.");
            }
        }

        [Test]
        public async Task UpdateWorkItemsForTeam_CsvConnectorWithoutHistory_StateUnchangedAcrossSyncs_DoesNotSynthesiseTransition()
        {
            var team = await GivenPersistedTeam(rawStates: ["New", "Active"], mappedToDo: "To Do", mappedDoing: "In Progress");

            var connector = ConnectorWithoutHistory();
            var subject = CreateSubject(team, connector);

            connector.Setup(x => x.GetWorkItemsForTeam(team))
                .ReturnsAsync([IncomingItem(team, referenceId: "CSV-2", rawState: "Active", category: StateCategories.Doing, syncedTransitions: [])]);
            await subject.UpdateWorkItemsForTeam(team);
            await subject.UpdateWorkItemsForTeam(team);

            var persistedTransitions = LoadTransitions();

            Assert.That(persistedTransitions.Count(t => t.ToState == "In Progress"), Is.LessThanOrEqualTo(1),
                "Re-syncing an unchanged CSV item must not synthesise duplicate sync-delta transitions.");
        }

        [Test]
        public async Task UpdateWorkItemsForTeam_LinearConnectorWithHistory_PersistsRealTransitionsAndDerivesCurrentStateEnteredAt()
        {
            var team = await GivenPersistedTeam(rawStates: ["New", "Active"], mappedToDo: "To Do", mappedDoing: "In Progress");

            var enteredCurrentState = new DateTime(2026, 5, 20, 9, 0, 0, DateTimeKind.Utc);
            var rawTransitions = new List<WorkItemStateTransition>
            {
                new() { FromState = "New", ToState = "Active", TransitionedAt = enteredCurrentState },
            };

            var connector = ConnectorWithHistory();
            var subject = CreateSubject(team, connector);

            var incoming = IncomingItem(team, referenceId: "LIN-1", rawState: "Active", category: StateCategories.Doing,
                syncedTransitions: WorkItemStateTransitionMapper.MapToMappedStates(rawTransitions, team));
            connector.Setup(x => x.GetWorkItemsForTeam(team)).ReturnsAsync([incoming]);

            await subject.UpdateWorkItemsForTeam(team);

            var persistedItem = LoadWorkItem("LIN-1");
            var persistedTransitions = LoadTransitions();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(persistedItem, Is.Not.Null);
                Assert.That(persistedTransitions, Has.Some.Matches<WorkItemStateTransition>(t => t.ToState == "In Progress"));
                Assert.That(persistedItem!.CurrentStateEnteredAt, Is.EqualTo(enteredCurrentState),
                    "When the connector supplies real history, CurrentStateEnteredAt must derive from the real transition timestamp, not the sync time.");
            }
        }

        private Mock<IWorkTrackingConnector> ConnectorWithoutHistory()
        {
            var connector = new Mock<IWorkTrackingConnector>();
            connector.SetupGet(x => x.SupportsTransitionHistory).Returns(false);
            return connector;
        }

        private Mock<IWorkTrackingConnector> ConnectorWithHistory()
        {
            var connector = new Mock<IWorkTrackingConnector>();
            connector.SetupGet(x => x.SupportsTransitionHistory).Returns(true);
            return connector;
        }

        private WorkItem IncomingItem(Team team, string referenceId, string rawState, StateCategories category, IReadOnlyList<WorkItemStateTransition> syncedTransitions)
        {
            return new WorkItem(new WorkItemBase
            {
                ReferenceId = referenceId,
                Name = $"Story {referenceId}",
                Type = "Story",
                State = team.MapRawStateToMappedName(rawState),
                StateCategory = category,
                Order = referenceId,
                StartedDate = new DateTime(2026, 5, 19, 0, 0, 0, DateTimeKind.Utc),
                SyncedTransitions = syncedTransitions,
            }, team);
        }

        private async Task<Team> GivenPersistedTeam(string[] rawStates, string mappedToDo, string mappedDoing)
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = $"Connection {Guid.NewGuid():N}",
                WorkTrackingSystem = WorkTrackingSystems.Csv,
            };

            var team = new Team
            {
                Name = $"Team {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = connection,
                StateMappings =
                [
                    new StateMapping { Name = mappedToDo, States = [rawStates[0]] },
                    new StateMapping { Name = mappedDoing, States = [rawStates[1]] },
                ],
            };
            team.WorkItemTypes.Add("Story");

            var teamRepository = new TeamRepository(DatabaseContext, Mock.Of<ILogger<TeamRepository>>());
            teamRepository.Add(team);
            await teamRepository.Save();

            return team;
        }

        private WorkItem? LoadWorkItem(string referenceId)
        {
            var workItemRepository = new WorkItemRepository(DatabaseContext, Mock.Of<ILogger<WorkItemRepository>>());
            return workItemRepository.GetByPredicate(wi => wi.ReferenceId == referenceId);
        }

        private List<WorkItemStateTransition> LoadTransitions()
        {
            var transitionRepository = new WorkItemStateTransitionRepository(DatabaseContext, Mock.Of<ILogger<WorkItemStateTransitionRepository>>());
            return transitionRepository.GetAll().ToList();
        }

        private WorkItemService CreateSubject(Team team, Mock<IWorkTrackingConnector> connector)
        {
            var connectorFactoryMock = new Mock<IWorkTrackingConnectorFactory>();
            connectorFactoryMock.Setup(x => x.GetWorkTrackingConnector(It.IsAny<WorkTrackingSystems>())).Returns(connector.Object);

            var workItemRepository = new WorkItemRepository(DatabaseContext, Mock.Of<ILogger<WorkItemRepository>>());
            var transitionRepository = new WorkItemStateTransitionRepository(DatabaseContext, Mock.Of<ILogger<WorkItemStateTransitionRepository>>());

            return new WorkItemService(
                Mock.Of<ILogger<WorkItemService>>(),
                connectorFactoryMock.Object,
                Mock.Of<IRepository<Feature>>(),
                workItemRepository,
                Mock.Of<IPortfolioMetricsService>(),
                Mock.Of<IRepository<Team>>(),
                transitionRepository);
        }
    }
}
