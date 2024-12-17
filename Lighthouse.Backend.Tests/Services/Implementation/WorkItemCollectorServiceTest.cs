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

        private WorkItemCollectorService subject;

        [SetUp]
        public void SetUp()
        {
            workItemServiceMock = new Mock<IWorkItemService>();
            featureRepositoryMock = new Mock<IRepository<Feature>>();

            var workItemServiceFactoryMock = new Mock<IWorkItemServiceFactory>();
            workItemServiceFactoryMock.Setup(x => x.GetWorkItemServiceForWorkTrackingSystem(It.IsAny<WorkTrackingSystems>())).Returns(workItemServiceMock.Object);

            workItemServiceMock.Setup(x => x.GetOpenWorkItems(It.IsAny<IEnumerable<string>>(), It.IsAny<Team>())).Returns(Task.FromResult(new List<string>()));

            subject = new WorkItemCollectorService(workItemServiceFactoryMock.Object, featureRepositoryMock.Object, Mock.Of<ILogger<WorkItemCollectorService>>());
        }

        [Test]
        public async Task CollectFeaturesForProject_SingleTeamInvolved_FindsFeauture()
        {
            var team = CreateTeam();

            var project = CreateProject(team);
            var feature = new Feature(team, 12) { ReferenceId = "12" };

            workItemServiceMock.Setup(x => x.GetOpenWorkItems(project.WorkItemTypes, It.IsAny<IWorkItemQueryOwner>())).Returns(Task.FromResult(new List<string> { feature.ReferenceId }));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult((12, 12)));

            await subject.UpdateFeaturesForProject(project);

            Assert.Multiple(() =>
            {
                Assert.That(project.Features.ToList(), Has.Count.EqualTo(1));

                var actualFeature = project.Features.Single();
                Assert.That(actualFeature.ReferenceId, Is.EqualTo(feature.ReferenceId));
                Assert.That(feature.IsUsingDefaultFeatureSize, Is.False);
            });
        }

        [Test]
        public async Task CollectFeaturesForProject_GivenExistingFeatures_ClearsExistingFeatures()
        {
            var team = CreateTeam();
            var project = CreateProject(team);
            var existingFeature = new Feature(team, 12) { Id = 12 };

            project.Features.Add(existingFeature);

            workItemServiceMock.Setup(x => x.GetOpenWorkItems(project.WorkItemTypes, It.IsAny<IWorkItemQueryOwner>())).Returns(Task.FromResult(new List<string>()));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(existingFeature.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult((12, 12)));

            await subject.UpdateFeaturesForProject(project);

            Assert.That(project.Features, Is.Empty);
        }

        [Test]
        [TestCase("Analysis in Progress", 2, 42, 42)]
        [TestCase("Analysis Done", 7, 42, 7)]
        public async Task CollectFeaturesForProject_HasRemainingWork_InStateToOverrideRealWork_UsesDefaultItems(string featureState, int remainingWork, int defaultWork, int expectedWork)
        {
            var team = CreateTeam();
            var project = CreateProject(team);

            project.DefaultAmountOfWorkItemsPerFeature = defaultWork;
            project.OverrideRealChildCountStates.Add("Analysis in Progress");

            var feature1 = new Feature(team, 2) { ReferenceId = "12", State = featureState };
            var feature2 = new Feature(team, 2) { ReferenceId = "1337" };

            workItemServiceMock.Setup(x => x.GetOpenWorkItems(project.WorkItemTypes, It.IsAny<IWorkItemQueryOwner>())).Returns(Task.FromResult(new List<string> { feature1.ReferenceId, feature2.ReferenceId }));
            workItemServiceMock.Setup(x => x.GetWorkItemDetails("12", project)).ReturnsAsync((feature1.Name, feature1.Order, feature1.Url ?? string.Empty, feature1.State));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature1.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult((remainingWork, 20)));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature2.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult((2, 12)));

            await subject.UpdateFeaturesForProject(project);

            Assert.Multiple(() =>
            {
                Assert.That(project.Features, Has.Count.EqualTo(2));

                var feature = project.Features.First();

                var isDefault = defaultWork == expectedWork;
                Assert.That(feature.IsUsingDefaultFeatureSize, Is.EqualTo(isDefault));
                Assert.That(feature.FeatureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(expectedWork));
            });
        }

        [Test]
        public async Task CollectFeaturesForProject_HasRemainingWork_InStateToOverrideRealWork_ItemWasMovedBack_UsesDefaultItems()
        {
            var team = CreateTeam();
            var project = CreateProject(team);

            project.DefaultAmountOfWorkItemsPerFeature = 7;
            project.OverrideRealChildCountStates.Add("Analysis in Progress");

            var feature1 = new Feature(team, 2) { ReferenceId = "12", State = "Analysis in Progress" };
            var feature2 = new Feature(team, 2) { ReferenceId = "1337" };

            var features = new List<Feature>() { feature1, feature2 };

            workItemServiceMock.Setup(x => x.GetOpenWorkItems(project.WorkItemTypes, It.IsAny<IWorkItemQueryOwner>())).Returns(Task.FromResult(new List<string> { feature1.ReferenceId, feature2.ReferenceId }));
            workItemServiceMock.Setup(x => x.GetWorkItemDetails("12", project)).ReturnsAsync((feature1.Name, feature1.Order, feature1.Url ?? string.Empty, feature1.State));

            featureRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<Feature, bool>>())).Returns((Func<Feature, bool> predicate) => features.SingleOrDefault(predicate));

            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature1.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult((10, 20)));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature2.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult((2, 12)));

            await subject.UpdateFeaturesForProject(project);

            Assert.Multiple(() =>
            {
                Assert.That(project.Features, Has.Count.EqualTo(2));

                var feature = project.Features.Single(f => f.ReferenceId == "12");
                Assert.That(feature.IsUsingDefaultFeatureSize, Is.True);
                Assert.That(feature.FeatureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(7));
            });
        }

        [Test]
        public async Task CollectFeaturesForProject_NoRemainingWork_NoTotalWork_AddsDefaultRemainingWorkToFeature()
        {
            var team = CreateTeam([1]);
            var project = CreateProject(team);
            project.DefaultAmountOfWorkItemsPerFeature = 12;

            var feature1 = new Feature(team, 0) { ReferenceId = "42" };
            var feature2 = new Feature(team, 2) { ReferenceId = "12" };

            workItemServiceMock.Setup(x => x.GetOpenWorkItems(project.WorkItemTypes, It.IsAny<IWorkItemQueryOwner>())).Returns(Task.FromResult(new List<string> { feature1.ReferenceId, feature2.ReferenceId }));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature1.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult((0, 0)));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature2.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult((2, 12)));

            await subject.UpdateFeaturesForProject(project);

            Assert.Multiple(() =>
            {
                Assert.That(project.Features, Has.Count.EqualTo(2));

                var feature = project.Features.First();
                Assert.That(feature.FeatureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(12));
                Assert.That(feature.IsUsingDefaultFeatureSize, Is.True);
            });
        }

        [Test]
        [TestCase(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, 80, 8)]
        [TestCase(new[] { 2, 4, 10, 3, 4, 5, 9, 7, 8, 7 }, 80, 8)]
        [TestCase(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, 120, 10)]
        [TestCase(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, 0, 1)]
        [TestCase(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, 1, 1)]
        [TestCase(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, 65, 6)]
        [TestCase(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, 85, 8)]
        public async Task CollectFeaturesForProject_UseCalculatedDefault_AddsDefaultRemainingWorkBasedOnPercentileToFeature(int[] childItemCount, int percentile, int expectedValue)
        {
            var team = CreateTeam([1]);
            var project = CreateProject(team);
            project.UsePercentileToCalculateDefaultAmountOfWorkItems = true;
            project.HistoricalFeaturesWorkItemQuery = "[System.Tags] CONTAINS 'This Team'";
            project.DefaultWorkItemPercentile = percentile;
            project.DefaultAmountOfWorkItemsPerFeature = 12;

            var feature1 = new Feature(team, 0) { ReferenceId = "42" };
            var feature2 = new Feature(team, 2) { ReferenceId = "12" };

            workItemServiceMock.Setup(x => x.GetOpenWorkItems(project.WorkItemTypes, It.IsAny<IWorkItemQueryOwner>())).Returns(Task.FromResult(new List<string> { feature1.ReferenceId, feature2.ReferenceId }));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature1.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult((0, 0)));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature2.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult((2, 12)));
            workItemServiceMock.Setup(x => x.GetChildItemsForFeaturesInProject(project)).ReturnsAsync(childItemCount);

            await subject.UpdateFeaturesForProject(project);

            Assert.Multiple(() =>
            {
                var feature = project.Features.First();
                Assert.That(feature.FeatureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(expectedValue));
                Assert.That(feature.IsUsingDefaultFeatureSize, Is.True);
            });
        }

        [Test]
        public async Task CollectFeaturesForProject_UseCalculatedDefault_QueryHasNoMatches_AddsDefaultRemainingWork()
        {
            var team = CreateTeam([1]);
            var project = CreateProject(team);

            project.UsePercentileToCalculateDefaultAmountOfWorkItems = true;
            project.HistoricalFeaturesWorkItemQuery = "[System.Tags] CONTAINS 'This Team'";
            project.DefaultWorkItemPercentile = 80;
            project.DefaultAmountOfWorkItemsPerFeature = 12;

            var feature1 = new Feature(team, 0) { ReferenceId = "42" };
            var feature2 = new Feature(team, 2) { ReferenceId = "12" };

            workItemServiceMock.Setup(x => x.GetOpenWorkItems(project.WorkItemTypes, It.IsAny<IWorkItemQueryOwner>())).Returns(Task.FromResult(new List<string> { feature1.ReferenceId, feature2.ReferenceId }));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature1.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult((0, 0)));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature2.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult((2, 12)));
            workItemServiceMock.Setup(x => x.GetChildItemsForFeaturesInProject(project)).ReturnsAsync(new List<int>());

            await subject.UpdateFeaturesForProject(project);

            Assert.Multiple(() =>
            {
                var feature = project.Features.First();
                Assert.That(feature.FeatureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(12));
                Assert.That(feature.IsUsingDefaultFeatureSize, Is.True);
            });
        }

        [Test]
        public async Task CollectFeaturesForProject_NoRemainingWork_NotTotalWork_SizeEstimateFieldSet_SizeEstimateNotAvailable_AddsDefaultRemainingWorkToFeature()
        {
            var team = CreateTeam([1]);
            var project = CreateProject(team);
            project.DefaultAmountOfWorkItemsPerFeature = 12;
            project.SizeEstimateField = "customfield_10037";

            var feature1 = new Feature(team, 0) { ReferenceId = "42" };
            var feature2 = new Feature(team, 2) { ReferenceId = "12" };

            workItemServiceMock.Setup(x => x.GetOpenWorkItems(project.WorkItemTypes, It.IsAny<IWorkItemQueryOwner>())).Returns(Task.FromResult(new List<string> { feature1.ReferenceId, feature2.ReferenceId }));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature1.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult((0, 0)));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature2.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult((2, 12)));

            workItemServiceMock.Setup(x => x.GetEstimatedSizeForItem(feature1.ReferenceId, project)).Returns(Task.FromResult(0));

            await subject.UpdateFeaturesForProject(project);

            Assert.That(project.Features, Has.Count.EqualTo(2));
            Assert.That(project.Features.First().FeatureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(12));
        }

        [Test]
        public async Task CollectFeaturesForProject_NoRemainingWork_NoTotalWork_SizeEstimateFieldSet_SizeEstimateAvailable_AddsEstimatedWorkToFeature()
        {
            var team = CreateTeam([1]);
            var project = CreateProject(team);
            project.DefaultAmountOfWorkItemsPerFeature = 12;
            project.SizeEstimateField = "customfield_10037";

            var feature1 = new Feature(team, 0) { ReferenceId = "42" };
            var feature2 = new Feature(team, 2) { ReferenceId = "12" };

            workItemServiceMock.Setup(x => x.GetOpenWorkItems(project.WorkItemTypes, It.IsAny<IWorkItemQueryOwner>())).Returns(Task.FromResult(new List<string> { feature1.ReferenceId, feature2.ReferenceId }));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature1.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult((0, 0)));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature2.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult((2, 12)));

            workItemServiceMock.Setup(x => x.GetEstimatedSizeForItem(feature1.ReferenceId, project)).Returns(Task.FromResult(7));

            await subject.UpdateFeaturesForProject(project);

            Assert.That(project.Features, Has.Count.EqualTo(2));
            Assert.That(project.Features.First().FeatureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(7));
        }

        [Test]
        public async Task CollectFeaturesForProject_NoRemainingWork_HasTotalWork_DoesNotAddDefaultRemainingWorkToFeature()
        {
            var team = CreateTeam([1]);
            var project = CreateProject(team);
            project.DefaultAmountOfWorkItemsPerFeature = 12;

            var feature1 = new Feature(team, 0) { ReferenceId = "42" };
            var feature2 = new Feature(team, 2) { ReferenceId = "12" };

            workItemServiceMock.Setup(x => x.GetOpenWorkItems(project.WorkItemTypes, It.IsAny<IWorkItemQueryOwner>())).Returns(Task.FromResult(new List<string> { feature1.ReferenceId, feature2.ReferenceId }));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature1.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult((0, 7)));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature2.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult((2, 12)));

            await subject.UpdateFeaturesForProject(project);

            Assert.That(project.Features, Has.Count.EqualTo(2));
            Assert.That(project.Features.First().FeatureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(0));
        }

        [Test]
        public async Task CollectFeaturesForProject_NoTotalWork_MulitpleTeams_SplitsDefaultRemainingWorkByTeam()
        {
            var team1 = CreateTeam();
            var team2 = CreateTeam();
            var project = CreateProject(team1, team2);

            project.DefaultAmountOfWorkItemsPerFeature = 12;

            var feature1 = new Feature([(team1, 0, 12), (team2, 0, 10)]) { ReferenceId = "17" };
            var feature2 = new Feature([(team1, 2, 13), (team2, 2, 3)]) { ReferenceId = "19" };

            workItemServiceMock.Setup(x => x.GetOpenWorkItems(project.WorkItemTypes, It.IsAny<IWorkItemQueryOwner>())).Returns(Task.FromResult(new List<string> { feature1.ReferenceId, feature2.ReferenceId }));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(feature1.ReferenceId, It.IsAny<Team>())).Returns(Task.FromResult((0, 0)));
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
            var project = CreateProject(team1, team2);

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
                Assert.That(project.Features.First().FeatureWork.First().RemainingWorkItems, Is.EqualTo(12));
                Assert.That(project.Features.First().FeatureWork.Last().RemainingWorkItems, Is.EqualTo(0));
            });
        }

        [Test]
        public async Task CollectFeaturesForProject_SingleTeamInvolved_FindsRemainingWorkByTeam()
        {
            var team = CreateTeam();
            var project = CreateProject(team);

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
            var project = CreateProject(team1, team2);

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

            var project = CreateProject(team1, team2);

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

            var project = CreateProject(team1, team2);

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
        [TestCase(new[] { "12", "1337", "42" }, new string[0], 3)]
        [TestCase(new[] { "12", "1337", "42" }, new string[] { "In Progress" }, 3)]
        public async Task CollectFeaturesForProject_UnparentedItems_CreatesDummyFeatureForUnparented(string[] unparentedItems, string[] overrideStates, int expectedItems)
        {
            var expectedUnparentedOrder = "123";
            var team = CreateTeam();
            var project = CreateProject(team);

            project.OverrideRealChildCountStates.AddRange(overrideStates);

            project.UnparentedItemsQuery = "[System.Tags] CONTAINS Release 123";

            workItemServiceMock.Setup(x => x.GetOpenWorkItems(project.WorkItemTypes, It.IsAny<IWorkItemQueryOwner>())).Returns(Task.FromResult(new List<string>()));
            workItemServiceMock.Setup(x => x.GetRelatedWorkItems(It.IsAny<string>(), It.IsAny<Team>())).Returns(Task.FromResult((0, 12)));

            workItemServiceMock.Setup(x => x.GetWorkItemsByQuery(team.WorkItemTypes, team, project.UnparentedItemsQuery)).Returns(Task.FromResult((new List<string>(unparentedItems), new List<string>(unparentedItems))));
            workItemServiceMock.Setup(x => x.GetAdjacentOrderIndex(It.IsAny<IEnumerable<string>>(), RelativeOrder.Above)).Returns(expectedUnparentedOrder);

            await subject.UpdateFeaturesForProject(project);

            var actualFeature = project.Features.Single();
            Assert.Multiple(() =>
            {
                Assert.That(actualFeature.Name, Is.EqualTo("Release 1 - Unparented"));
                Assert.That(actualFeature.GetRemainingWorkForTeam(team), Is.EqualTo(expectedItems));
                Assert.That(actualFeature.Order, Is.EqualTo(expectedUnparentedOrder));
            });
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

        private Project CreateProject(params Team[] teams)
        {
            var project = new Project
            {
                Name = "Release 1",
            };

            project.WorkItemTypes.Add("Feature");
            project.UpdateTeams(teams);

            var workTrackingConnection = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.Jira };
            project.WorkTrackingSystemConnection = workTrackingConnection;

            return project;
        }
    }
}
