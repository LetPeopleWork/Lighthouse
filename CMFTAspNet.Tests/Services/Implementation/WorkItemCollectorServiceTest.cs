using CMFTAspNet.Models;
using CMFTAspNet.Services.Factories;
using CMFTAspNet.Services.Implementation;
using CMFTAspNet.Services.Interfaces;
using CMFTAspNet.WorkTracking;
using Moq;

namespace CMFTAspNet.Tests.Services.Implementation
{
    public class WorkItemCollectorServiceTest
    {
        private Mock<IWorkItemService> workItemServiceMock;

        private Mock<IRepository<Feature>> featureRepositoryMock;

        private WorkItemCollectorService subject;

        [SetUp]
        public void SetUp()
        {
            workItemServiceMock = new Mock<IWorkItemService>();
            featureRepositoryMock = new Mock<IRepository<Feature>>();

            var workItemServiceFactoryMock = new Mock<IWorkItemServiceFactory>();
            workItemServiceFactoryMock.Setup(x => x.GetWorkItemServiceForWorkTrackingSystem(It.IsAny<WorkTrackingSystems>())).Returns(workItemServiceMock.Object);

            workItemServiceMock.Setup(x => x.GetWorkItemsByAreaPath(It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<Team>())).Returns(Task.FromResult(new List<int>()));
            workItemServiceMock.Setup(x => x.GetWorkItemsByTag(It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<Team>())).Returns(Task.FromResult(new List<int>()));

            subject = new WorkItemCollectorService(workItemServiceFactoryMock.Object, featureRepositoryMock.Object);
        }

