using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkItems;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkItems
{
    public class DemoDataStateEnteredSeamIntegrationTest : IntegrationTestBase
    {
        [Test]
        public async Task UpdateWorkItemsForTeam_TeamZenithDemoData_DerivesCurrentStateEnteredAtForInProgressItems_IncludingBlockedAndStale()
        {
            var team = await GivenPersistedDemoTeam("Team Zenith");

            var subject = CreateSubject(team);

            await subject.UpdateWorkItemsForTeam(team);

            var inProgressItems = LoadInProgressItems(team);
            var blockedAndStale = inProgressItems.SingleOrDefault(wi => wi.IsBlocked);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(inProgressItems, Is.Not.Empty);
                Assert.That(inProgressItems, Has.All.Matches<WorkItem>(wi => wi.CurrentStateEnteredAt != null),
                    "Every in-progress demo item must carry a derived CurrentStateEnteredAt sourced from the StateEnteredDate column.");
                Assert.That(blockedAndStale, Is.Not.Null,
                    "Team Zenith must contain a blocked in-progress demo item so the E2E can prove blocked-excludes-stale.");
                Assert.That(blockedAndStale!.CurrentStateEnteredAt!.Value.Date, Is.LessThanOrEqualTo(DateTime.UtcNow.Date.AddDays(-7)),
                    "The blocked in-progress demo item must be clearly stale (entered its state well over a week ago).");
            }
        }

        private async Task<Team> GivenPersistedDemoTeam(string teamName)
        {
            var demoFactory = new DemoDataFactory(GetWorkTrackingSystemFactory());
            var team = demoFactory.CreateDemoTeam(teamName);
            team.WorkTrackingSystemConnection = demoFactory.CreateDemoWorkTrackingSystemConnection();

            var teamRepository = new TeamRepository(DatabaseContext, Mock.Of<ILogger<TeamRepository>>());
            teamRepository.Add(team);
            await teamRepository.Save();

            return team;
        }

        private List<WorkItem> LoadInProgressItems(Team team)
        {
            var workItemRepository = new WorkItemRepository(DatabaseContext, Mock.Of<ILogger<WorkItemRepository>>());
            return workItemRepository
                .GetAllByPredicate(wi => wi.TeamId == team.Id && wi.StateCategory == StateCategories.Doing)
                .ToList();
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
