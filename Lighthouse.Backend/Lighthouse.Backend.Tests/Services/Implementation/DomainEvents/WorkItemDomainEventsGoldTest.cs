using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkItems;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.DomainEvents
{
    [TestFixture]
    [NonParallelizable]
    public class WorkItemDomainEventsGoldTest : IntegrationTestBase
    {
        private static readonly TransitionProbeState ProbeState = new();

        public WorkItemDomainEventsGoldTest()
            : base(new GoldTestWebApplicationFactory())
        {
        }

        [SetUp]
        public void ResetProbe()
        {
            ProbeState.Reset();
        }

        [Test]
        public async Task SyncWithStateChange_FiresTransitionedHandler_SurvivesThrowingSibling_AndReDrivesOnRepublish()
        {
            var team = await GivenPersistedTeam("Active", "In Progress");
            var enteredCurrentState = new DateTime(2026, 5, 25, 12, 0, 0, DateTimeKind.Utc);
            var incoming = GivenIncomingItemEnteringState(team, "5025", enteredCurrentState);

            var subject = CreateSubject(team, incoming);
            await subject.UpdateWorkItemsForTeam(team);

            var persistedItem = ReloadWorkItem("5025");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ProbeState.HandledWorkItemIds, Is.EqualTo(new[] { persistedItem!.Id }), "the non-throwing probe handler ran despite a sibling throwing");
                Assert.That(persistedItem, Is.Not.Null, "the committed work-item fact survives a throwing handler");
                Assert.That(persistedItem!.CurrentStateEnteredAt, Is.EqualTo(enteredCurrentState), "the committed derived projection survives a throwing handler");
            }

            var dispatcher = ServiceProvider.GetRequiredService<IDomainEventDispatcher>();
            await dispatcher.PublishAsync(new WorkItemTransitioned(persistedItem!.Id, "New", "In Progress"));

            Assert.That(ProbeState.HandledWorkItemIds, Is.EqualTo(new[] { persistedItem.Id, persistedItem.Id }), "the next publish re-drives the reaction");
        }

        [Test]
        public async Task SyncEmittingDomainEvents_LeavesStateTransitionTableAsProjectionOnly_NoExtraRowsPersistedForEvents()
        {
            var team = await GivenPersistedTeam("Active", "In Progress");
            var enteredCurrentState = new DateTime(2026, 5, 25, 12, 0, 0, DateTimeKind.Utc);
            var incoming = GivenIncomingItemEnteringState(team, "6036", enteredCurrentState);

            var subject = CreateSubject(team, incoming);
            await subject.UpdateWorkItemsForTeam(team);

            var transitionRepository = new WorkItemStateTransitionRepository(DatabaseContext, Mock.Of<ILogger<WorkItemStateTransitionRepository>>());
            var persistedItem = ReloadWorkItem("6036");
            var persistedTransitions = transitionRepository.GetAll().ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(persistedTransitions, Has.Count.EqualTo(1), "only the single synced transition row is persisted; events persist nothing");
                Assert.That(persistedTransitions[0].WorkItemId, Is.EqualTo(persistedItem!.Id));
                Assert.That(persistedTransitions[0].ToState, Is.EqualTo("In Progress"));
                Assert.That(persistedTransitions[0].TransitionedAt, Is.EqualTo(enteredCurrentState));
            }
        }

        private static WorkItem GivenIncomingItemEnteringState(Team team, string referenceId, DateTime enteredCurrentState)
        {
            var rawTransitions = new List<WorkItemStateTransition>
            {
                new() { FromState = "New", ToState = "Active", TransitionedAt = enteredCurrentState },
            };

            return new WorkItem(new WorkItemBase
            {
                ReferenceId = referenceId,
                Name = $"Story {referenceId}",
                Type = "Story",
                State = team.MapRawStateToMappedName("Active"),
                StateCategory = StateCategories.Doing,
                Order = referenceId,
                StartedDate = enteredCurrentState.AddDays(-1),
                SyncedTransitions = WorkItemStateTransitionMapper.MapToMappedStates(rawTransitions, team),
            }, team);
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

        private WorkItem? ReloadWorkItem(string referenceId)
        {
            var workItemRepository = new WorkItemRepository(DatabaseContext, Mock.Of<ILogger<WorkItemRepository>>());
            return workItemRepository.GetByPredicate(wi => wi.ReferenceId == referenceId);
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
                ServiceProvider.GetRequiredService<IDomainEventDispatcher>());
        }

        private sealed class TransitionProbeState
        {
            private readonly List<int> handledWorkItemIds = [];

            public IReadOnlyList<int> HandledWorkItemIds
            {
                get
                {
                    lock (handledWorkItemIds)
                    {
                        return handledWorkItemIds.ToList();
                    }
                }
            }

            public void Record(int workItemId)
            {
                lock (handledWorkItemIds)
                {
                    handledWorkItemIds.Add(workItemId);
                }
            }

            public void Reset()
            {
                lock (handledWorkItemIds)
                {
                    handledWorkItemIds.Clear();
                }
            }
        }

        private sealed class ProbeHandler(TransitionProbeState state) : IDomainEventHandler<WorkItemTransitioned>
        {
            public Task HandleAsync(WorkItemTransitioned domainEvent, CancellationToken cancellationToken)
            {
                state.Record(domainEvent.WorkItemId);
                return Task.CompletedTask;
            }
        }

        private sealed class ThrowingProbeHandler : IDomainEventHandler<WorkItemTransitioned>
        {
            public Task HandleAsync(WorkItemTransitioned domainEvent, CancellationToken cancellationToken)
            {
                throw new InvalidOperationException("gold-test handler boom");
            }
        }

        private sealed class GoldTestWebApplicationFactory : TestWebApplicationFactory<Program>
        {
            protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton(ProbeState);
                    services.AddScoped<IDomainEventHandler<WorkItemTransitioned>, ThrowingProbeHandler>();
                    services.AddScoped<IDomainEventHandler<WorkItemTransitioned>, ProbeHandler>();
                });
            }
        }
    }
}
