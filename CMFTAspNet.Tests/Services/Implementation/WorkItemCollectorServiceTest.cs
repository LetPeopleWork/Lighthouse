using CMFTAspNet.Models;
using CMFTAspNet.Models.Teams;
using CMFTAspNet.Services.Factories;
using CMFTAspNet.Services.Implementation;
using CMFTAspNet.Services.Interfaces;
using Moq;

namespace CMFTAspNet.Tests.Services.Implementation
{
    public class WorkItemCollectorServiceTest
    {
        private Mock<IWorkItemService> workItemServiceMock;
        private WorkItemCollectorService subject;

        [SetUp]
        public void SetUp()
        {
            workItemServiceMock = new Mock<IWorkItemService>();
            
            var workItemServiceFactoryMock = new Mock<IWorkItemServiceFactory>();
            workItemServiceFactoryMock.Setup(x => x.CreateWorkItemServiceForTeam(It.IsAny<ITeamConfiguration>())).Returns(workItemServiceMock.Object);

            workItemServiceMock.Setup(x => x.GetWorkItemsByAreaPath(It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<ITeamConfiguration>())).Returns(Task.FromResult(new List<int>()));
            workItemServiceMock.Setup(x => x.GetWorkItemsByTag(It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<ITeamConfiguration>())).Returns(Task.FromResult(new List<int>()));

            subject = new WorkItemCollectorService(workItemServiceFactoryMock.Object);
        }

        [Test]
        public async Task CollectFeaturesForRelease_SingleTeamInvolved_SearchByTag_FindsFeauture()
        {
            var team = CreateTeam();
            var release = CreateReleaseConfiguration(SearchBy.Tag, [team]);
            var feature = new Feature(team, 12) { Id = 12 };

            workItemServiceMock.Setup(x => x.GetWorkItemsByTag(release.WorkItemTypes, release.SearchTerm, It.IsAny<ITeamConfiguration>())).Returns(Task.FromResult(new List<int> { feature.Id }));
            workItemServiceMock.Setup(x => x.GetRemainingRelatedWorkItems(feature.Id, It.IsAny<ITeamConfiguration>())).Returns(Task.FromResult(12));

            var features = await subject.CollectFeaturesForReleases([release]);

            Assert.That(features.Count(), Is.EqualTo(1));

            var actualFeature = features.Single();
            Assert.That(actualFeature.Id, Is.EqualTo(feature.Id));
        }

        [Test]
        public async Task CollectFeaturesForRelease_SingleTeamInvolved_SearchByAreaPath_FindsFeature()
        {
            var team = CreateTeam();
            var release = CreateReleaseConfiguration(SearchBy.AreaPath, [team]);
            var feature = new Feature(team, 12) { Id = 12 };

            workItemServiceMock.Setup(x => x.GetWorkItemsByAreaPath(release.WorkItemTypes, release.SearchTerm, It.IsAny<ITeamConfiguration>())).Returns(Task.FromResult(new List<int> { feature.Id }));
            workItemServiceMock.Setup(x => x.GetRemainingRelatedWorkItems(feature.Id, It.IsAny<ITeamConfiguration>())).Returns(Task.FromResult(12));

            var features = await subject.CollectFeaturesForReleases([release]);

            Assert.That(features.Count(), Is.EqualTo(1));

            var actualFeature = features.Single();
            Assert.That(actualFeature.Id, Is.EqualTo(feature.Id));
        }

