using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkItems;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkItems
{
    public class WorkItemServiceCsvStateEnteredSeamIntegrationTest : IntegrationTestBase
    {
        private const string StateSinceColumn = "StateSince";

        [Test]
        public async Task UpdateWorkItemsForTeam_CsvWithStateEnteredColumn_DerivesCurrentStateEnteredAtFromColumnDate()
        {
            var enteredCurrentState = DateTime.UtcNow.Date.AddDays(-4);
            var team = await GivenPersistedTeam(CsvWithInProgressItem("CSV-STATE-1", enteredCurrentState));

            var subject = CreateSubject(team);

            await subject.UpdateWorkItemsForTeam(team);

            var persistedItem = LoadWorkItem("CSV-STATE-1");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(persistedItem, Is.Not.Null);
                Assert.That(persistedItem!.State, Is.EqualTo("In Progress"));
                Assert.That(persistedItem.CurrentStateEnteredAt, Is.Not.Null);
                Assert.That(persistedItem.CurrentStateEnteredAt!.Value.Date, Is.EqualTo(enteredCurrentState.Date).Within(TimeSpan.FromDays(1)));
            }
        }

        [Test]
        public async Task UpdateWorkItemsForTeam_CsvWithStateEnteredColumn_ReSync_DoesNotDuplicateTransition()
        {
            var enteredCurrentState = DateTime.UtcNow.Date.AddDays(-4);
            var team = await GivenPersistedTeam(CsvWithInProgressItem("CSV-STATE-2", enteredCurrentState));

            var subject = CreateSubject(team);

            await subject.UpdateWorkItemsForTeam(team);
            await subject.UpdateWorkItemsForTeam(team);

            var persistedTransitions = LoadTransitions();

            Assert.That(persistedTransitions.Count(t => t.ToState == "In Progress"), Is.EqualTo(1),
                "Re-syncing the same CSV with a state-since column must not duplicate the derived transition (DDD-7 idempotency).");
        }

        private static string CsvWithInProgressItem(string referenceId, DateTime enteredCurrentState)
        {
            var startedDate = enteredCurrentState.AddDays(-1).ToString("yyyy-MM-dd");
            var since = enteredCurrentState.ToString("yyyy-MM-dd");
            return string.Join(
                "\n",
                "ID,Name,State,Type,Started Date,Closed Date,StateSince",
                $"{referenceId},In progress story,In Progress,Story,{startedDate},,{since}");
        }

        private async Task<Team> GivenPersistedTeam(string csvContent)
        {
            var connection = GetWorkTrackingSystemFactory().CreateDefaultConnectionForWorkTrackingSystem(WorkTrackingSystems.Csv);
            connection.Name = $"Connection {Guid.NewGuid():N}";
            connection.Options.Single(o => o.Key == CsvWorkTrackingOptionNames.DateTimeFormat).Value = "yyyy-MM-dd";
            connection.Options.Single(o => o.Key == CsvWorkTrackingOptionNames.StateEnteredDateHeader).Value = StateSinceColumn;

            var team = new Team
            {
                Name = $"Team {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = connection,
                DataRetrievalValue = csvContent,
                ToDoStates = ["To Do"],
                DoingStates = ["In Progress"],
                DoneStates = ["Done"],
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

        private IWorkTrackingSystemFactory GetWorkTrackingSystemFactory()
        {
            return ServiceProvider.GetService<IWorkTrackingSystemFactory>() ?? throw new InvalidOperationException("Could not resolve Work Tracking System Factory");
        }

        private WorkItemService CreateSubject(Team team)
        {
            var realConnector = ServiceProvider.GetService<IWorkTrackingConnectorFactory>()!
                .GetWorkTrackingConnector(WorkTrackingSystems.Csv);

            var connectorFactoryMock = new Mock<IWorkTrackingConnectorFactory>();
            connectorFactoryMock.Setup(x => x.GetWorkTrackingConnector(It.IsAny<WorkTrackingSystems>())).Returns(realConnector);

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
                Mock.Of<IFeatureStateTransitionRepository>());
        }
    }
}
