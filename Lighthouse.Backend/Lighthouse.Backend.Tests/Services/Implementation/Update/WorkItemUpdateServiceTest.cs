using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.Update;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Tests.TestHelpers;
using Lighthouse.Backend.WorkTracking;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;

namespace Lighthouse.Backend.Tests.Services.Implementation.Update
{
    public class WorkItemUpdateServiceTest : UpdateServiceTestBase
    {
        private Mock<IRepository<Feature>> featureRepositoryMock;
        private Mock<IRepository<Project>> projectRepoMock;
        private Mock<IRepository<WorkItem>> workItemRepositoryMock;
        private Mock<IWorkItemService> workItemServiceMock;
        private Mock<IAppSettingService> appSettingServiceMock;
        private Mock<IForecastUpdateService> forecastUpdateServiceMock;

        private int idCounter = 0;
        private List<WorkItem> workItems;

        [SetUp]
        public void SetUp()
        {
            workItemServiceMock = new Mock<IWorkItemService>();
            featureRepositoryMock = new Mock<IRepository<Feature>>();
            projectRepoMock = new Mock<IRepository<Project>>();
            workItemRepositoryMock = new Mock<IRepository<WorkItem>>();
            appSettingServiceMock = new Mock<IAppSettingService>();
            forecastUpdateServiceMock = new Mock<IForecastUpdateService>();

            var workItemServiceFactoryMock = new Mock<IWorkItemServiceFactory>();
            workItemServiceFactoryMock.Setup(x => x.GetWorkItemServiceForWorkTrackingSystem(It.IsAny<WorkTrackingSystems>())).Returns(workItemServiceMock.Object);
            workItemServiceMock.Setup(x => x.GetFeaturesForProject(It.IsAny<Project>())).Returns(Task.FromResult(new List<Feature>()));

            SetupServiceProviderMock(projectRepoMock.Object);
            SetupServiceProviderMock(featureRepositoryMock.Object);
            SetupServiceProviderMock(workItemRepositoryMock.Object);
            SetupServiceProviderMock(appSettingServiceMock.Object);
            SetupServiceProviderMock(workItemServiceFactoryMock.Object);
            SetupServiceProviderMock(forecastUpdateServiceMock.Object);

            SetupRefreshSettings(10, 10);

            workItems = new List<WorkItem>();
            workItemRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<WorkItem, bool>>>()))
            .Returns((Expression<Func<WorkItem, bool>> predicate) => workItems.Where(predicate.Compile()).AsQueryable());
            workItemRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<WorkItem, bool>>()))
            .Returns((Func<WorkItem, bool> predicate) => workItems.SingleOrDefault(predicate));
        }

        [Test]
        public void UpdateFeaturesForProject_SingleTeamInvolved_FindsFeauture()
        {
            var team = CreateTeam();

            var project = CreateProject(team);
            var feature = new Feature(team, 12) { ReferenceId = "12" };
            SetupProjects(project);
            SetupWorkForFeature(feature, 1, 0, team);

            workItemServiceMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature }));

            var subject = CreateSubject();
            subject.TriggerUpdate(project.Id);

            Assert.Multiple(() =>
            {
                Assert.That(project.Features.ToList(), Has.Count.EqualTo(1));

                var actualFeature = project.Features.Single();
                Assert.That(actualFeature.ReferenceId, Is.EqualTo(feature.ReferenceId));
                Assert.That(feature.IsUsingDefaultFeatureSize, Is.False);
            });
        }

        [Test]
        public void UpdatesFeatures_TriggersReforecast()
        {
            var team = CreateTeam();

            var project = CreateProject(team);
            var feature = new Feature(team, 12) { ReferenceId = "12" };
            SetupProjects(project);

            workItemServiceMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature }));

            var subject = CreateSubject();
            subject.TriggerUpdate(project.Id);

            forecastUpdateServiceMock.Verify(x => x.UpdateForecastsForProject(project.Id));
        }

        [Test]
        public void UpdateFeaturesForProject_GivenExistingFeatures_ClearsExistingFeatures()
        {
            var team = CreateTeam();
            var project = CreateProject(team);
            SetupProjects(project);
            var existingFeature = new Feature(team, 12) { Id = 12 };

            project.Features.Add(existingFeature);

            workItemServiceMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature>()));

            var subject = CreateSubject();
            subject.TriggerUpdate(project.Id);

            Assert.That(project.Features, Is.Empty);
        }

        [Test]
        [TestCase("Analysis in Progress", 2, 42, 42)]
        [TestCase("Analysis Done", 7, 42, 7)]
        public void UpdateFeaturesForProject_HasRemainingWork_InStateToOverrideRealWork_UsesDefaultItems(string featureState, int remainingWork, int defaultWork, int expectedWork)
        {
            var team = CreateTeam();
            var project = CreateProject(team);
            SetupProjects(project);

            project.DefaultAmountOfWorkItemsPerFeature = defaultWork;
            project.OverrideRealChildCountStates.Add("Analysis in Progress");

            var feature1 = new Feature(team, 2) { ReferenceId = "12", State = featureState };
            var feature2 = new Feature(team, 2) { ReferenceId = "1337" };

            SetupWorkForFeature(feature1, 10, remainingWork, team);
            SetupWorkForFeature(feature2, 15, 12, team);

            workItemServiceMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));

            var subject = CreateSubject();
            subject.TriggerUpdate(project.Id);

            Assert.Multiple(() =>
            {
                Assert.That(project.Features, Has.Count.EqualTo(2));

                var feature = project.Features[0];

                var isDefault = defaultWork == expectedWork;
                Assert.That(feature.IsUsingDefaultFeatureSize, Is.EqualTo(isDefault));
                Assert.That(feature.FeatureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(expectedWork));
            });
        }

        [Test]
        [TestCase(10, 20, 10)]
        [TestCase(0, 10, 0)]
        [TestCase(10, 10, 10)]
        [TestCase(0, 0, 0)]
        public void UpdateFeaturesForProject_IsInDoneState_DoesNotUsesDefaultItems(int remainingWork, int totalWork, int expectedWork)
        {
            var team = CreateTeam();
            var project = CreateProject(team);
            SetupProjects(project);

            project.DoneStates.Clear();
            project.DoneStates.Add("Done");

            project.DefaultAmountOfWorkItemsPerFeature = 42;

            var feature = new Feature(team, remainingWork) { ReferenceId = "12", State = "Done", StateCategory = StateCategories.Done };
            SetupWorkForFeature(feature, totalWork, remainingWork, team);

            workItemServiceMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature }));

            var subject = CreateSubject();
            subject.TriggerUpdate(project.Id);

            Assert.Multiple(() =>
            {
                Assert.That(project.Features, Has.Count.EqualTo(1));

                var feature = project.Features[0];

                Assert.That(feature.IsUsingDefaultFeatureSize, Is.False);
                Assert.That(feature.FeatureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(expectedWork));
            });
        }

        [Test]
        public void UpdateFeaturesForProject_HasRemainingWork_InStateToOverrideRealWork_ItemWasMovedBack_UsesDefaultItems()
        {
            var team = CreateTeam();
            var project = CreateProject(team);
            SetupProjects(project);

            project.DefaultAmountOfWorkItemsPerFeature = 7;
            project.OverrideRealChildCountStates.Add("Analysis in Progress");

            var feature1 = new Feature(team, 2) { ReferenceId = "12", State = "Analysis in Progress" };
            var feature2 = new Feature(team, 2) { ReferenceId = "1337" };

            var features = new List<Feature>() { feature1, feature2 };

            workItemServiceMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));

            featureRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<Feature, bool>>())).Returns((Func<Feature, bool> predicate) => features.SingleOrDefault(predicate));

            var subject = CreateSubject();
            subject.TriggerUpdate(project.Id);

            Assert.Multiple(() =>
            {
                Assert.That(project.Features, Has.Count.EqualTo(2));

                var feature = project.Features.Single(f => f.ReferenceId == "12");
                Assert.That(feature.IsUsingDefaultFeatureSize, Is.True);
                Assert.That(feature.FeatureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(7));
            });
        }

        [Test]
        public void UpdateFeaturesForProject_NoRemainingWork_NoTotalWork_AddsDefaultRemainingWorkToFeature()
        {
            var team = CreateTeam();
            var project = CreateProject(team);
            project.DefaultAmountOfWorkItemsPerFeature = 12;
            SetupProjects(project);

            var feature1 = new Feature(team, 0) { ReferenceId = "42" };
            var feature2 = new Feature(team, 2) { ReferenceId = "12" };

            workItemServiceMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));

            var subject = CreateSubject();
            subject.TriggerUpdate(project.Id);

            Assert.Multiple(() =>
            {
                Assert.That(project.Features, Has.Count.EqualTo(2));

                var feature = project.Features[0];
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
        public void UpdateFeaturesForProject_UseCalculatedDefault_AddsDefaultRemainingWorkBasedOnPercentileToFeature(int[] childItemCount, int percentile, int expectedValue)
        {
            var team = CreateTeam();
            var project = CreateProject(team);
            SetupProjects(project);

            project.UsePercentileToCalculateDefaultAmountOfWorkItems = true;
            project.HistoricalFeaturesWorkItemQuery = "[System.Tags] CONTAINS 'This Team'";
            project.DefaultWorkItemPercentile = percentile;
            project.DefaultAmountOfWorkItemsPerFeature = 12;

            var feature1 = new Feature(team, 0) { ReferenceId = "42" };
            var feature2 = new Feature(team, 2) { ReferenceId = "12" };

            workItemServiceMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));

            var itemCounter = 0;
            var historicalFeatureSize = childItemCount.ToDictionary(x => $"{itemCounter++}", x => x);
            workItemServiceMock.Setup(x => x.GetHistoricalFeatureSize(project)).ReturnsAsync(historicalFeatureSize);

            var subject = CreateSubject();
            subject.TriggerUpdate(project.Id);

            Assert.Multiple(() =>
            {
                var feature = project.Features[0];
                Assert.That(feature.FeatureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(expectedValue));
                Assert.That(feature.IsUsingDefaultFeatureSize, Is.True);
            });
        }

        [Test]
        public void UpdateFeaturesForProject_UseCalculatedDefault_QueryHasNoMatches_AddsDefaultRemainingWork()
        {
            var team = CreateTeam();
            var project = CreateProject(team);

            project.UsePercentileToCalculateDefaultAmountOfWorkItems = true;
            project.HistoricalFeaturesWorkItemQuery = "[System.Tags] CONTAINS 'This Team'";
            project.DefaultWorkItemPercentile = 80;
            project.DefaultAmountOfWorkItemsPerFeature = 12;
            SetupProjects(project);

            var feature1 = new Feature(team, 0) { ReferenceId = "42" };
            var feature2 = new Feature(team, 2) { ReferenceId = "12" };

            workItemServiceMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));
            workItemServiceMock.Setup(x => x.GetHistoricalFeatureSize(project)).ReturnsAsync(new Dictionary<string, int>());

            var subject = CreateSubject();
            subject.TriggerUpdate(project.Id);

            Assert.Multiple(() =>
            {
                var feature = project.Features[0];
                Assert.That(feature.FeatureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(12));
                Assert.That(feature.IsUsingDefaultFeatureSize, Is.True);
            });
        }

        [Test]
        public void UpdateFeaturesForProject_NoRemainingWork_NotTotalWork_SizeEstimateFieldSet_SizeEstimateNotAvailable_AddsDefaultRemainingWorkToFeature()
        {
            var team = CreateTeam();
            var project = CreateProject(team);
            project.DefaultAmountOfWorkItemsPerFeature = 12;
            project.SizeEstimateField = "customfield_10037";
            SetupProjects(project);

            var feature1 = new Feature(team, 0) { ReferenceId = "42", EstimatedSize = 0 };
            var feature2 = new Feature(team, 2) { ReferenceId = "12" };

            workItemServiceMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));

            var subject = CreateSubject();
            subject.TriggerUpdate(project.Id);

            Assert.That(project.Features, Has.Count.EqualTo(2));
            Assert.That(project.Features[0].FeatureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(12));
        }

        [Test]
        public void UpdateFeaturesForProject_NoRemainingWork_NoTotalWork_SizeEstimateFieldSet_SizeEstimateAvailable_AddsEstimatedWorkToFeature()
        {
            var team = CreateTeam();
            var project = CreateProject(team);
            project.DefaultAmountOfWorkItemsPerFeature = 12;
            project.SizeEstimateField = "customfield_10037";
            SetupProjects(project);

            var feature1 = new Feature(team, 0) { ReferenceId = "42", EstimatedSize = 7 };
            var feature2 = new Feature(team, 2) { ReferenceId = "12" };

            workItemServiceMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));

            var subject = CreateSubject();
            subject.TriggerUpdate(project.Id);

            Assert.That(project.Features, Has.Count.EqualTo(2));
            Assert.That(project.Features[0].FeatureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(7));
        }

        [Test]
        public void UpdateFeaturesForProject_NoRemainingWork_HasTotalWork_DoesNotAddDefaultRemainingWorkToFeature()
        {
            var team = CreateTeam();
            var project = CreateProject(team);
            project.DefaultAmountOfWorkItemsPerFeature = 12;
            SetupProjects(project);

            var feature1 = new Feature(team, 0) { ReferenceId = "42" };
            var feature2 = new Feature(team, 2) { ReferenceId = "12" };

            workItemServiceMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));

            SetupWorkForFeature(feature1, 7, 0, team);

            var subject = CreateSubject();
            subject.TriggerUpdate(project.Id);

            Assert.That(project.Features, Has.Count.EqualTo(2));
            Assert.That(project.Features[0].FeatureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(0));
        }

        [Test]
        public void UpdateFeaturesForProject_NoTotalWork_MulitpleTeams_SplitsDefaultRemainingWorkByTeam()
        {
            var team1 = CreateTeam();
            var team2 = CreateTeam();
            var project = CreateProject(team1, team2);
            SetupProjects(project);

            project.DefaultAmountOfWorkItemsPerFeature = 12;

            var feature1 = new Feature([(team1, 0, 12), (team2, 0, 10)]) { ReferenceId = "17" };
            var feature2 = new Feature([(team1, 2, 13), (team2, 2, 3)]) { ReferenceId = "19" };

            workItemServiceMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));
            SetupWorkForFeature(feature1, 0, 0);
            SetupWorkForFeature(feature2, 12, 12);

            var subject = CreateSubject();
            subject.TriggerUpdate(project.Id);

            Assert.Multiple(() =>
            {
                Assert.That(project.Features, Has.Count.EqualTo(2));

                var featureWork = project.Features[0].FeatureWork;
                Assert.That(featureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(12));
                Assert.That(featureWork[featureWork.Count - 1].RemainingWorkItems, Is.EqualTo(6));
                Assert.That(featureWork[featureWork.Count - 1].RemainingWorkItems, Is.EqualTo(6));
            });
        }

        [Test]
        public void UpdateFeaturesForProject_SingleTeamInvolved_FindsRemainingWorkByTeam()
        {
            var team = CreateTeam();
            var project = CreateProject(team);
            SetupProjects(project);

            var remainingWorkItems = 12;
            var feature = new Feature(team, remainingWorkItems) { ReferenceId = "42" };

            workItemServiceMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature }));
            SetupWorkForFeature(feature, 12, remainingWorkItems, team);

            var subject = CreateSubject();
            subject.TriggerUpdate(project.Id);

            var actualFeature = project.Features.Single();
            Assert.That(actualFeature.GetRemainingWorkForTeam(team), Is.EqualTo(remainingWorkItems));
        }


        [Test]
        public void UpdateFeaturesForProject_MultipleTeamsInvolved()
        {
            var team1 = CreateTeam();
            var team2 = CreateTeam();
            var project = CreateProject(team1, team2);
            SetupProjects(project);

            workItemServiceMock.Setup(x => x.GetFeaturesForProject(It.IsAny<Project>())).Returns(Task.FromResult(new List<Feature>()));

            var subject = CreateSubject();
            subject.TriggerUpdate(project.Id);

            workItemServiceMock.Verify(x => x.GetFeaturesForProject(project), Times.Exactly(1));
        }

        [Test]
        public void UpdateFeaturesForProject_TwoTeamsInvolved_IndividualFeatures_FindsRemainingWorkByTeam()
        {
            var team1 = CreateTeam();
            var team2 = CreateTeam();

            var project = CreateProject(team1, team2);
            SetupProjects(project);

            var remainingWorkItemsFeature1 = 12;
            var remainingWorkItemsFeature2 = 7;
            var feature1 = new Feature(team1, remainingWorkItemsFeature1) { ReferenceId = "1" };
            var feature2 = new Feature(team2, remainingWorkItemsFeature1) { ReferenceId = "2" };

            workItemServiceMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));

            SetupWorkForFeature(feature1, 12, remainingWorkItemsFeature1, team1);
            SetupWorkForFeature(feature2, 12, remainingWorkItemsFeature2, team2);

            var subject = CreateSubject();
            subject.TriggerUpdate(project.Id);

            var actualFeature1 = project.Features[0];
            Assert.That(actualFeature1.GetRemainingWorkForTeam(team1), Is.EqualTo(remainingWorkItemsFeature1));
            var actualFeature2 = project.Features[project.Features.Count - 1];
            Assert.That(actualFeature2.GetRemainingWorkForTeam(team2), Is.EqualTo(remainingWorkItemsFeature2));
        }

        [Test]
        public void UpdateFeaturesForProject_TwoTeamsInvolved_SingleFeature_FindsRemainingWorkByTeam()
        {
            var team1 = CreateTeam();
            var team2 = CreateTeam();

            var project = CreateProject(team1, team2);
            SetupProjects(project);

            var remainingWorkItemsTeam1 = 12;
            var remainingWorkItemsTeam2 = 7;
            var feature = new Feature(team1, remainingWorkItemsTeam1) { ReferenceId = "1" };

            workItemServiceMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature }));

            SetupWorkForFeature(feature, 12, remainingWorkItemsTeam1, team1);
            SetupWorkForFeature(feature, 12, remainingWorkItemsTeam2, team2);

            var subject = CreateSubject();
            subject.TriggerUpdate(project.Id);

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
        public void UpdateFeaturesForProject_UnparentedItems_CreatesDummyFeatureForUnparented(string[] unparentedItems, string[] overrideStates, int expectedItems)
        {
            var expectedUnparentedOrder = "123";
            var team = CreateTeam();
            var project = CreateProject(team);
            SetupProjects(project);

            project.OverrideRealChildCountStates.AddRange(overrideStates);
            project.UnparentedItemsQuery = "[System.Tags] CONTAINS Release 123";

            workItemServiceMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature>()));

            foreach (var unparentedItem in unparentedItems)
            {
                SetupUnparentedWorkItems(unparentedItem, team);
            }

            workItemServiceMock.Setup(x => x.GetWorkItemsIdsForTeamWithAdditionalQuery(team, project.UnparentedItemsQuery))
                .Returns(Task.FromResult(new List<string>(unparentedItems)));

            workItemServiceMock.Setup(x => x.GetAdjacentOrderIndex(It.IsAny<IEnumerable<string>>(), RelativeOrder.Above)).Returns(expectedUnparentedOrder);

            var subject = CreateSubject();
            subject.TriggerUpdate(project.Id);

            var actualFeature = project.Features.Single();
            Assert.Multiple(() =>
            {
                Assert.That(actualFeature.Name, Is.EqualTo("Release 1 - Unparented"));
                Assert.That(actualFeature.GetRemainingWorkForTeam(team), Is.EqualTo(expectedItems));
                Assert.That(actualFeature.Order, Is.EqualTo(expectedUnparentedOrder));
                Assert.That(Guid.TryParse(actualFeature.ReferenceId, out _), Is.True, "ReferenceId should be a valid GUID");
            });
        }

        [Test]
        public void UpdateFeaturesForProject_UseDefaultWork_NoOwningTeam_SplitsWorkByAllInvolvedTeams()
        {
            var team1 = CreateTeam();
            var team2 = CreateTeam();

            var project = CreateProject(team1, team2);
            project.DefaultAmountOfWorkItemsPerFeature = 12;
            SetupProjects(project);

            var teams = new List<(Team team, int remainingItems, int totalItems)> { (team1, 0, 0), (team2, 0, 0) };
            var feature = new Feature(teams) { ReferenceId = "42" };

            workItemServiceMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature }));
            SetupWorkForFeature(feature, 0, 0);

            var subject = CreateSubject();
            subject.TriggerUpdate(project.Id);

            Assert.Multiple(() =>
            {
                var feature = project.Features.Single();

                Assert.That(feature.FeatureWork, Has.Count.EqualTo(2));
                Assert.That(feature.FeatureWork[0].TotalWorkItems, Is.EqualTo(6));
                Assert.That(feature.FeatureWork[feature.FeatureWork.Count - 1].TotalWorkItems, Is.EqualTo(6));
            });
        }

        [Test]
        public void UpdateFeaturesForProject_UseDefaultWork_OwningTeam_AssignsAllWorkToOwningTeam()
        {
            var team1 = CreateTeam();
            var team2 = CreateTeam();

            var project = CreateProject(team1, team2);
            project.DefaultAmountOfWorkItemsPerFeature = 12;
            project.OwningTeam = team2;
            SetupProjects(project);

            var teams = new List<(Team team, int remainingItems, int totalItems)> { (team1, 0, 0), (team2, 0, 0) };
            var feature = new Feature(teams) { ReferenceId = "42" };

            workItemServiceMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature }));
            SetupWorkForFeature(feature, 0, 0);

            var subject = CreateSubject();
            subject.TriggerUpdate(project.Id);

            Assert.Multiple(() =>
            {
                var feature = project.Features.Single();

                Assert.That(feature.FeatureWork, Has.Count.EqualTo(1));
                Assert.That(feature.FeatureWork[feature.FeatureWork.Count - 1].TotalWorkItems, Is.EqualTo(12));
                Assert.That(feature.FeatureWork[feature.FeatureWork.Count - 1].Team, Is.EqualTo(team2));
            });
        }

        [Test]
        public void UpdateFeaturesForProject_UseDefaultWork_NoOwningTeam_FeatureOwnerDefined_AssignsAllWorkToOwningTeam()
        {
            var team1 = CreateTeam();
            team1.Name = "Godzilla";

            var team2 = CreateTeam();
            team2.Name = "The Most Awesome";

            var project = CreateProject(team1, team2);
            project.DefaultAmountOfWorkItemsPerFeature = 12;
            project.OwningTeam = null;
            project.FeatureOwnerField = "System.AreaPath";
            SetupProjects(project);

            var teams = new List<(Team team, int remainingItems, int totalItems)> { (team1, 0, 0), (team2, 0, 0) };
            var feature = new Feature(teams) { ReferenceId = "42", OwningTeam = "Project\\The Most Awesome\\Features" };

            workItemServiceMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature }));
            SetupWorkForFeature(feature, 0, 0);

            var subject = CreateSubject();
            subject.TriggerUpdate(project.Id);

            Assert.Multiple(() =>
            {
                var feature = project.Features.Single();

                Assert.That(feature.FeatureWork, Has.Count.EqualTo(1));
                Assert.That(feature.FeatureWork[feature.FeatureWork.Count - 1].TotalWorkItems, Is.EqualTo(12));
                Assert.That(feature.FeatureWork[feature.FeatureWork.Count - 1].Team, Is.EqualTo(team2));
            });
        }

        [Test]
        public void UpdateFeaturesForProject_UseDefaultWork_OwningTeam_FeatureOwnerDefined_AssignsAllWorkToFeatureOwningTeam()
        {
            var team1 = CreateTeam();
            var team2 = CreateTeam();
            team2.Name = "The Most Awesome";

            var project = CreateProject(team1, team2);
            project.DefaultAmountOfWorkItemsPerFeature = 12;
            project.OwningTeam = team1;
            project.FeatureOwnerField = "System.AreaPath";
            SetupProjects(project);

            var teams = new List<(Team team, int remainingItems, int totalItems)> { (team1, 0, 0), (team2, 0, 0) };
            var feature = new Feature(teams) { ReferenceId = "42", OwningTeam = "Project\\The Most Awesome\\Features" };

            workItemServiceMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature }));
            SetupWorkForFeature(feature, 0, 0);

            var subject = CreateSubject();
            subject.TriggerUpdate(project.Id);

            Assert.Multiple(() =>
            {
                var feature = project.Features.Single();

                Assert.That(feature.FeatureWork, Has.Count.EqualTo(1));
                Assert.That(feature.FeatureWork[feature.FeatureWork.Count - 1].TotalWorkItems, Is.EqualTo(12));
                Assert.That(feature.FeatureWork[feature.FeatureWork.Count - 1].Team, Is.EqualTo(team2));
            });
        }

        [Test]
        public void UpdateFeaturesForProject_UseDefaultWork_NoOwningTeam_MultipleFeatureOwnerDefined_SplitsAllWorkAcrossFeatureOwningTeams()
        {
            var team1 = CreateTeam();
            var team2 = CreateTeam();
            team2.Name = "The Most Awesome";

            var team3 = CreateTeam();
            team3.Name = "Other";

            var project = CreateProject(team1, team2, team3);
            project.DefaultAmountOfWorkItemsPerFeature = 12;
            project.FeatureOwnerField = "System.Tags";
            SetupProjects(project);

            var teams = new List<(Team team, int remainingItems, int totalItems)> { (team1, 0, 0), (team2, 0, 0), (team3, 0, 0) };
            var feature = new Feature(teams) { ReferenceId = "42", OwningTeam = "The Most Awesome;Other" };
            SetupWorkForFeature(feature, 0, 0);

            workItemServiceMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature }));

            var subject = CreateSubject();
            subject.TriggerUpdate(project.Id);

            Assert.Multiple(() =>
            {
                var feature = project.Features.Single();

                Assert.That(feature.FeatureWork, Has.Count.EqualTo(2));
                Assert.That(feature.FeatureWork[0].TotalWorkItems, Is.EqualTo(6));
                Assert.That(feature.FeatureWork[0].Team, Is.EqualTo(team2));
                Assert.That(feature.FeatureWork[1].TotalWorkItems, Is.EqualTo(6));
                Assert.That(feature.FeatureWork[1].Team, Is.EqualTo(team3));
            });
        }

        [Test]
        public void UpdateFeaturesForProject_UseDefaultWork_OwningTeam_MultipleFeatureOwnerDefined_SplitsAllWorkAcrossFeatureOwningTeams()
        {
            var team1 = CreateTeam();
            var team2 = CreateTeam();
            team2.Name = "The Most Awesome";

            var team3 = CreateTeam();
            team3.Name = "Other";

            var project = CreateProject(team1, team2, team3);
            project.DefaultAmountOfWorkItemsPerFeature = 12;
            project.FeatureOwnerField = "System.Tags";
            project.OwningTeam = team1;
            SetupProjects(project);

            var teams = new List<(Team team, int remainingItems, int totalItems)> { (team1, 0, 0), (team2, 0, 0), (team3, 0, 0) };
            var feature = new Feature(teams) { ReferenceId = "42", OwningTeam = "The Most Awesome;Other" };
            SetupWorkForFeature(feature, 0, 0);

            workItemServiceMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature }));

            var subject = CreateSubject();
            subject.TriggerUpdate(project.Id);

            Assert.Multiple(() =>
            {
                var feature = project.Features.Single();

                Assert.That(feature.FeatureWork, Has.Count.EqualTo(2));
                Assert.That(feature.FeatureWork[0].TotalWorkItems, Is.EqualTo(6));
                Assert.That(feature.FeatureWork[0].Team, Is.EqualTo(team2));
                Assert.That(feature.FeatureWork[1].TotalWorkItems, Is.EqualTo(6));
                Assert.That(feature.FeatureWork[1].Team, Is.EqualTo(team3));
            });
        }

        [Test]
        public void UpdateFeaturesForProject_UseDefaultWork_OwningTeam_FeatureOwnerNotFound_AssignsWorkToOwningTeam()
        {
            var team1 = CreateTeam();
            var team2 = CreateTeam();
            team2.Name = "The Most Awesome";

            var team3 = CreateTeam();
            team3.Name = "Other";

            var project = CreateProject(team1, team2, team3);
            project.DefaultAmountOfWorkItemsPerFeature = 12;
            project.FeatureOwnerField = "System.Tags";
            project.OwningTeam = team1;
            SetupProjects(project);

            var teams = new List<(Team team, int remainingItems, int totalItems)> { (team1, 0, 0), (team2, 0, 0), (team3, 0, 0) };
            var feature = new Feature(teams) { ReferenceId = "42", OwningTeam = "Some Random String That does not contain a team name!" };
            SetupWorkForFeature(feature, 0, 0);

            workItemServiceMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature }));

            var subject = CreateSubject();
            subject.TriggerUpdate(project.Id);

            Assert.Multiple(() =>
            {
                var feature = project.Features.Single();

                Assert.That(feature.FeatureWork, Has.Count.EqualTo(1));
                Assert.That(feature.FeatureWork[0].TotalWorkItems, Is.EqualTo(12));
                Assert.That(feature.FeatureWork[0].Team, Is.EqualTo(team1));
            });
        }

        [Test]
        public void UpdateFeaturesForProject_UseDefaultWork_NoOwningTeam_FeatureOwnerNotFound_SplitsAcrossAllTeams()
        {
            var team1 = CreateTeam();
            var team2 = CreateTeam();
            team2.Name = "The Most Awesome";

            var team3 = CreateTeam();
            team3.Name = "Other";

            var project = CreateProject(team1, team2, team3);
            project.DefaultAmountOfWorkItemsPerFeature = 15;
            project.FeatureOwnerField = "System.Tags";
            SetupProjects(project);

            var teams = new List<(Team team, int remainingItems, int totalItems)> { (team1, 0, 0), (team2, 0, 0), (team3, 0, 0) };
            var feature = new Feature(teams) { ReferenceId = "42", OwningTeam = "Some Random String That does not contain a team name!" };
            SetupWorkForFeature(feature, 0, 0);

            workItemServiceMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature }));

            var subject = CreateSubject();
            subject.TriggerUpdate(project.Id);

            Assert.Multiple(() =>
            {
                var feature = project.Features.Single();

                Assert.That(feature.FeatureWork, Has.Count.EqualTo(3));

                foreach (var fw in feature.FeatureWork)
                {
                    Assert.That(fw.TotalWorkItems, Is.EqualTo(5));
                }
            });
        }

        [Test]
        public async Task ExecuteAsync_ReadyToRefresh_RefreshesAllProjectsAsync()
        {
            var project = CreateProject(DateTime.Now.AddDays(-1));
            SetupProjects(project);

            workItemServiceMock.Setup(x => x.GetFeaturesForProject(It.IsAny<Project>())).Returns(Task.FromResult(new List<Feature>()));

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            Mock.Get(UpdateQueueService).Verify(x => x.EnqueueUpdate(UpdateType.Features, project.Id, It.IsAny<Func<IServiceProvider, Task>>()));
            projectRepoMock.Verify(x => x.Save());
        }

        [Test]
        public async Task ExecuteAsync_MultipleProjects_RefreshesAllProjectsAsync()
        {
            var project1 = CreateProject(DateTime.Now.AddDays(-1));
            var project2 = CreateProject(DateTime.Now.AddDays(-1));
            SetupProjects(project1, project2);

            workItemServiceMock.Setup(x => x.GetFeaturesForProject(It.IsAny<Project>())).Returns(Task.FromResult(new List<Feature>()));

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            Mock.Get(UpdateQueueService).Verify(x => x.EnqueueUpdate(UpdateType.Features, project1.Id, It.IsAny<Func<IServiceProvider, Task>>()));
            Mock.Get(UpdateQueueService).Verify(x => x.EnqueueUpdate(UpdateType.Features, project2.Id, It.IsAny<Func<IServiceProvider, Task>>()));
            projectRepoMock.Verify(x => x.Save(), Times.Exactly(2));
        }

        [Test]
        public async Task ExecuteAsync_MultipleProjects_RefreshesOnlyProjectsWhereLastRefreshIsOlderThanConfiguredSetting()
        {
            var project1 = CreateProject(DateTime.Now.AddDays(-1));
            var project2 = CreateProject(DateTime.Now);

            workItemServiceMock.Setup(x => x.GetFeaturesForProject(It.IsAny<Project>())).Returns(Task.FromResult(new List<Feature>()));

            SetupRefreshSettings(10, 360);

            SetupProjects(project1, project2);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            Mock.Get(UpdateQueueService).Verify(x => x.EnqueueUpdate(UpdateType.Features, project1.Id, It.IsAny<Func<IServiceProvider, Task>>()));
            Mock.Get(UpdateQueueService).Verify(x => x.EnqueueUpdate(UpdateType.Features, project2.Id, It.IsAny<Func<IServiceProvider, Task>>()), Times.Never);
            projectRepoMock.Verify(x => x.Save(), Times.Exactly(1));
        }

        private void SetupUnparentedWorkItems(string referenceId, Team team)
        {
            var unparentedWorkItem = new WorkItem
            {
                Id = idCounter++,
                StateCategory = StateCategories.ToDo,
                State = "To Do",
                ReferenceId = referenceId,
                Team = team,
                TeamId = team.Id
            };

            workItems.AddRange(unparentedWorkItem);
        }

        private void SetupWorkForFeature(Feature feature, int doneItems, int toDoItems)
        {
            SetupWorkForFeature(feature, doneItems, toDoItems, null);
        }

        private void SetupWorkForFeature(Feature feature, int totalItems, int remainingItems, Team? team)
        {
            var newWorkItems = new List<WorkItem>();

            for (var itemCount = 0; itemCount < totalItems; itemCount++)
            {
                var id = Guid.NewGuid().ToString();
                var workItem = new WorkItem
                {
                    Id = idCounter++,
                    StateCategory = StateCategories.Done,
                    ReferenceId = id,
                    ParentReferenceId = feature.ReferenceId,
                    Team = team,
                    TeamId = team?.Id ?? -1,
                };
                newWorkItems.Add(workItem);
            }

            newWorkItems.Take(remainingItems).ToList().ForEach(x => x.StateCategory = StateCategories.Doing);

            workItems.AddRange(newWorkItems);
        }

        private void SetupProjects(params Project[] projects)
        {
            projectRepoMock.Setup(x => x.GetAll()).Returns(projects);

            foreach (var project in projects)
            {
                projectRepoMock.Setup(x => x.GetById(project.Id)).Returns(project);
            }
        }

        private void SetupRefreshSettings(int interval, int refreshAfter)
        {
            var refreshSettings = new RefreshSettings { Interval = interval, RefreshAfter = refreshAfter, StartDelay = 0 };
            appSettingServiceMock.Setup(x => x.GetFeaturRefreshSettings()).Returns(refreshSettings);
        }


        private Team CreateTeam()
        {
            var team = new Team { Name = "Team", Id = idCounter++ };

            team.WorkItemTypes.Add("User Story");

            var workTrackingConnection = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.Jira };
            team.WorkTrackingSystemConnection = workTrackingConnection;

            return team;
        }

        private Project CreateProject(params Team[] teams)
        {
            return CreateProject(DateTime.Now, teams);
        }

        private Project CreateProject(DateTime lastUpdateTime, params Team[] teams)
        {
            var project = new Project
            {
                Id = idCounter++,
                Name = "Release 1",
            };

            project.WorkItemTypes.Add("Feature");
            project.UpdateTeams(teams);

            var workTrackingConnection = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.Jira };
            project.WorkTrackingSystemConnection = workTrackingConnection;

            project.ProjectUpdateTime = lastUpdateTime;

            return project;
        }

        private WorkItemUpdateService CreateSubject()
        {
            return new WorkItemUpdateService(Mock.Of<ILogger<WorkItemUpdateService>>(), ServiceScopeFactory, UpdateQueueService);
        }
    }
}