        [Test]
        public async Task CollectFeaturesForProject_SingleTeamInvolved_SearchByTag_FindsFeauture()
        {
            var team = CreateTeam();
            var project = CreateProject(SearchBy.Tag, [team]);
            var feature = new Feature(team, 12) { ReferenceId = 12 };

            workItemServiceMock.Setup(x => x.GetWorkItemsByTag(project.WorkItemTypes, project.SearchTerm, It.IsAny<Team>())).Returns(Task.FromResult(new List<int> { feature.ReferenceId }));
            workItemServiceMock.Setup(x => x.GetRemainingRelatedWorkItems(feature.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult(12));

            await subject.UpdateFeaturesForProject(project);

            Assert.That(project.Features.ToList(), Has.Count.EqualTo(1));

            var actualFeature = project.Features.Single();
            Assert.That(actualFeature.ReferenceId, Is.EqualTo(feature.ReferenceId));
        }

        [Test]
        public async Task CollectFeaturesForProject_GivenExistingFeatures_ClearsExistingFeatures()
        {
            var team = CreateTeam();
            var project = CreateProject(SearchBy.Tag, [team]);
            var existingFeature = new Feature(team, 12) { Id = 12 };

            project.Features.Add(existingFeature);

            workItemServiceMock.Setup(x => x.GetWorkItemsByTag(project.WorkItemTypes, project.SearchTerm, It.IsAny<Team>())).Returns(Task.FromResult(new List<int>()));
            workItemServiceMock.Setup(x => x.GetRemainingRelatedWorkItems(existingFeature.Id, It.IsAny<Team>())).Returns(Task.FromResult(12));

            await subject.UpdateFeaturesForProject(project);

            Assert.That(project.Features, Is.Empty);
        }

        [Test]
        public async Task CollectFeaturesForProject_SingleTeamInvolved_SearchByAreaPath_FindsFeature()
        {
            var team = CreateTeam();
            var project = CreateProject(SearchBy.AreaPath, [team]);
            var feature = new Feature(team, 12) { ReferenceId = 12 };

            workItemServiceMock.Setup(x => x.GetWorkItemsByAreaPath(project.WorkItemTypes, project.SearchTerm, It.IsAny<Team>())).Returns(Task.FromResult(new List<int> { feature.ReferenceId }));
            workItemServiceMock.Setup(x => x.GetRemainingRelatedWorkItems(feature.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult(12));

            await subject.UpdateFeaturesForProject(project);

            Assert.That(project.Features.ToList(), Has.Count.EqualTo(1));

            var actualFeature = project.Features.Single();
            Assert.That(actualFeature.ReferenceId, Is.EqualTo(feature.ReferenceId));
        }

        [Test]
        public async Task CollectFeaturesForProject_NoRemainingWork_AddsDefaultRemainingWorkToFeature()
        {
            var team = CreateTeam();
            var project = CreateProject(SearchBy.AreaPath, [team]);
            project.DefaultAmountOfWorkItemsPerFeature = 12;

            var feature = new Feature(team, 0) { Id = 42 };

            workItemServiceMock.Setup(x => x.GetWorkItemsByAreaPath(project.WorkItemTypes, project.SearchTerm, It.IsAny<Team>())).Returns(Task.FromResult(new List<int> { feature.Id }));
            workItemServiceMock.Setup(x => x.GetRemainingRelatedWorkItems(feature.Id, It.IsAny<Team>())).Returns(Task.FromResult(0));

            await subject.UpdateFeaturesForProject(project);

            Assert.That(project.Features, Has.Count.EqualTo(1));
            Assert.That(project.Features.Single().RemainingWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(12));
        }

        [Test]
        public async Task CollectFeaturesForProject_SingleTeamInvolved_FindsRemainingWorkByTeam()
        {
            var team = CreateTeam();
            var project = CreateProject(SearchBy.AreaPath, [team]);

            var remainingWorkItems = 12;
            var feature = new Feature(team, remainingWorkItems) { Id = 42 };

            workItemServiceMock.Setup(x => x.GetWorkItemsByAreaPath(project.WorkItemTypes, project.SearchTerm, It.IsAny<Team>())).Returns(Task.FromResult(new List<int> { feature.Id }));
            workItemServiceMock.Setup(x => x.GetRemainingRelatedWorkItems(feature.Id, It.IsAny<Team>())).Returns(Task.FromResult(remainingWorkItems));

            await subject.UpdateFeaturesForProject(project);

            var actualFeature = project.Features.Single();
            Assert.That(actualFeature.GetRemainingWorkForTeam(team), Is.EqualTo(remainingWorkItems));
        }


        [Test]
        public async Task CollectFeaturesForProject_MultipleTeamsInvolved()
        {
            var team1 = CreateTeam();
            var team2 = CreateTeam();
            var project = CreateProject(SearchBy.Tag, [team1, team2]);

            workItemServiceMock.Setup(x => x.GetWorkItemsByTag(It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<Team>())).Returns(Task.FromResult(new List<int>()));

            await subject.UpdateFeaturesForProject(project);

            workItemServiceMock.Verify(x => x.GetWorkItemsByTag(project.WorkItemTypes, It.IsAny<string>(), team1), Times.Exactly(1));
            workItemServiceMock.Verify(x => x.GetWorkItemsByTag(project.WorkItemTypes, It.IsAny<string>(), team2), Times.Exactly(1));
        }

        [Test]
        public async Task CollectFeaturesForProject_TwoTeamsInvolved_IndividualFeatures_FindsRemainingWorkByTeam()
        {
            var team1 = CreateTeam();
            var team2 = CreateTeam();
            var project = CreateProject(SearchBy.AreaPath, [team1, team2]);

            var remainingWorkItemsFeature1 = 12;
            var remainingWorkItemsFeature2 = 1337;
            var feature1 = new Feature(team1, remainingWorkItemsFeature1) { Id = 1 };
            var feature2 = new Feature(team2, remainingWorkItemsFeature1) { Id = 2 };

            workItemServiceMock.Setup(x => x.GetWorkItemsByAreaPath(project.WorkItemTypes, project.SearchTerm, team1)).Returns(Task.FromResult(new List<int> { feature1.Id }));
            workItemServiceMock.Setup(x => x.GetRemainingRelatedWorkItems(feature1.Id, team1)).Returns(Task.FromResult(remainingWorkItemsFeature1));

            workItemServiceMock.Setup(x => x.GetWorkItemsByAreaPath(project.WorkItemTypes, project.SearchTerm, team2)).Returns(Task.FromResult(new List<int> { feature2.Id }));
            workItemServiceMock.Setup(x => x.GetRemainingRelatedWorkItems(feature2.Id, team2)).Returns(Task.FromResult(remainingWorkItemsFeature2));

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
            var project = CreateProject(SearchBy.AreaPath, [team1, team2]);

            var remainingWorkItemsTeam1 = 12;
            var remainingWorkItemsTeam2 = 7;
            var feature = new Feature(team1, remainingWorkItemsTeam1) { Id = 1 };

            workItemServiceMock.Setup(x => x.GetWorkItemsByAreaPath(project.WorkItemTypes, project.SearchTerm, It.IsAny<Team>())).Returns(Task.FromResult(new List<int> { feature.Id }));
            workItemServiceMock.Setup(x => x.GetRemainingRelatedWorkItems(feature.Id, team1)).Returns(Task.FromResult(remainingWorkItemsTeam1));
            workItemServiceMock.Setup(x => x.GetRemainingRelatedWorkItems(feature.Id, team2)).Returns(Task.FromResult(remainingWorkItemsTeam2));

            await subject.UpdateFeaturesForProject(project);

            var actualFeature = project.Features.Single();
            Assert.Multiple(() =>
            {
                Assert.That(actualFeature.GetRemainingWorkForTeam(team1), Is.EqualTo(remainingWorkItemsTeam1));
                Assert.That(actualFeature.GetRemainingWorkForTeam(team2), Is.EqualTo(remainingWorkItemsTeam2));
            });
        }

        [Test]
        public async Task CollectFeaturesForProject_SearchByAreaPath_UnparentedItems_CreatesDummyFeatureForUnparented()
        {
            var team = CreateTeam();
            var project = CreateProject(SearchBy.AreaPath, [team]);

            project.IncludeUnparentedItems = true;

            var unparentedItems = new int[] { 12, 1337, 42 };

            workItemServiceMock.Setup(x => x.GetWorkItemsByAreaPath(project.WorkItemTypes, project.SearchTerm, It.IsAny<Team>())).Returns(Task.FromResult(new List<int>()));
            workItemServiceMock.Setup(x => x.GetRemainingRelatedWorkItems(It.IsAny<int>(), It.IsAny<Team>())).Returns(Task.FromResult(0));

            workItemServiceMock.Setup(x => x.GetNotClosedWorkItemsByAreaPath(team.WorkItemTypes, project.SearchTerm, team)).Returns(Task.FromResult(new List<int>(unparentedItems)));

            await subject.UpdateFeaturesForProject(project);

            var actualFeature = project.Features.Single();
            Assert.Multiple(() =>
            {
                Assert.That(actualFeature.Name, Is.EqualTo("Release 1 - Unparented"));
                Assert.That(actualFeature.GetRemainingWorkForTeam(team), Is.EqualTo(unparentedItems.Length));
            });
        }

        [Test]
        public async Task CollectFeaturesForProject_SearchByTag_UnparentedItems_CreatesDummyFeatureForUnparented()
        {
            var team = CreateTeam();
            var project = CreateProject(SearchBy.Tag, [team]);

            project.IncludeUnparentedItems = true;

            var unparentedItems = new int[] { 12, 1337, 42 };

            workItemServiceMock.Setup(x => x.GetWorkItemsByTag(project.WorkItemTypes, project.SearchTerm, It.IsAny<Team>())).Returns(Task.FromResult(new List<int>()));
            workItemServiceMock.Setup(x => x.GetRemainingRelatedWorkItems(It.IsAny<int>(), It.IsAny<Team>())).Returns(Task.FromResult(0));

            workItemServiceMock.Setup(x => x.GetNotClosedWorkItemsByTag(team.WorkItemTypes, project.SearchTerm, team)).Returns(Task.FromResult(new List<int>(unparentedItems)));

            await subject.UpdateFeaturesForProject(project);

            var actualFeature = project.Features.Single();
            Assert.Multiple(() =>
            {
                Assert.That(actualFeature.Name, Is.EqualTo("Release 1 - Unparented"));
                Assert.That(actualFeature.GetRemainingWorkForTeam(team), Is.EqualTo(unparentedItems.Length));
            });
        }

        private Team CreateTeam()
        {
            var team = new Team { Name = "Team" };

            team.WorkItemTypes.Add("User Story");

            return team;
        }

        private Project CreateProject(SearchBy searchBy, params Team[] teams)
        {
            var project = new Project
            {
                Name = "Release 1",
                SearchBy = searchBy,
                SearchTerm = "Release 1.33.7",
            };

            project.WorkItemTypes.Add("Feature");

            foreach (var team in teams)
            {
                project.InvolvedTeams.Add(team);
            }

            return project;
        }
    }
}
