using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.WorkTracking;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    public class WorkItemCollectorServiceTest
    {
        private Mock<IWorkItemService> workItemServiceMock;

        private Mock<IRepository<Feature>> featureRepositoryMock;

        private Mock<IRepository<Team>> teamRepositoryMock;

        private WorkItemCollectorService subject;

        [SetUp]
        public void SetUp()
        {
            workItemServiceMock = new Mock<IWorkItemService>();
            featureRepositoryMock = new Mock<IRepository<Feature>>();
            teamRepositoryMock = new Mock<IRepository<Team>>();

            var workItemServiceFactoryMock = new Mock<IWorkItemServiceFactory>();
            workItemServiceFactoryMock.Setup(x => x.GetWorkItemServiceForWorkTrackingSystem(It.IsAny<WorkTrackingSystems>())).Returns(workItemServiceMock.Object);

            workItemServiceMock.Setup(x => x.GetOpenWorkItems(It.IsAny<IEnumerable<string>>(), It.IsAny<Team>())).Returns(Task.FromResult(new List<string>()));

            subject = new WorkItemCollectorService(workItemServiceFactoryMock.Object, featureRepositoryMock.Object, teamRepositoryMock.Object, Mock.Of<ILogger<WorkItemCollectorService>>());
        }

        [Test]
        public async Task CollectFeaturesForProject_SingleTeamInvolved_SearchByTag_FindsFeauture()
        {
            var team = CreateTeam();
            SetupTeams(team);

            var project = CreateProject();
            var feature = new Feature(team, 12) { ReferenceId = "12" };

            workItemServiceMock.Setup(x => x.GetOpenWorkItems(project.WorkItemTypes, It.IsAny<IWorkItemQueryOwner>())).Returns(Task.FromResult(new List<string> { feature.ReferenceId }));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult((12, 12)));

            await subject.UpdateFeaturesForProject(project);

            Assert.That(project.Features.ToList(), Has.Count.EqualTo(1));

            var actualFeature = project.Features.Single();
            Assert.That(actualFeature.ReferenceId, Is.EqualTo(feature.ReferenceId));
        }

        [Test]
        public async Task CollectFeaturesForProject_GivenExistingFeatures_ClearsExistingFeatures()
        {
            var team = CreateTeam();
            var project = CreateProject();
            var existingFeature = new Feature(team, 12) { Id = 12 };

            project.Features.Add(existingFeature);

            workItemServiceMock.Setup(x => x.GetOpenWorkItems(project.WorkItemTypes, It.IsAny<IWorkItemQueryOwner>())).Returns(Task.FromResult(new List<string>()));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(existingFeature.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult((12, 12)));

            await subject.UpdateFeaturesForProject(project);

            Assert.That(project.Features, Is.Empty);
        }

        [Test]
        public async Task CollectFeaturesForProject_SingleTeamInvolved_SearchByAreaPath_FindsFeature()
        {
            var team = CreateTeam();
            SetupTeams(team);

            var project = CreateProject();
            var feature = new Feature(team, 12) { ReferenceId = "12" };

            workItemServiceMock.Setup(x => x.GetOpenWorkItems(project.WorkItemTypes, It.IsAny<IWorkItemQueryOwner>())).Returns(Task.FromResult(new List<string> { feature.ReferenceId }));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult((12, 12)));

            await subject.UpdateFeaturesForProject(project);

            Assert.That(project.Features.ToList(), Has.Count.EqualTo(1));

            var actualFeature = project.Features.Single();
            Assert.That(actualFeature.ReferenceId, Is.EqualTo(feature.ReferenceId));
        }

        [Test]
        public async Task CollectFeaturesForProject_NoRemainingWork_FeatureHasNoChildren_AddsDefaultRemainingWorkToFeature()
        {
            var team = CreateTeam([1]);
            var project = CreateProject();
            project.DefaultAmountOfWorkItemsPerFeature = 12;

            SetupTeams(team);

            var feature1 = new Feature(team, 0) { ReferenceId = "42" };
            var feature2 = new Feature(team, 2) { ReferenceId = "12" };

            workItemServiceMock.Setup(x => x.GetOpenWorkItems(project.WorkItemTypes, It.IsAny<IWorkItemQueryOwner>())).Returns(Task.FromResult(new List<string> { feature1.ReferenceId, feature2.ReferenceId }));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature1.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult((0, 12)));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature2.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult((2, 12)));

            workItemServiceMock.Setup(x => x.ItemHasChildren(feature1.ReferenceId, project)).ReturnsAsync(false);

            await subject.UpdateFeaturesForProject(project);

            Assert.That(project.Features, Has.Count.EqualTo(2));
            Assert.That(project.Features.First().FeatureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(12));
        }

        [Test]
        public async Task CollectFeaturesForProject_NoRemainingWork_FeatureHasNoChildren_SizeEstimateFieldSet_SizeEstimateNotAvailable_AddsDefaultRemainingWorkToFeature()
        {
            var team = CreateTeam([1]);
            var project = CreateProject();
            project.DefaultAmountOfWorkItemsPerFeature = 12;
            project.SizeEstimateField = "customfield_10037";

            SetupTeams(team);

            var feature1 = new Feature(team, 0) { ReferenceId = "42" };
            var feature2 = new Feature(team, 2) { ReferenceId = "12" };

            workItemServiceMock.Setup(x => x.GetOpenWorkItems(project.WorkItemTypes, It.IsAny<IWorkItemQueryOwner>())).Returns(Task.FromResult(new List<string> { feature1.ReferenceId, feature2.ReferenceId }));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature1.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult((0, 12)));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature2.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult((2, 12)));

            workItemServiceMock.Setup(x => x.GetEstimatedSizeForItem(feature1.ReferenceId, project)).Returns(Task.FromResult(0));

            workItemServiceMock.Setup(x => x.ItemHasChildren(feature1.ReferenceId, project)).ReturnsAsync(false);

            await subject.UpdateFeaturesForProject(project);

            Assert.That(project.Features, Has.Count.EqualTo(2));
            Assert.That(project.Features.First().FeatureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(12));
        }

        [Test]
        public async Task CollectFeaturesForProject_NoRemainingWork_FeatureHasNoChildren_SizeEstimateFieldSet_SizeEstimateAvailable_AddsEstimatedWorkToFeature()
        {
            var team = CreateTeam([1]);
            var project = CreateProject();
            project.DefaultAmountOfWorkItemsPerFeature = 12;
            project.SizeEstimateField = "customfield_10037";

            SetupTeams(team);

            var feature1 = new Feature(team, 0) { ReferenceId = "42" };
            var feature2 = new Feature(team, 2) { ReferenceId = "12" };

            workItemServiceMock.Setup(x => x.GetOpenWorkItems(project.WorkItemTypes, It.IsAny<IWorkItemQueryOwner>())).Returns(Task.FromResult(new List<string> { feature1.ReferenceId, feature2.ReferenceId }));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature1.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult((0, 12)));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature2.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult((2, 12)));

            workItemServiceMock.Setup(x => x.GetEstimatedSizeForItem(feature1.ReferenceId, project)).Returns(Task.FromResult(7));

            workItemServiceMock.Setup(x => x.ItemHasChildren(feature1.ReferenceId, project)).ReturnsAsync(false);

            await subject.UpdateFeaturesForProject(project);

            Assert.That(project.Features, Has.Count.EqualTo(2));
            Assert.That(project.Features.First().FeatureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(7));
        }

        [Test]
        public async Task CollectFeaturesForProject_NoRemainingWork_FeatureHasChildren_DoesNotAddDefaultRemainingWorkToFeature()
        {
            var team = CreateTeam([1]);
            var project = CreateProject();
            project.DefaultAmountOfWorkItemsPerFeature = 12;

            SetupTeams(team);

            var feature1 = new Feature(team, 0) { ReferenceId = "42" };
            var feature2 = new Feature(team, 2) { ReferenceId = "12" };

            workItemServiceMock.Setup(x => x.GetOpenWorkItems(project.WorkItemTypes, It.IsAny<IWorkItemQueryOwner>())).Returns(Task.FromResult(new List<string> { feature1.ReferenceId, feature2.ReferenceId }));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature1.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult((0, 12)));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature2.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult((2, 12)));

            workItemServiceMock.Setup(x => x.ItemHasChildren(feature1.ReferenceId, project)).ReturnsAsync(true);

            await subject.UpdateFeaturesForProject(project);

            Assert.That(project.Features, Has.Count.EqualTo(2));
            Assert.That(project.Features.First().FeatureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(0));
        }

        [Test]
        public async Task CollectFeaturesForProject_NoRemainingWork_MulitpleTeams_SplitsDefaultRemainingWorkByTeam()
        {
            var team1 = CreateTeam();
            var team2 = CreateTeam();
            SetupTeams(team1, team2);

            var project = CreateProject();
            project.DefaultAmountOfWorkItemsPerFeature = 12;

            var feature1 = new Feature([(team1, 0, 12), (team2, 0, 10)]) { ReferenceId = "17" };
            var feature2 = new Feature([(team1, 2, 13), (team2, 2, 3)]) { ReferenceId = "19" };

            workItemServiceMock.Setup(x => x.GetOpenWorkItems(project.WorkItemTypes, It.IsAny<IWorkItemQueryOwner>())).Returns(Task.FromResult(new List<string> { feature1.ReferenceId, feature2.ReferenceId }));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature1.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult((0, 12)));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature2.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult((2, 12)));

            await subject.UpdateFeaturesForProject(project);

            Assert.Multiple(() =>
            {
                Assert.That(project.Features, Has.Count.EqualTo(2));
                Assert.That(project.Features.First().FeatureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(12));
                Assert.That(project.Features.First().FeatureWork.First().RemainingWorkItems, Is.EqualTo(6));
                Assert.That(project.Features.First().FeatureWork.Last().RemainingWorkItems, Is.EqualTo(6));
            });
        }

        [Test]
        public async Task CollectFeaturesForProject_NoRemainingWork_MulitpleTeams_OneTeamHasNoThroughput_DoesNotGetRemainingWork()
        {
            var team1 = CreateTeam();
            var team2 = CreateTeam([0]);
            SetupTeams(team1, team2);

            var project = CreateProject();
            project.DefaultAmountOfWorkItemsPerFeature = 12;

            var feature1 = new Feature([(team1, 0, 13), (team2, 0, 37)]) { ReferenceId = "34" };
            var feature2 = new Feature([(team1, 2, 42), (team2, 2, 12)]) { ReferenceId = "12" };

            workItemServiceMock.Setup(x => x.GetOpenWorkItems(project.WorkItemTypes, It.IsAny<IWorkItemQueryOwner>())).Returns(Task.FromResult(new List<string> { feature1.ReferenceId, feature2.ReferenceId }));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature1.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult((0, 0)));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature2.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult((2, 12)));

            await subject.UpdateFeaturesForProject(project);

            Assert.Multiple(() =>
            {
                Assert.That(project.Features, Has.Count.EqualTo(2));
                Assert.That(project.Features.First().FeatureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(12));
                Assert.That(project.Features.First().FeatureWork.Single().RemainingWorkItems, Is.EqualTo(12));
            });
        }

        [Test]
        public async Task CollectFeaturesForProject_SingleTeamInvolved_FindsRemainingWorkByTeam()
        {
            var team = CreateTeam();
            SetupTeams(team);

            var project = CreateProject();

            var remainingWorkItems = 12;
            var feature = new Feature(team, remainingWorkItems) { ReferenceId = "42" };

            workItemServiceMock.Setup(x => x.GetOpenWorkItems(project.WorkItemTypes, It.IsAny<IWorkItemQueryOwner>())).Returns(Task.FromResult(new List<string> { feature.ReferenceId }));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult((remainingWorkItems, 12)));

            await subject.UpdateFeaturesForProject(project);

            var actualFeature = project.Features.Single();
            Assert.That(actualFeature.GetRemainingWorkForTeam(team), Is.EqualTo(remainingWorkItems));
        }


        [Test]
        public async Task CollectFeaturesForProject_MultipleTeamsInvolved()
        {
            var team1 = CreateTeam();
            var team2 = CreateTeam();
            var project = CreateProject();

            SetupTeams(team1, team2);

            workItemServiceMock.Setup(x => x.GetOpenWorkItems(It.IsAny<IEnumerable<string>>(), It.IsAny<IWorkItemQueryOwner>())).Returns(Task.FromResult(new List<string>()));

            await subject.UpdateFeaturesForProject(project);

            workItemServiceMock.Verify(x => x.GetOpenWorkItems(project.WorkItemTypes, project), Times.Exactly(1));
            workItemServiceMock.Verify(x => x.GetOpenWorkItems(project.WorkItemTypes, team1), Times.Never);
            workItemServiceMock.Verify(x => x.GetOpenWorkItems(project.WorkItemTypes, team2), Times.Never);
        }

        [Test]
        public async Task CollectFeaturesForProject_TwoTeamsInvolved_IndividualFeatures_FindsRemainingWorkByTeam()
        {
            var team1 = CreateTeam();
            var team2 = CreateTeam();
            SetupTeams(team1, team2);

            var project = CreateProject();

            var remainingWorkItemsFeature1 = 12;
            var remainingWorkItemsFeature2 = 1337;
            var feature1 = new Feature(team1, remainingWorkItemsFeature1) { ReferenceId = "1" };
            var feature2 = new Feature(team2, remainingWorkItemsFeature1) { ReferenceId = "2" };

            workItemServiceMock.Setup(x => x.GetOpenWorkItems(project.WorkItemTypes, project)).Returns(Task.FromResult(new List<string> { feature1.ReferenceId, feature2.ReferenceId }));

            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature1.ReferenceId, team1)).Returns(Task.FromResult((remainingWorkItemsFeature1, 12)));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature2.ReferenceId, team2)).Returns(Task.FromResult((remainingWorkItemsFeature2, 12)));

            await subject.UpdateFeaturesForProject(project);

            var actualFeature1 = project.Features.First();
            Assert.That(actualFeature1.GetRemainingWorkForTeam(team1), Is.EqualTo(remainingWorkItemsFeature1));
            var actualFeature2 = project.Features.Last();
            Assert.That(actualFeature2.GetRemainingWorkForTeam(team2), Is.EqualTo(remainingWorkItemsFeature2));
        }

        [Test]
        public async Task CollectFeaturesForProject_TwoTeamsInvolved_SingleFeature_FindsRemainingWorkByTeam()
        {
            var team1 = CreateTeam();
            var team2 = CreateTeam();
            SetupTeams(team1, team2);

            var project = CreateProject();

            var remainingWorkItemsTeam1 = 12;
            var remainingWorkItemsTeam2 = 7;
            var feature = new Feature(team1, remainingWorkItemsTeam1) { ReferenceId = "1" };

            workItemServiceMock.Setup(x => x.GetOpenWorkItems(project.WorkItemTypes, project)).Returns(Task.FromResult(new List<string> { feature.ReferenceId }));

            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature.ReferenceId, team1)).Returns(Task.FromResult((remainingWorkItemsTeam1, 12)));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature.ReferenceId, team2)).Returns(Task.FromResult((remainingWorkItemsTeam2, 12)));

            await subject.UpdateFeaturesForProject(project);

            var actualFeature = project.Features.Single();
            Assert.Multiple(() =>
            {
                Assert.That(actualFeature.GetRemainingWorkForTeam(team1), Is.EqualTo(remainingWorkItemsTeam1));
                Assert.That(actualFeature.GetRemainingWorkForTeam(team2), Is.EqualTo(remainingWorkItemsTeam2));
            });
        }

        [Test]
        public async Task CollectFeaturesForProject_UnparentedItems_CreatesDummyFeatureForUnparented()
        {
            var expectedUnparentedOrder = "123";
            var team = CreateTeam();
            SetupTeams(team);

            var project = CreateProject();

            project.UnparentedItemsQuery = "[System.Tags] CONTAINS Release 123";

            var unparentedItems = new string[] { "12", "1337", "42" };

            workItemServiceMock.Setup(x => x.GetOpenWorkItems(project.WorkItemTypes, It.IsAny<IWorkItemQueryOwner>())).Returns(Task.FromResult(new List<string>()));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(It.IsAny<string>(), It.IsAny<Team>())).Returns(Task.FromResult((0, 12)));

            workItemServiceMock.Setup(x => x.GetWorkItemsByQuery(team.WorkItemTypes, team, project.UnparentedItemsQuery)).Returns(Task.FromResult((new List<string>(unparentedItems), new List<string>(unparentedItems))));
            workItemServiceMock.Setup(x => x.GetAdjacentOrderIndex(It.IsAny<IEnumerable<string>>(), RelativeOrder.Above)).Returns(expectedUnparentedOrder);

            await subject.UpdateFeaturesForProject(project);

            var actualFeature = project.Features.Single();
            Assert.Multiple(() =>
            {
                Assert.That(actualFeature.Name, Is.EqualTo("Release 1 - Unparented"));
                Assert.That(actualFeature.GetRemainingWorkForTeam(team), Is.EqualTo(unparentedItems.Length));
                Assert.That(actualFeature.Order, Is.EqualTo(expectedUnparentedOrder));
            });
        }


        private void SetupTeams(params Team[] teams)
        {
            teamRepositoryMock.Setup(x => x.GetAll()).Returns(teams);
        }

        private Team CreateTeam(int[]? throughput = null)
        {
            var team = new Team { Name = "Team" };

            if (throughput == null)
            {
                throughput = [1];
            }

            team.WorkItemTypes.Add("User Story");
            team.UpdateThroughput(throughput);

            var workTrackingConnection = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.Jira };
            team.WorkTrackingSystemConnection = workTrackingConnection;

            return team;
        }

        private Project CreateProject()
        {
            var project = new Project
            {
                Name = "Release 1",
            };

            project.WorkItemTypes.Add("Feature");

            var workTrackingConnection = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.Jira };
            project.WorkTrackingSystemConnection = workTrackingConnection;

            return project;
        }
    }
}
