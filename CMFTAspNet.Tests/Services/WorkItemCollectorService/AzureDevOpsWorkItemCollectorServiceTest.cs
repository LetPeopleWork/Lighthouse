using CMFTAspNet.Models;
using CMFTAspNet.Models.Teams;
using CMFTAspNet.Services.AzureDevOps;
using CMFTAspNet.Services.WorkItemCollectorService;
using Moq;

namespace CMFTAspNet.Tests.Services.WorkItemCollectorService
{
    public class AzureDevOpsWorkItemCollectorServiceTest
    {
        private Mock<IAzureDevOpsWorkItemService> azureDevOpsWorkItemServiceMock;
        private AzureDevOpsWorkItemCollectorService subject;

        [SetUp]
        public void SetUp()
        {
            azureDevOpsWorkItemServiceMock = new Mock<IAzureDevOpsWorkItemService>();
            subject = new AzureDevOpsWorkItemCollectorService(azureDevOpsWorkItemServiceMock.Object);
        }

        [Test]
        public async Task CollectFeaturesForRelease_SingleTeamInvolved_SearchByTag_FindsFeauture()
        {
            var team = CreateTeam();
            var release = CreateReleaseConfiguration(SearchBy.Tag, [team]);
            var feature = new Feature(team, 12) { Id = 12 };

            azureDevOpsWorkItemServiceMock.Setup(x => x.GetWorkItemsByTag(release.WorkItemType, release.SearchTerm, It.IsAny<AzureDevOpsTeamConfiguration>())).Returns(Task.FromResult(new List<int> { feature.Id }));
            azureDevOpsWorkItemServiceMock.Setup(x => x.GetRemainingRelatedWorkItems(It.IsAny<AzureDevOpsTeamConfiguration>(), feature.Id)).Returns(Task.FromResult(12));

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

            azureDevOpsWorkItemServiceMock.Setup(x => x.GetWorkItemsByAreaPath(release.WorkItemType, release.SearchTerm, It.IsAny<AzureDevOpsTeamConfiguration>())).Returns(Task.FromResult(new List<int> { feature.Id }));
            azureDevOpsWorkItemServiceMock.Setup(x => x.GetRemainingRelatedWorkItems(It.IsAny<AzureDevOpsTeamConfiguration>(), feature.Id)).Returns(Task.FromResult(12));

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

            azureDevOpsWorkItemServiceMock.Setup(x => x.GetWorkItemsByAreaPath(release.WorkItemType, release.SearchTerm, It.IsAny<AzureDevOpsTeamConfiguration>())).Returns(Task.FromResult(new List<int> { feature.Id }));
            azureDevOpsWorkItemServiceMock.Setup(x => x.GetRemainingRelatedWorkItems(It.IsAny<AzureDevOpsTeamConfiguration>(), feature.Id)).Returns(Task.FromResult(0));

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

            azureDevOpsWorkItemServiceMock.Setup(x => x.GetWorkItemsByAreaPath(release.WorkItemType, release.SearchTerm, It.IsAny<AzureDevOpsTeamConfiguration>())).Returns(Task.FromResult(new List<int> { feature.Id }));
            azureDevOpsWorkItemServiceMock.Setup(x => x.GetRemainingRelatedWorkItems(It.IsAny<AzureDevOpsTeamConfiguration>(), feature.Id)).Returns(Task.FromResult(remainingWorkItems));

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

            azureDevOpsWorkItemServiceMock.Setup(x => x.GetWorkItemsByTag(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AzureDevOpsTeamConfiguration>())).Returns(Task.FromResult(new List<int>()));

            _ = await subject.CollectFeaturesForReleases([release]);

            azureDevOpsWorkItemServiceMock.Verify(x => x.GetWorkItemsByTag(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AzureDevOpsTeamConfiguration>()), Times.Exactly(2));
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

            azureDevOpsWorkItemServiceMock.Setup(x => x.GetWorkItemsByAreaPath(release.WorkItemType, release.SearchTerm, (AzureDevOpsTeamConfiguration)team1.TeamConfiguration)).Returns(Task.FromResult(new List<int> { feature1.Id }));
            azureDevOpsWorkItemServiceMock.Setup(x => x.GetRemainingRelatedWorkItems((AzureDevOpsTeamConfiguration)team1.TeamConfiguration, feature1.Id)).Returns(Task.FromResult(remainingWorkItemsFeature1));

            azureDevOpsWorkItemServiceMock.Setup(x => x.GetWorkItemsByAreaPath(release.WorkItemType, release.SearchTerm, (AzureDevOpsTeamConfiguration)team2.TeamConfiguration)).Returns(Task.FromResult(new List<int> { feature2.Id }));
            azureDevOpsWorkItemServiceMock.Setup(x => x.GetRemainingRelatedWorkItems((AzureDevOpsTeamConfiguration)team2.TeamConfiguration, feature2.Id)).Returns(Task.FromResult(remainingWorkItemsFeature2));

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

            azureDevOpsWorkItemServiceMock.Setup(x => x.GetWorkItemsByAreaPath(release.WorkItemType, release.SearchTerm, It.IsAny<AzureDevOpsTeamConfiguration>())).Returns(Task.FromResult(new List<int> { feature.Id }));
            azureDevOpsWorkItemServiceMock.Setup(x => x.GetRemainingRelatedWorkItems((AzureDevOpsTeamConfiguration)team1.TeamConfiguration, feature.Id)).Returns(Task.FromResult(remainingWorkItemsTeam1));
            azureDevOpsWorkItemServiceMock.Setup(x => x.GetRemainingRelatedWorkItems((AzureDevOpsTeamConfiguration)team2.TeamConfiguration, feature.Id)).Returns(Task.FromResult(remainingWorkItemsTeam2));

            var features = await subject.CollectFeaturesForReleases([release]);

            var actualFeature = features.Single();
            Assert.Multiple(() =>
            {
                Assert.That(actualFeature.RemainingWork[team1], Is.EqualTo(remainingWorkItemsTeam1));
                Assert.That(actualFeature.RemainingWork[team2], Is.EqualTo(remainingWorkItemsTeam2));
            });
        }

        private Team CreateTeam()
        {
            var team = new Team("InvolvedTeam", 1);
            team.UpdateTeamConfiguration(new AzureDevOpsTeamConfiguration());
            return team;
        }

        private ReleaseConfiguration CreateReleaseConfiguration(SearchBy searchBy, params Team[] teams)
        {
            var releaseConfig = new ReleaseConfiguration
            {
                Name = "Release 1",
                SearchBy = searchBy,
                SearchTerm = "Release 1.33.7",
                WorkItemType = "Feature"
            };

            foreach (var team in teams)
            {
                releaseConfig.InvolvedTeams.Add(team);
            }

            return releaseConfig;
        }
    }
}