        [Test]
        public async Task CollectFeaturesForRelease_NoRemainingWork_IgnoresFeature()
        {
            var team = CreateTeam();
            var release = CreateReleaseConfiguration(SearchBy.AreaPath, [team]);

            var feature = new Feature(team, 0) { Id = 42 };

            workItemServiceMock.Setup(x => x.GetWorkItemsByAreaPath(release.WorkItemTypes, release.SearchTerm, It.IsAny<ITeamConfiguration>())).Returns(Task.FromResult(new List<int> { feature.Id }));
            workItemServiceMock.Setup(x => x.GetRemainingRelatedWorkItems(feature.Id, It.IsAny<ITeamConfiguration>())).Returns(Task.FromResult(0));

            var features = await subject.CollectFeaturesForReleases([release]);

            Assert.That(features.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task CollectFeaturesForRelease_SingleTeamInvolved_FindsRemainingWorkByTeam()
        {
            var team = CreateTeam();
            var release = CreateReleaseConfiguration(SearchBy.AreaPath, [team]);

            var remainingWorkItems = 12;
            var feature = new Feature(team, remainingWorkItems) { Id = 42 };

            workItemServiceMock.Setup(x => x.GetWorkItemsByAreaPath(release.WorkItemTypes, release.SearchTerm, It.IsAny<ITeamConfiguration>())).Returns(Task.FromResult(new List<int> { feature.Id }));
            workItemServiceMock.Setup(x => x.GetRemainingRelatedWorkItems(feature.Id, It.IsAny<ITeamConfiguration>())).Returns(Task.FromResult(remainingWorkItems));

            var features = await subject.CollectFeaturesForReleases([release]);

            var actualFeature = features.Single();
            Assert.That(actualFeature.RemainingWork[team], Is.EqualTo(remainingWorkItems));
        }


        [Test]
        public async Task CollectFeaturesForRelease_MultipleTeamsInvolved()
        {
            var team1 = CreateTeam();
            var team2 = CreateTeam();
            var release = CreateReleaseConfiguration(SearchBy.Tag, [team1, team2]);

            workItemServiceMock.Setup(x => x.GetWorkItemsByTag(It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<ITeamConfiguration>())).Returns(Task.FromResult(new List<int>()));

            _ = await subject.CollectFeaturesForReleases([release]);

            workItemServiceMock.Verify(x => x.GetWorkItemsByTag(release.WorkItemTypes, It.IsAny<string>(), team1.TeamConfiguration), Times.Exactly(1));
            workItemServiceMock.Verify(x => x.GetWorkItemsByTag(release.WorkItemTypes, It.IsAny<string>(), team2.TeamConfiguration), Times.Exactly(1));
        }

        [Test]
        public async Task CollectFeaturesForRelease_TwoTeamsInvolved_IndividualFeatures_FindsRemainingWorkByTeam()
        {
            var team1 = CreateTeam();
            var team2 = CreateTeam();
            var release = CreateReleaseConfiguration(SearchBy.AreaPath, [team1, team2]);

            var remainingWorkItemsFeature1 = 12;
            var remainingWorkItemsFeature2 = 1337;
            var feature1 = new Feature(team1, remainingWorkItemsFeature1) { Id = 1 };
            var feature2 = new Feature(team2, remainingWorkItemsFeature1) { Id = 2 };

            workItemServiceMock.Setup(x => x.GetWorkItemsByAreaPath(release.WorkItemTypes, release.SearchTerm, team1.TeamConfiguration)).Returns(Task.FromResult(new List<int> { feature1.Id }));
            workItemServiceMock.Setup(x => x.GetRemainingRelatedWorkItems(feature1.Id, team1.TeamConfiguration)).Returns(Task.FromResult(remainingWorkItemsFeature1));

            workItemServiceMock.Setup(x => x.GetWorkItemsByAreaPath(release.WorkItemTypes, release.SearchTerm, team2.TeamConfiguration)).Returns(Task.FromResult(new List<int> { feature2.Id }));
            workItemServiceMock.Setup(x => x.GetRemainingRelatedWorkItems(feature2.Id, team2.TeamConfiguration)).Returns(Task.FromResult(remainingWorkItemsFeature2));

            var features = await subject.CollectFeaturesForReleases([release]);

            var actualFeature1 = features.First();
            Assert.That(actualFeature1.RemainingWork[team1], Is.EqualTo(remainingWorkItemsFeature1));
            var actualFeature2 = features.Last();
            Assert.That(actualFeature2.RemainingWork[team2], Is.EqualTo(remainingWorkItemsFeature2));
        }

        [Test]
        public async Task CollectFeaturesForRelease_TwoTeamsInvolved_SingleFeature_FindsRemainingWorkByTeam()
        {
            var team1 = CreateTeam();
            var team2 = CreateTeam();
            var release = CreateReleaseConfiguration(SearchBy.AreaPath, [team1, team2]);

            var remainingWorkItemsTeam1 = 12;
            var remainingWorkItemsTeam2 = 7;
            var feature = new Feature(team1, remainingWorkItemsTeam1) { Id = 1 };

            workItemServiceMock.Setup(x => x.GetWorkItemsByAreaPath(release.WorkItemTypes, release.SearchTerm, It.IsAny<ITeamConfiguration>())).Returns(Task.FromResult(new List<int> { feature.Id }));
            workItemServiceMock.Setup(x => x.GetRemainingRelatedWorkItems(feature.Id, team1.TeamConfiguration)).Returns(Task.FromResult(remainingWorkItemsTeam1));
            workItemServiceMock.Setup(x => x.GetRemainingRelatedWorkItems(feature.Id, team2.TeamConfiguration)).Returns(Task.FromResult(remainingWorkItemsTeam2));

            var features = await subject.CollectFeaturesForReleases([release]);

            var actualFeature = features.Single();
            Assert.Multiple(() =>
            {
                Assert.That(actualFeature.RemainingWork[team1], Is.EqualTo(remainingWorkItemsTeam1));
                Assert.That(actualFeature.RemainingWork[team2], Is.EqualTo(remainingWorkItemsTeam2));
            });
        }

        [Test]
        public async Task CollectFeaturesForRelease_UnparentedItems_CreatesDummyFeatureForUnparented()
        {
            var team = CreateTeam();
            var release = CreateReleaseConfiguration(SearchBy.AreaPath, [team]);

            var unparentedItems = new int[] { 12, 1337, 42 };
            

            workItemServiceMock.Setup(x => x.GetWorkItemsByAreaPath(release.WorkItemTypes, release.SearchTerm, It.IsAny<ITeamConfiguration>())).Returns(Task.FromResult(new List<int>()));
            workItemServiceMock.Setup(x => x.GetRemainingRelatedWorkItems(It.IsAny<int>(), It.IsAny<ITeamConfiguration>())).Returns(Task.FromResult(0));

            workItemServiceMock.Setup(x => x.GetWorkItemsByAreaPath(team.TeamConfiguration.WorkItemTypes, release.SearchTerm, team.TeamConfiguration)).Returns(Task.FromResult(new List<int>(unparentedItems)));

            var features = await subject.CollectFeaturesForReleases([release]);

            var actualFeature = features.Single();
            Assert.That(actualFeature.RemainingWork[team], Is.EqualTo(unparentedItems.Length));
        }

        private Team CreateTeam()
        {
            var team = new Team("InvolvedTeam", 1);
            team.UpdateTeamConfiguration(new AzureDevOpsTeamConfiguration());

            team.TeamConfiguration.WorkItemTypes.Add("User Story");

            return team;
        }

        private ReleaseConfiguration CreateReleaseConfiguration(SearchBy searchBy, params Team[] teams)
        {
            var releaseConfig = new ReleaseConfiguration
            {
                Name = "Release 1",
                SearchBy = searchBy,
                SearchTerm = "Release 1.33.7",
            };

            releaseConfig.WorkItemTypes.Add("Feature");

            foreach (var team in teams)
            {
                releaseConfig.InvolvedTeams.Add(team);
            }

            return releaseConfig;
        }
    }
}
