using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkItemRules;
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
    public class WorkItemServiceTransitionSyncIntegrationTest : IntegrationTestBase
    {
        [Test]
        public async Task UpdateWorkItemsForTeam_NewItemWithRawTransitions_PersistsTransitionsWithRealWorkItemId_AndDerivesCurrentStateEnteredAt()
        {
            var team = await GivenPersistedTeam(rawState: "Active", mappedState: "In Progress");

            var enteredCurrentState = new DateTime(2026, 5, 25, 12, 0, 0, DateTimeKind.Utc);

            var rawTransitions = new List<WorkItemStateTransition>
            {
                new() { FromState = "New", ToState = "Active", TransitionedAt = enteredCurrentState },
            };

            var incoming = new WorkItem(new WorkItemBase
            {
                ReferenceId = "5025",
                Name = "Story 5025",
                Type = "Story",
                State = team.MapRawStateToMappedName("Active"),
                StateCategory = StateCategories.Doing,
                Order = "5025",
                StartedDate = enteredCurrentState.AddDays(-1),
                SyncedTransitions = WorkItemStateTransitionMapper.MapToMappedStates(rawTransitions, team),
            }, team);

            var subject = CreateSubject(team, incoming);

            await subject.UpdateWorkItemsForTeam(team);

            var transitionRepository = new WorkItemStateTransitionRepository(DatabaseContext, Mock.Of<ILogger<WorkItemStateTransitionRepository>>());
            var workItemRepository = new WorkItemRepository(DatabaseContext, Mock.Of<ILogger<WorkItemRepository>>());

            var persistedItem = workItemRepository.GetByPredicate(wi => wi.ReferenceId == "5025");
            var persistedTransitions = transitionRepository.GetAll().ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(persistedItem, Is.Not.Null);
                Assert.That(persistedTransitions, Has.Count.EqualTo(1));
                Assert.That(persistedTransitions[0].WorkItemId, Is.EqualTo(persistedItem!.Id));
                Assert.That(persistedTransitions[0].WorkItemId, Is.GreaterThan(0));
                Assert.That(persistedTransitions[0].ToState, Is.EqualTo("In Progress"));
                Assert.That(persistedItem.CurrentStateEnteredAt, Is.EqualTo(enteredCurrentState));
            }
        }

        private async Task<Team> GivenPersistedTeam(string rawState, string mappedState)
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = $"Connection {Guid.NewGuid():N}",
                WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
            };

            var team = new Team
            {
                Name = $"Team {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = connection,
                StateMappings = [new StateMapping { Name = mappedState, States = [rawState] }],
            };
            team.WorkItemTypes.Add("Story");

            var teamRepository = new TeamRepository(DatabaseContext, Mock.Of<ILogger<TeamRepository>>());
            teamRepository.Add(team);
            await teamRepository.Save();

            return team;
        }

        private WorkItemService CreateSubject(Team team, params WorkItem[] incomingItems)
        {
            var connectorMock = new Mock<IWorkTrackingConnector>();
            connectorMock.Setup(x => x.GetWorkItemsForTeam(team)).ReturnsAsync(incomingItems.ToList());

            var connectorFactoryMock = new Mock<IWorkTrackingConnectorFactory>();
            connectorFactoryMock.Setup(x => x.GetWorkTrackingConnector(It.IsAny<WorkTrackingSystems>())).Returns(connectorMock.Object);

            var workItemRepository = new WorkItemRepository(DatabaseContext, Mock.Of<ILogger<WorkItemRepository>>());
            var transitionRepository = new WorkItemStateTransitionRepository(DatabaseContext, Mock.Of<ILogger<WorkItemStateTransitionRepository>>());

            return new WorkItemService(
                Mock.Of<ILogger<WorkItemService>>(),
                connectorFactoryMock.Object,
                Mock.Of<IRepository<Feature>>(),
                workItemRepository,
                Mock.Of<IPortfolioMetricsService>(),
                Mock.Of<IRepository<Team>>(),
                transitionRepository,
                Mock.Of<IFeatureStateTransitionRepository>(),
                Mock.Of<Backend.Services.Interfaces.DomainEvents.IDomainEventDispatcher>(),
                new BlockedItemService(new RuleEvaluator<WorkItem>(), new WorkItemFieldProvider()),
                Mock.Of<Lighthouse.Backend.Services.Interfaces.Repositories.IFeatureBlockedTransitionRepository>(r => r.GetOpenSpellsForPortfolio(It.IsAny<int>()) == new Dictionary<int, Lighthouse.Backend.Models.FeatureBlockedTransition>()));
        }
    }
}
