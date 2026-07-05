using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.WorkItemRules;
using Lighthouse.Backend.Services.Implementation.WorkItems;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkItems
{
    public class WorkItemServiceTest
    {
        private Mock<IRepository<Feature>> featureRepositoryMock;
        private Mock<IWorkItemRepository> workItemRepositoryMock;
        private Mock<IWorkTrackingConnector> workTrackingConnectorMock;
        private Mock<IPortfolioMetricsService> projectMetricsServiceMock;
        private Mock<IRepository<Team>> teamRepositoryMock;
        private Mock<IWorkItemStateTransitionRepository> stateTransitionRepositoryMock;
        private Mock<IFeatureStateTransitionRepository> featureStateTransitionRepositoryMock;
        private Mock<IDomainEventDispatcher> domainEventDispatcherMock;

        private int idCounter;
        private List<WorkItem> workItems;
        private List<Team> teams;
        private List<WorkItemStateTransition> storedTransitions;

        [SetUp]
        public void SetUp()
        {
            workTrackingConnectorMock = new Mock<IWorkTrackingConnector>();
            featureRepositoryMock = new Mock<IRepository<Feature>>();
            workItemRepositoryMock = new Mock<IWorkItemRepository>();
            projectMetricsServiceMock = new Mock<IPortfolioMetricsService>();
            teamRepositoryMock = new Mock<IRepository<Team>>();
            stateTransitionRepositoryMock = new Mock<IWorkItemStateTransitionRepository>();
            featureStateTransitionRepositoryMock = new Mock<IFeatureStateTransitionRepository>();
            domainEventDispatcherMock = new Mock<IDomainEventDispatcher>();

            featureStateTransitionRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<FeatureStateTransition, bool>>>()))
                .Returns(new List<FeatureStateTransition>().AsQueryable());

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(It.IsAny<Portfolio>())).Returns(Task.FromResult(new List<Feature>()));

            workItems = [];
            teams = [];
            storedTransitions = [];

            workItemRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<WorkItem, bool>>>()))
                .Returns((Expression<Func<WorkItem, bool>> predicate) => workItems.Where(predicate.Compile()).AsQueryable());
            workItemRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<WorkItem, bool>>()))
                .Returns((Func<WorkItem, bool> predicate) => workItems.SingleOrDefault(predicate));
            workItemRepositoryMock.Setup(x => x.Add(It.IsAny<WorkItem>()))
                .Callback((WorkItem item) => workItems.Add(item));
            teamRepositoryMock.Setup(x => x.GetAll()).Returns(() => teams);

            stateTransitionRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<WorkItemStateTransition, bool>>>()))
                .Returns((Expression<Func<WorkItemStateTransition, bool>> predicate) => storedTransitions.Where(predicate.Compile()).AsQueryable());
            stateTransitionRepositoryMock.Setup(x => x.Add(It.IsAny<WorkItemStateTransition>()))
                .Callback((WorkItemStateTransition transition) => storedTransitions.Add(transition));
        }

        [Test]
        public async Task UpdateFeaturesForProject_SingleTeamInvolved_FindsFeauture()
        {
            var team = CreateTeam();

            var project = CreatePortfolio();
            var feature = new Feature(team, 12) { ReferenceId = "12" };

            SetupWorkForFeature(feature, 1, 0, team);

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature }));

            var subject = CreateSubject();
            await subject.UpdateFeaturesForPortfolio(project);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(project.Features.ToList(), Has.Count.EqualTo(1));

                var actualFeature = project.Features.Single();
                Assert.That(actualFeature.ReferenceId, Is.EqualTo(feature.ReferenceId));
                Assert.That(feature.IsUsingDefaultFeatureSize, Is.False);
            }
        }

        [Test]
        public async Task UpdateFeaturesForProject_GivenExistingFeatures_ClearsExistingFeatures()
        {
            var team = CreateTeam();
            var project = CreatePortfolio();

            var existingFeature = new Feature(team, 12) { Id = 12 };

            project.Features.Add(existingFeature);

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature>()));

            var subject = CreateSubject();
            await subject.UpdateFeaturesForPortfolio(project);

            Assert.That(project.Features, Is.Empty);
        }

        [Test]
        [TestCase("Analysis in Progress", 2, 42, 42)]
        [TestCase("Analysis Done", 7, 42, 7)]
        public async Task UpdateFeaturesForProject_HasRemainingWork_InStateToOverrideRealWork_UsesDefaultItems(string featureState, int remainingWork, int defaultWork, int expectedWork)
        {
            var team = CreateTeam();
            var project = CreatePortfolio();

            project.DefaultAmountOfWorkItemsPerFeature = defaultWork;
            project.OverrideRealChildCountStates.Add("Analysis in Progress");

            var feature1 = new Feature(team, 2) { ReferenceId = "12", State = featureState };
            var feature2 = new Feature(team, 2) { ReferenceId = "1337" };

            SetupWorkForFeature(feature1, 10, remainingWork, team);
            SetupWorkForFeature(feature2, 15, 12, team);

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));

            var subject = CreateSubject();
            await subject.UpdateFeaturesForPortfolio(project);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(project.Features, Has.Count.EqualTo(2));

                var feature = project.Features[0];

                var isDefault = defaultWork == expectedWork;
                Assert.That(feature.IsUsingDefaultFeatureSize, Is.EqualTo(isDefault));
                Assert.That(feature.FeatureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(expectedWork));
            }
        }

        [Test]
        [TestCase(10, 20, 10)]
        [TestCase(0, 10, 0)]
        [TestCase(10, 10, 10)]
        [TestCase(0, 0, 0)]
        public async Task UpdateFeaturesForProject_IsInDoneState_DoesNotUsesDefaultItems(int remainingWork, int totalWork, int expectedWork)
        {
            var team = CreateTeam();
            var project = CreatePortfolio();


            project.DoneStates.Clear();
            project.DoneStates.Add("Done");

            project.DefaultAmountOfWorkItemsPerFeature = 42;

            var feature = new Feature(team, remainingWork) { ReferenceId = "12", State = "Done", StateCategory = StateCategories.Done };
            SetupWorkForFeature(feature, totalWork, remainingWork, team);

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature }));

            var subject = CreateSubject();
            await subject.UpdateFeaturesForPortfolio(project);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(project.Features, Has.Count.EqualTo(1));

                var featureToVerify = project.Features[0];

                Assert.That(featureToVerify.IsUsingDefaultFeatureSize, Is.False);
                Assert.That(featureToVerify.FeatureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(expectedWork));
            }
        }

        [Test]
        public async Task UpdateFeaturesForProject_HasRemainingWork_InStateToOverrideRealWork_ItemWasMovedBack_UsesDefaultItems()
        {
            var team = CreateTeam();
            var project = CreatePortfolio();


            project.DefaultAmountOfWorkItemsPerFeature = 7;
            project.OverrideRealChildCountStates.Add("Analysis in Progress");

            var feature1 = new Feature(team, 2) { ReferenceId = "12", State = "Analysis in Progress" };
            var feature2 = new Feature(team, 2) { ReferenceId = "1337" };

            var features = new List<Feature>() { feature1, feature2 };

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));

            featureRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<Feature, bool>>())).Returns((Func<Feature, bool> predicate) => features.SingleOrDefault(predicate));

            var subject = CreateSubject();
            await subject.UpdateFeaturesForPortfolio(project);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(project.Features, Has.Count.EqualTo(2));

                var feature = project.Features.Single(f => f.ReferenceId == "12");
                Assert.That(feature.IsUsingDefaultFeatureSize, Is.True);
                Assert.That(feature.FeatureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(7));
            }
        }

        [Test]
        public async Task UpdateFeaturesForProject_NoRemainingWork_NoTotalWork_AddsDefaultRemainingWorkToFeature()
        {
            var team = CreateTeam();
            var project = CreatePortfolio();
            project.DefaultAmountOfWorkItemsPerFeature = 12;


            var feature1 = new Feature(team, 0) { ReferenceId = "42" };
            var feature2 = new Feature(team, 2) { ReferenceId = "12" };

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));

            var subject = CreateSubject();
            await subject.UpdateFeaturesForPortfolio(project);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(project.Features, Has.Count.EqualTo(2));

                var feature = project.Features[0];
                Assert.That(feature.FeatureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(12));
                Assert.That(feature.IsUsingDefaultFeatureSize, Is.True);
            }
        }

        [Test]
        public async Task UpdateFeaturesForProject_NoWorkForAnyFeature_SplitsAcrossAllAvailableTeams()
        {
            _ = CreateTeam();
            _ = CreateTeam();

            var portfolio = CreatePortfolio();
            portfolio.DefaultAmountOfWorkItemsPerFeature = 12;

            var feature1 = new Feature { ReferenceId = "42" };
            var feature2 = new Feature { ReferenceId = "12" };

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(portfolio)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));

            var subject = CreateSubject();
            await subject.UpdateFeaturesForPortfolio(portfolio);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(portfolio.Features, Has.Count.EqualTo(2));

                Assert.That(portfolio.Teams.ToList(), Has.Count.EqualTo(2));

                foreach (var feature in portfolio.Features)
                {
                    Assert.That(feature.FeatureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(12));
                    Assert.That(feature.IsUsingDefaultFeatureSize, Is.True);

                    Assert.That(feature.Teams.ToList(), Has.Count.EqualTo(2));
                }
            }
        }

        [Test]
        [TestCase(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, 80, 8)]
        [TestCase(new[] { 2, 4, 10, 3, 4, 5, 9, 7, 8, 7 }, 80, 8)]
        [TestCase(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, 120, 10)]
        [TestCase(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, 0, 1)]
        [TestCase(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, 1, 1)]
        [TestCase(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, 65, 6)]
        [TestCase(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, 85, 8)]
        public async Task UpdateFeaturesForProject_UseCalculatedDefault_AddsDefaultRemainingWorkBasedOnPercentileToFeature(int[] childItemCount, int percentile, int expectedValue)
        {
            var team = CreateTeam();
            var project = CreatePortfolio();

            project.UsePercentileToCalculateDefaultAmountOfWorkItems = true;
            project.PercentileHistoryInDays = 90;
            project.DefaultWorkItemPercentile = percentile;
            project.DefaultAmountOfWorkItemsPerFeature = 12;

            var feature1 = new Feature(team, 0) { ReferenceId = "42" };
            var feature2 = new Feature(team, 2) { ReferenceId = "12" };

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));

            var features = new List<Feature>();

            for (var index = 0; index < childItemCount.Length; index++)
            {
                var feature = new Feature
                {
                    Id = index
                };

                var remainingWork = childItemCount[index];
                feature.AddOrUpdateWorkForTeam(team, remainingWork, remainingWork);

                features.Add(feature);
            }

            projectMetricsServiceMock.Setup(x => x.GetCycleTimeDataForPortfolio(project, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(features);

            var subject = CreateSubject();
            await subject.UpdateFeaturesForPortfolio(project);

            using (Assert.EnterMultipleScope())
            {
                var feature = project.Features[0];
                Assert.That(feature.FeatureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(expectedValue));
                Assert.That(feature.IsUsingDefaultFeatureSize, Is.True);
            }
        }

        [Test]
        public async Task UpdateFeaturesForProject_UseCalculatedDefault_QueryHasNoMatches_AddsDefaultRemainingWork()
        {
            var team = CreateTeam();
            var project = CreatePortfolio();

            project.UsePercentileToCalculateDefaultAmountOfWorkItems = true;
            project.PercentileHistoryInDays = 45;
            project.DefaultWorkItemPercentile = 80;
            project.DefaultAmountOfWorkItemsPerFeature = 12;


            var feature1 = new Feature(team, 0) { ReferenceId = "42" };
            var feature2 = new Feature(team, 2) { ReferenceId = "12" };

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));
            projectMetricsServiceMock.Setup(x => x.GetCycleTimeDataForPortfolio(project, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<Feature>());

            var subject = CreateSubject();
            await subject.UpdateFeaturesForPortfolio(project);

            using (Assert.EnterMultipleScope())
            {
                var feature = project.Features[0];
                Assert.That(feature.FeatureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(12));
                Assert.That(feature.IsUsingDefaultFeatureSize, Is.True);
            }
        }

        [Test]
        public async Task UpdateFeaturesForProject_NoRemainingWork_NotTotalWork_SizeEstimateFieldSet_SizeEstimateNotAvailable_AddsDefaultRemainingWorkToFeature()
        {
            var team = CreateTeam();
            var project = CreatePortfolio();
            project.DefaultAmountOfWorkItemsPerFeature = 12;
            project.SizeEstimateAdditionalFieldDefinitionId = 1;


            var feature1 = new Feature(team, 0) { ReferenceId = "42", EstimatedSize = 0 };
            var feature2 = new Feature(team, 2) { ReferenceId = "12" };

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));

            var subject = CreateSubject();
            await subject.UpdateFeaturesForPortfolio(project);

            Assert.That(project.Features, Has.Count.EqualTo(2));
            Assert.That(project.Features[0].FeatureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(12));
        }

        [Test]
        public async Task UpdateFeaturesForProject_NoRemainingWork_NoTotalWork_SizeEstimateFieldSet_SizeEstimateAvailable_AddsEstimatedWorkToFeature()
        {
            var team = CreateTeam();
            var project = CreatePortfolio();
            project.DefaultAmountOfWorkItemsPerFeature = 12;
            project.SizeEstimateAdditionalFieldDefinitionId = 1; // Size field configured and feature has size


            var feature1 = new Feature(team, 0) { ReferenceId = "42", EstimatedSize = 7 };
            var feature2 = new Feature(team, 2) { ReferenceId = "12" };

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));

            var subject = CreateSubject();
            await subject.UpdateFeaturesForPortfolio(project);

            Assert.That(project.Features, Has.Count.EqualTo(2));
            Assert.That(project.Features[0].FeatureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(7));
        }

        [Test]
        public async Task UpdateFeaturesForProject_NoRemainingWork_HasTotalWork_DoesNotAddDefaultRemainingWorkToFeature()
        {
            var team = CreateTeam();
            var project = CreatePortfolio();
            project.DefaultAmountOfWorkItemsPerFeature = 12;


            var feature1 = new Feature(team, 0) { ReferenceId = "42" };
            var feature2 = new Feature(team, 2) { ReferenceId = "12" };

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));

            SetupWorkForFeature(feature1, 7, 0, team);

            var subject = CreateSubject();
            await subject.UpdateFeaturesForPortfolio(project);

            Assert.That(project.Features, Has.Count.EqualTo(2));
            Assert.That(project.Features[0].FeatureWork.Sum(x => x.RemainingWorkItems), Is.Zero);
        }

        [Test]
        public async Task UpdateFeaturesForProject_NoTotalWork_MulitpleTeams_SplitsDefaultRemainingWorkByTeam()
        {
            var team1 = CreateTeam();
            var team2 = CreateTeam();
            var project = CreatePortfolio();


            project.DefaultAmountOfWorkItemsPerFeature = 12;

            var feature1 = new Feature([(team1, 0, 12), (team2, 0, 10)]) { ReferenceId = "17" };
            var feature2 = new Feature([(team1, 2, 13), (team2, 2, 3)]) { ReferenceId = "19" };

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));
            SetupWorkForFeature(feature1, 0, 0);
            SetupWorkForFeature(feature2, 12, 12);

            var subject = CreateSubject();
            await subject.UpdateFeaturesForPortfolio(project);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(project.Features, Has.Count.EqualTo(2));

                var featureWork = project.Features[0].FeatureWork;
                Assert.That(featureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(12));
                Assert.That(featureWork[^1].RemainingWorkItems, Is.EqualTo(6));
                Assert.That(featureWork[^1].RemainingWorkItems, Is.EqualTo(6));
            }
        }

        [Test]
        public async Task UpdateFeaturesForProject_SingleTeamInvolved_FindsRemainingWorkByTeam()
        {
            var team = CreateTeam();
            var project = CreatePortfolio();


            var remainingWorkItems = 12;
            var feature = new Feature(team, remainingWorkItems) { ReferenceId = "42" };

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature }));
            SetupWorkForFeature(feature, 12, remainingWorkItems, team);

            var subject = CreateSubject();
            await subject.UpdateFeaturesForPortfolio(project);

            var actualFeature = project.Features.Single();
            Assert.That(actualFeature.GetRemainingWorkForTeam(team), Is.EqualTo(remainingWorkItems));
        }


        [Test]
        public async Task UpdateFeaturesForProject_MultipleTeamsInvolved()
        {
            _ = CreateTeam();
            _ = CreateTeam();
            var project = CreatePortfolio();


            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(It.IsAny<Portfolio>())).Returns(Task.FromResult(new List<Feature>()));

            var subject = CreateSubject();
            await subject.UpdateFeaturesForPortfolio(project);

            workTrackingConnectorMock.Verify(x => x.GetFeaturesForProject(project), Times.Exactly(1));
        }

        [Test]
        public async Task UpdateFeaturesForProject_TwoTeamsInvolved_IndividualFeatures_FindsRemainingWorkByTeam()
        {
            var team1 = CreateTeam();
            var team2 = CreateTeam();

            var project = CreatePortfolio();

            var remainingWorkItemsFeature1 = 12;
            var remainingWorkItemsFeature2 = 7;
            var feature1 = new Feature(team1, remainingWorkItemsFeature1) { ReferenceId = "1" };
            var feature2 = new Feature(team2, remainingWorkItemsFeature1) { ReferenceId = "2" };

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));

            SetupWorkForFeature(feature1, 12, remainingWorkItemsFeature1, team1);
            SetupWorkForFeature(feature2, 12, remainingWorkItemsFeature2, team2);

            var subject = CreateSubject();
            await subject.UpdateFeaturesForPortfolio(project);

            var actualFeature1 = project.Features[0];
            Assert.That(actualFeature1.GetRemainingWorkForTeam(team1), Is.EqualTo(remainingWorkItemsFeature1));
            var actualFeature2 = project.Features[^1];
            Assert.That(actualFeature2.GetRemainingWorkForTeam(team2), Is.EqualTo(remainingWorkItemsFeature2));
        }

        [Test]
        public async Task UpdateFeaturesForProject_TwoTeamsInvolved_SingleFeature_FindsRemainingWorkByTeam()
        {
            var team1 = CreateTeam();
            var team2 = CreateTeam();

            var project = CreatePortfolio();

            var remainingWorkItemsTeam1 = 12;
            var remainingWorkItemsTeam2 = 7;
            var feature = new Feature(team1, remainingWorkItemsTeam1) { ReferenceId = "1" };

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature }));

            SetupWorkForFeature(feature, 12, remainingWorkItemsTeam1, team1);
            SetupWorkForFeature(feature, 12, remainingWorkItemsTeam2, team2);

            var subject = CreateSubject();
            await subject.UpdateFeaturesForPortfolio(project);

            var actualFeature = project.Features.Single();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(actualFeature.GetRemainingWorkForTeam(team1), Is.EqualTo(remainingWorkItemsTeam1));
                Assert.That(actualFeature.GetRemainingWorkForTeam(team2), Is.EqualTo(remainingWorkItemsTeam2));
            }
        }

        [Test]
        public async Task UpdateFeaturesForProject_UseDefaultWork_NoOwningTeam_SplitsWorkByAllInvolvedTeams()
        {
            var team1 = CreateTeam();
            var team2 = CreateTeam();

            var project = CreatePortfolio();
            project.DefaultAmountOfWorkItemsPerFeature = 12;

            var testTeams = new List<(Team team, int remainingItems, int totalItems)> { (team1, 0, 0), (team2, 0, 0) };
            var feature = new Feature(testTeams) { ReferenceId = "42" };

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature }));
            SetupWorkForFeature(feature, 0, 0);

            var subject = CreateSubject();
            await subject.UpdateFeaturesForPortfolio(project);

            using (Assert.EnterMultipleScope())
            {
                var featureToVerify = project.Features.Single();

                Assert.That(featureToVerify.FeatureWork, Has.Count.EqualTo(2));
                Assert.That(featureToVerify.FeatureWork[0].TotalWorkItems, Is.EqualTo(6));
                Assert.That(featureToVerify.FeatureWork[^1].TotalWorkItems, Is.EqualTo(6));
            }
        }

        [Test]
        public async Task UpdateFeaturesForProject_UseDefaultWork_OwningTeam_AssignsAllWorkToOwningTeam()
        {
            var team1 = CreateTeam();
            var team2 = CreateTeam();

            var project = CreatePortfolio();
            project.DefaultAmountOfWorkItemsPerFeature = 12;
            project.OwningTeam = team2;


            var testTeams = new List<(Team team, int remainingItems, int totalItems)> { (team1, 0, 0), (team2, 0, 0) };
            var feature = new Feature(testTeams) { ReferenceId = "42" };

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature }));
            SetupWorkForFeature(feature, 0, 0);

            var subject = CreateSubject();
            await subject.UpdateFeaturesForPortfolio(project);

            using (Assert.EnterMultipleScope())
            {
                var featureToVerify = project.Features.Single();

                Assert.That(featureToVerify.FeatureWork, Has.Count.EqualTo(1));
                Assert.That(featureToVerify.FeatureWork[^1].TotalWorkItems, Is.EqualTo(12));
                Assert.That(featureToVerify.FeatureWork[^1].Team, Is.EqualTo(team2));
            }
        }

        [Test]
        public async Task UpdateFeaturesForProject_UseDefaultWork_NoOwningTeam_FeatureOwnerDefined_AssignsAllWorkToOwningTeam()
        {
            var team1 = CreateTeam();
            team1.Name = "Godzilla";

            var team2 = CreateTeam();
            team2.Name = "The Most Awesome";

            var portfolio = CreatePortfolio();
            portfolio.DefaultAmountOfWorkItemsPerFeature = 12;
            portfolio.OwningTeam = null;
            portfolio.FeatureOwnerAdditionalFieldDefinitionId = 1;

            AddAdditionalField(portfolio, 1, "MyCustomField", "My Custom Field");

            var testTeams = new List<(Team team, int remainingItems, int totalItems)> { (team1, 0, 0), (team2, 0, 0) };
            var feature = new Feature(testTeams)
            {
                ReferenceId = "42",
                AdditionalFieldValues =
                {
                    [1] = @"Project\The Most Awesome\Features"
                }
            };

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(portfolio)).Returns(Task.FromResult(new List<Feature> { feature }));
            SetupWorkForFeature(feature, 0, 0);

            var subject = CreateSubject();
            await subject.UpdateFeaturesForPortfolio(portfolio);

            using (Assert.EnterMultipleScope())
            {
                var featureToVerify = portfolio.Features.Single();

                Assert.That(featureToVerify.FeatureWork, Has.Count.EqualTo(1));
                Assert.That(featureToVerify.FeatureWork[^1].TotalWorkItems, Is.EqualTo(12));
                Assert.That(featureToVerify.FeatureWork[^1].Team, Is.EqualTo(team2));
            }
        }

        [Test]
        public async Task UpdateFeaturesForProject_UseDefaultWork_OwningTeam_FeatureOwnerDefined_AssignsAllWorkToFeatureOwningTeam()
        {
            var team1 = CreateTeam();
            var team2 = CreateTeam();
            team2.Name = "The Most Awesome";

            var portfolio = CreatePortfolio();
            portfolio.DefaultAmountOfWorkItemsPerFeature = 12;
            portfolio.OwningTeam = team1;
            portfolio.FeatureOwnerAdditionalFieldDefinitionId = 12;

            AddAdditionalField(portfolio, 12, "MyCustomField", "My Custom Field");

            var testTeams = new List<(Team team, int remainingItems, int totalItems)> { (team1, 0, 0), (team2, 0, 0) };
            var feature = new Feature(testTeams)
            {
                ReferenceId = "42",
                AdditionalFieldValues =
                {
                    [12] = @"Project\The Most Awesome\Features"
                }
            };

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(portfolio)).Returns(Task.FromResult(new List<Feature> { feature }));
            SetupWorkForFeature(feature, 0, 0);

            var subject = CreateSubject();
            await subject.UpdateFeaturesForPortfolio(portfolio);

            using (Assert.EnterMultipleScope())
            {
                var featureToVerify = portfolio.Features.Single();

                Assert.That(featureToVerify.FeatureWork, Has.Count.EqualTo(1));
                Assert.That(featureToVerify.FeatureWork[^1].TotalWorkItems, Is.EqualTo(12));
                Assert.That(featureToVerify.FeatureWork[^1].Team, Is.EqualTo(team2));
            }
        }

        [Test]
        public async Task UpdateFeaturesForProject_UseDefaultWork_NoOwningTeam_MultipleFeatureOwnerDefined_SplitsAllWorkAcrossFeatureOwningTeams()
        {
            var team1 = CreateTeam();
            var team2 = CreateTeam();
            team2.Name = "The Most Awesome";

            var team3 = CreateTeam();
            team3.Name = "Other";

            var portfolio = CreatePortfolio();
            portfolio.DefaultAmountOfWorkItemsPerFeature = 12;
            portfolio.FeatureOwnerAdditionalFieldDefinitionId = 1;

            AddAdditionalField(portfolio, 1, "MyCustomField", "My Custom Field");

            var testTeams = new List<(Team team, int remainingItems, int totalItems)> { (team1, 0, 0), (team2, 0, 0), (team3, 0, 0) };
            var feature = new Feature(testTeams)
            {
                ReferenceId = "42",
                AdditionalFieldValues =
                {
                    [1] = "The Most Awesome;Other"
                }
            };
            SetupWorkForFeature(feature, 0, 0);

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(portfolio)).Returns(Task.FromResult(new List<Feature> { feature }));

            var subject = CreateSubject();
            await subject.UpdateFeaturesForPortfolio(portfolio);

            using (Assert.EnterMultipleScope())
            {
                var featureToVerify = portfolio.Features.Single();

                Assert.That(featureToVerify.FeatureWork, Has.Count.EqualTo(2));
                Assert.That(featureToVerify.FeatureWork[0].TotalWorkItems, Is.EqualTo(6));
                Assert.That(featureToVerify.FeatureWork[0].Team, Is.EqualTo(team2));
                Assert.That(featureToVerify.FeatureWork[1].TotalWorkItems, Is.EqualTo(6));
                Assert.That(featureToVerify.FeatureWork[1].Team, Is.EqualTo(team3));
            }
        }

        [Test]
        public async Task UpdateFeaturesForProject_UseDefaultWork_OwningTeam_MultipleFeatureOwnerDefined_SplitsAllWorkAcrossFeatureOwningTeams()
        {
            var team1 = CreateTeam();
            var team2 = CreateTeam();
            team2.Name = "The Most Awesome";

            var team3 = CreateTeam();
            team3.Name = "Other";

            var portfolio = CreatePortfolio();
            portfolio.DefaultAmountOfWorkItemsPerFeature = 12;
            portfolio.FeatureOwnerAdditionalFieldDefinitionId = 1;
            portfolio.OwningTeam = team1;

            AddAdditionalField(portfolio, 1, "MyCustomField", "My Custom Field");

            var testTeams = new List<(Team team, int remainingItems, int totalItems)> { (team1, 0, 0), (team2, 0, 0), (team3, 0, 0) };
            var feature = new Feature(testTeams)
            {
                ReferenceId = "42",
                AdditionalFieldValues =
                {
                    [1] = "The Most Awesome;Other"
                }
            };
            SetupWorkForFeature(feature, 0, 0);

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(portfolio)).Returns(Task.FromResult(new List<Feature> { feature }));

            var subject = CreateSubject();
            await subject.UpdateFeaturesForPortfolio(portfolio);

            using (Assert.EnterMultipleScope())
            {
                var featureToVerify = portfolio.Features.Single();

                Assert.That(featureToVerify.FeatureWork, Has.Count.EqualTo(2));
                Assert.That(featureToVerify.FeatureWork[0].TotalWorkItems, Is.EqualTo(6));
                Assert.That(featureToVerify.FeatureWork[0].Team, Is.EqualTo(team2));
                Assert.That(featureToVerify.FeatureWork[1].TotalWorkItems, Is.EqualTo(6));
                Assert.That(featureToVerify.FeatureWork[1].Team, Is.EqualTo(team3));
            }
        }

        [Test]
        public async Task UpdateFeaturesForProject_UseDefaultWork_OwningTeam_FeatureOwnerNotFound_AssignsWorkToOwningTeam()
        {
            var team1 = CreateTeam();
            var team2 = CreateTeam();
            team2.Name = "The Most Awesome";

            var team3 = CreateTeam();
            team3.Name = "Other";

            var portfolio = CreatePortfolio();
            portfolio.DefaultAmountOfWorkItemsPerFeature = 12;
            portfolio.FeatureOwnerAdditionalFieldDefinitionId = 1;
            portfolio.OwningTeam = team1;

            AddAdditionalField(portfolio, 1, "MyCustomField", "My Custom Field");

            var testTeams = new List<(Team team, int remainingItems, int totalItems)> { (team1, 0, 0), (team2, 0, 0), (team3, 0, 0) };
            var feature = new Feature(testTeams)
            {
                ReferenceId = "42",
                AdditionalFieldValues =
                {
                    [1] = "Some Random String That does not contain a team name!"
                }
            };
            SetupWorkForFeature(feature, 0, 0);

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(portfolio)).Returns(Task.FromResult(new List<Feature> { feature }));

            var subject = CreateSubject();
            await subject.UpdateFeaturesForPortfolio(portfolio);

            using (Assert.EnterMultipleScope())
            {
                var featureToVerify = portfolio.Features.Single();

                Assert.That(featureToVerify.FeatureWork, Has.Count.EqualTo(1));
                Assert.That(featureToVerify.FeatureWork[0].TotalWorkItems, Is.EqualTo(12));
                Assert.That(featureToVerify.FeatureWork[0].Team, Is.EqualTo(team1));
            }
        }

        [Test]
        public async Task UpdateFeaturesForProject_UseDefaultWork_NoOwningTeam_FeatureOwnerNotFound_SplitsAcrossAllTeams()
        {
            var team1 = CreateTeam();
            var team2 = CreateTeam();
            team2.Name = "The Most Awesome";

            var team3 = CreateTeam();
            team3.Name = "Other";

            var portfolio = CreatePortfolio();
            portfolio.DefaultAmountOfWorkItemsPerFeature = 15;
            portfolio.FeatureOwnerAdditionalFieldDefinitionId = 1;

            AddAdditionalField(portfolio, 1, "MyCustomField", "My Custom Field");

            var testTeams = new List<(Team team, int remainingItems, int totalItems)> { (team1, 0, 0), (team2, 0, 0), (team3, 0, 0) };
            var feature = new Feature(testTeams)
            {
                ReferenceId = "42",
                AdditionalFieldValues =
                {
                    [1] = "Some Random String That does not contain a team name!"
                }
            };
            SetupWorkForFeature(feature, 0, 0);

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(portfolio)).Returns(Task.FromResult(new List<Feature> { feature }));

            var subject = CreateSubject();
            await subject.UpdateFeaturesForPortfolio(portfolio);

            using (Assert.EnterMultipleScope())
            {
                var featureToVerify = portfolio.Features.Single();

                Assert.That(featureToVerify.FeatureWork, Has.Count.EqualTo(3));

                foreach (var fw in featureToVerify.FeatureWork)
                {
                    Assert.That(fw.TotalWorkItems, Is.EqualTo(5));
                }
            }
        }

        [Test]
        public async Task UpdateFeaturesForProject_FeatureHasParent_StoresParentAsFeature()
        {
            var team = CreateTeam();
            var project = CreatePortfolio();
            var feature = new Feature(team, 12) { ReferenceId = "12", ParentReferenceId = "1886" };
            var parentFeature = new Feature { ReferenceId = "1886" };

            featureRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<Feature, bool>>())).Returns((Func<Feature, bool> _) => null);

            SetupWorkForFeature(feature, 1, 0, team);

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature }));
            workTrackingConnectorMock.Setup(x => x.GetParentFeaturesDetails(project, new[] { feature.ParentReferenceId })).ReturnsAsync(
                [parentFeature]);

            var subject = CreateSubject();
            await subject.UpdateFeaturesForPortfolio(project);

            featureRepositoryMock.Verify(x => x.Add(parentFeature));
            Assert.That(parentFeature.IsParentFeature, Is.True);
        }

        [Test]
        public async Task UpdateFeaturesForProject_FeatureHasParent_FeatureAlreadyExists_UpdatesParentFeature()
        {
            var team = CreateTeam();
            var project = CreatePortfolio();
            var feature = new Feature(team, 12) { ReferenceId = "12", ParentReferenceId = "1886" };
            var parentFeature = new Feature { ReferenceId = "1886" };

            featureRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<Feature, bool>>())).Returns((Func<Feature, bool> predicate) => new List<Feature> { parentFeature }.SingleOrDefault(predicate));

            SetupWorkForFeature(feature, 1, 0, team);

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature }));
            workTrackingConnectorMock.Setup(x => x.GetParentFeaturesDetails(project, new[] { feature.ParentReferenceId })).ReturnsAsync(
                [parentFeature]);

            var subject = CreateSubject();
            await subject.UpdateFeaturesForPortfolio(project);

            featureRepositoryMock.Verify(x => x.Update(It.IsAny<Feature>()), Times.Never());
            Assert.That(parentFeature.IsParentFeature, Is.True);
        }

        [Test]
        public async Task UpdateWorkItemsForTeam_NoExistingItems_AddsWorkItems()
        {
            var team = CreateTeam();

            AddWorkItemForTeam(team);
            AddWorkItemForTeam(team);

            workTrackingConnectorMock.Setup(x => x.GetWorkItemsForTeam(team)).ReturnsAsync(workItems.ToList());

            workItems.Clear();

            var subject = CreateSubject();

            await subject.UpdateWorkItemsForTeam(team);

            workItemRepositoryMock.Verify(x => x.Add(It.IsAny<WorkItem>()), Times.Exactly(2));
            workItemRepositoryMock.Verify(x => x.Update(It.IsAny<WorkItem>()), Times.Never);
            workItemRepositoryMock.Verify(x => x.Remove(It.IsAny<int>()), Times.Never);
            workItemRepositoryMock.Verify(x => x.Save(), Times.AtLeastOnce);
        }

        [Test]
        public async Task UpdateWorkItemsForTeam_ItemsExist_UpdatesWorkItems()
        {
            var team = CreateTeam();

            AddWorkItemForTeam(team);
            AddWorkItemForTeam(team);

            workTrackingConnectorMock.Setup(x => x.GetWorkItemsForTeam(team)).ReturnsAsync(workItems);

            var subject = CreateSubject();

            await subject.UpdateWorkItemsForTeam(team);

            workItemRepositoryMock.Verify(x => x.Update(It.IsAny<WorkItem>()), Times.Exactly(2));
            workItemRepositoryMock.Verify(x => x.Add(It.IsAny<WorkItem>()), Times.Never);
            workItemRepositoryMock.Verify(x => x.Remove(It.IsAny<int>()), Times.Never);
            workItemRepositoryMock.Verify(x => x.Save(), Times.AtLeastOnce);
        }

        [Test]
        public async Task UpdateWorkItemsForTeam_ItemsDontExistAnymore_RemovesWorkItems()
        {
            var team = CreateTeam();

            AddWorkItemForTeam(team);
            AddWorkItemForTeam(team);

            workTrackingConnectorMock.Setup(x => x.GetWorkItemsForTeam(team)).ReturnsAsync([]);

            var subject = CreateSubject();

            await subject.UpdateWorkItemsForTeam(team);

            workItemRepositoryMock.Verify(x => x.Remove(It.IsAny<int>()), Times.Exactly(2));
            workItemRepositoryMock.Verify(x => x.Add(It.IsAny<WorkItem>()), Times.Never);
            workItemRepositoryMock.Verify(x => x.Update(It.IsAny<WorkItem>()), Times.Never);
            workItemRepositoryMock.Verify(x => x.Save(), Times.AtLeastOnce);
        }

        [Test]
        public async Task RefreshWorkItems_DerivesCurrentStateEnteredAt_FromLatestMatchingTransition_Idempotently()
        {
            var team = CreateTeam();

            var earlierDoing = new DateTime(2026, 5, 20, 9, 0, 0, DateTimeKind.Utc);
            var latestDoing = new DateTime(2026, 5, 23, 14, 0, 0, DateTimeKind.Utc);

            var incoming = CreateWorkItemWithTransitions(team, "Doing",
                ("To Do", "Doing", earlierDoing),
                ("Doing", "Review", new DateTime(2026, 5, 22, 10, 0, 0, DateTimeKind.Utc)),
                ("Review", "Doing", latestDoing));

            workTrackingConnectorMock.Setup(x => x.GetWorkItemsForTeam(team)).ReturnsAsync([incoming]);

            var subject = CreateSubject();
            await subject.UpdateWorkItemsForTeam(team);

            var persistedAfterFirstSync = workItems.Single(wi => wi.ReferenceId == incoming.ReferenceId);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(persistedAfterFirstSync.CurrentStateEnteredAt, Is.EqualTo(latestDoing));
                Assert.That(storedTransitions, Has.Count.EqualTo(3));
            }

            await subject.UpdateWorkItemsForTeam(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(storedTransitions, Has.Count.EqualTo(3));
                Assert.That(persistedAfterFirstSync.CurrentStateEnteredAt, Is.EqualTo(latestDoing));
            }
        }

        [Test]
        public async Task RefreshWorkItems_NoTransitionMatchesCurrentState_LeavesCurrentStateEnteredAtNull()
        {
            var team = CreateTeam();

            var incoming = CreateWorkItemWithTransitions(team, "Doing",
                ("To Do", "Review", new DateTime(2026, 5, 21, 9, 0, 0, DateTimeKind.Utc)));

            workTrackingConnectorMock.Setup(x => x.GetWorkItemsForTeam(team)).ReturnsAsync([incoming]);

            var subject = CreateSubject();
            await subject.UpdateWorkItemsForTeam(team);

            var persisted = workItems.Single(wi => wi.ReferenceId == incoming.ReferenceId);
            Assert.That(persisted.CurrentStateEnteredAt, Is.Null);
        }

        [Test]
        public async Task UpdateFeaturesForProject_HasRemainingWork_InStateToOverrideRealWork_ActualWorkExceedsDefault_UsesActualWork()
        {
            var team = CreateTeam();
            var project = CreatePortfolio();

            project.DefaultAmountOfWorkItemsPerFeature = 7;
            project.OverrideRealChildCountStates.Add("Analysis in Progress");

            // Feature has 11 child stories (total) - default is only 7
            // Since 11 > 7, we expect 11 to be used, not 7
            var feature = new Feature(team, 11) { ReferenceId = "12", State = "Analysis in Progress" };
            SetupWorkForFeature(feature, 11, 11, team);

            workTrackingConnectorMock
                .Setup(x => x.GetFeaturesForProject(project))
                .Returns(Task.FromResult(new List<Feature> { feature }));

            var subject = CreateSubject();
            await subject.UpdateFeaturesForPortfolio(project);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(project.Features, Has.Count.EqualTo(1));

                var actualFeature = project.Features.Single();
                Assert.That(actualFeature.IsUsingDefaultFeatureSize, Is.False);
                Assert.That(actualFeature.FeatureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(11));
            }
        }

        [Test]
        public async Task UpdateWorkItemsForTeam_StateChanges_PublishesWorkItemTransitionedWithFromAndToState()
        {
            var team = CreateTeam();
            var existing = CreateWorkItemWithTransitions(team, "To Do");
            existing.State = "To Do";
            existing.StateCategory = StateCategories.ToDo;
            workItems.Add(existing);

            var incoming = CreateIncomingFor(existing, "Doing", StateCategories.Doing);

            workTrackingConnectorMock.Setup(x => x.GetWorkItemsForTeam(team)).ReturnsAsync([incoming]);

            var subject = CreateSubject();
            await subject.UpdateWorkItemsForTeam(team);

            domainEventDispatcherMock.Verify(
                x => x.PublishAsync(
                    It.Is<WorkItemTransitioned>(e => e.WorkItemId == existing.Id && e.FromState == "To Do" && e.ToState == "Doing"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task UpdateWorkItemsForTeam_StateUnchanged_PublishesNoWorkItemTransitioned()
        {
            var team = CreateTeam();
            var existing = CreateWorkItemWithTransitions(team, "Doing");
            existing.State = "Doing";
            existing.StateCategory = StateCategories.Doing;
            workItems.Add(existing);

            var incoming = CreateIncomingFor(existing, "Doing", StateCategories.Doing);

            workTrackingConnectorMock.Setup(x => x.GetWorkItemsForTeam(team)).ReturnsAsync([incoming]);

            var subject = CreateSubject();
            await subject.UpdateWorkItemsForTeam(team);

            domainEventDispatcherMock.Verify(
                x => x.PublishAsync(It.IsAny<WorkItemTransitioned>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task UpdateWorkItemsForTeam_StalenessThresholdCrossed_PublishesWorkItemBecameStaleOnceWithThreshold()
        {
            var team = CreateTeam();
            team.StalenessThresholdDays = 5;

            var enteredDoing = DateTime.UtcNow.AddDays(-30);
            var incoming = CreateWorkItemWithTransitions(team, "Doing", ("To Do", "Doing", enteredDoing));
            incoming.StateCategory = StateCategories.Doing;

            workTrackingConnectorMock.Setup(x => x.GetWorkItemsForTeam(team)).ReturnsAsync([incoming]);

            var subject = CreateSubject();
            await subject.UpdateWorkItemsForTeam(team);
            await subject.UpdateWorkItemsForTeam(team);

            domainEventDispatcherMock.Verify(
                x => x.PublishAsync(
                    It.Is<WorkItemBecameStale>(e => e.WorkItemId == incoming.Id && e.ThresholdDays == 5),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task UpdateWorkItemsForTeam_ItemLeavesDoingThenReentersStale_ResetsFlagAndRepublishesOnSecondCrossing()
        {
            var team = CreateTeam();
            team.StalenessThresholdDays = 5;
            workTrackingConnectorMock.Setup(x => x.SupportsTransitionHistory(It.IsAny<WorkTrackingSystemConnection>())).Returns(true);

            var firstStaleEntered = DateTime.UtcNow.AddDays(-30);
            var existing = CreateWorkItemWithTransitions(team, "Doing", ("To Do", "Doing", firstStaleEntered));
            existing.StateCategory = StateCategories.Doing;
            workItems.Add(existing);

            workTrackingConnectorMock.Setup(x => x.GetWorkItemsForTeam(team)).ReturnsAsync([existing]);

            var subject = CreateSubject();
            await subject.UpdateWorkItemsForTeam(team);

            var movedToReview = CreateIncomingFor(existing, "Review", StateCategories.Doing, ("Doing", "Review", DateTime.UtcNow.AddDays(-1)));
            workTrackingConnectorMock.Setup(x => x.GetWorkItemsForTeam(team)).ReturnsAsync([movedToReview]);
            await subject.UpdateWorkItemsForTeam(team);

            var reenteredDoingStale = CreateIncomingFor(existing, "Doing", StateCategories.Doing, ("Review", "Doing", DateTime.UtcNow.AddDays(-20)));
            workTrackingConnectorMock.Setup(x => x.GetWorkItemsForTeam(team)).ReturnsAsync([reenteredDoingStale]);
            await subject.UpdateWorkItemsForTeam(team);

            domainEventDispatcherMock.Verify(
                x => x.PublishAsync(
                    It.Is<WorkItemBecameStale>(e => e.WorkItemId == existing.Id),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Test]
        public async Task UpdateWorkItemsForTeam_BlockTransition_PublishesWorkItemBlockedOnceWhileStillBlockedPublishesNoMore()
        {
            var team = CreateTeam();
            team.BlockedStates.Add("Blocked");

            var existing = CreateWorkItemWithTransitions(team, "Doing");
            existing.State = "Doing";
            existing.StateCategory = StateCategories.Doing;
            workItems.Add(existing);

            var blocked = CreateIncomingFor(existing, "Blocked", StateCategories.Doing);
            workTrackingConnectorMock.Setup(x => x.GetWorkItemsForTeam(team)).ReturnsAsync([blocked]);

            var subject = CreateSubject();
            await subject.UpdateWorkItemsForTeam(team);
            await subject.UpdateWorkItemsForTeam(team);

            domainEventDispatcherMock.Verify(
                x => x.PublishAsync(
                    It.Is<WorkItemBlocked>(e => e.WorkItemId == existing.Id && e.Reason == "Blocked"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task UpdateWorkItemsForTeam_NeverBlocked_PublishesNoWorkItemBlocked()
        {
            var team = CreateTeam();
            team.BlockedStates.Add("Blocked");

            var existing = CreateWorkItemWithTransitions(team, "To Do");
            existing.State = "To Do";
            existing.StateCategory = StateCategories.ToDo;
            workItems.Add(existing);

            var incoming = CreateIncomingFor(existing, "Doing", StateCategories.Doing);
            workTrackingConnectorMock.Setup(x => x.GetWorkItemsForTeam(team)).ReturnsAsync([incoming]);

            var subject = CreateSubject();
            await subject.UpdateWorkItemsForTeam(team);

            domainEventDispatcherMock.Verify(
                x => x.PublishAsync(It.IsAny<WorkItemBlocked>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task UpdateWorkItemsForTeam_UnblockTransition_PublishesWorkItemUnblockedWhenBlockedItemLeavesBlockedState()
        {
            var team = CreateTeam();
            team.BlockedStates.Add("Blocked");

            var existing = CreateWorkItemWithTransitions(team, "Blocked");
            existing.State = "Blocked";
            existing.StateCategory = StateCategories.Doing;
            workItems.Add(existing);

            var unblocked = CreateIncomingFor(existing, "Doing", StateCategories.Doing);
            workTrackingConnectorMock.Setup(x => x.GetWorkItemsForTeam(team)).ReturnsAsync([unblocked]);

            var subject = CreateSubject();
            await subject.UpdateWorkItemsForTeam(team);

            domainEventDispatcherMock.Verify(
                x => x.PublishAsync(
                    It.Is<WorkItemUnblocked>(e => e.WorkItemId == existing.Id),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            domainEventDispatcherMock.Verify(
                x => x.PublishAsync(It.IsAny<WorkItemBlocked>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static WorkItem CreateIncomingFor(WorkItem existing, string newState, StateCategories stateCategory, params (string fromState, string toState, DateTime transitionedAt)[] transitions)
        {
            var syncedTransitions = transitions
                .Select(t => new WorkItemStateTransition { FromState = t.fromState, ToState = t.toState, TransitionedAt = t.transitionedAt })
                .ToList();

            return new WorkItem
            {
                ReferenceId = existing.ReferenceId,
                State = newState,
                StateCategory = stateCategory,
                Team = existing.Team,
                TeamId = existing.TeamId,
                SyncedTransitions = syncedTransitions,
            };
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

        private WorkItem CreateWorkItemWithTransitions(Team team, string currentState, params (string fromState, string toState, DateTime transitionedAt)[] transitions)
        {
            var syncedTransitions = transitions
                .Select(t => new WorkItemStateTransition { FromState = t.fromState, ToState = t.toState, TransitionedAt = t.transitionedAt })
                .ToList();

            return new WorkItem
            {
                Id = idCounter++,
                ReferenceId = Guid.NewGuid().ToString(),
                State = currentState,
                StateCategory = StateCategories.Doing,
                Team = team,
                TeamId = team.Id,
                SyncedTransitions = syncedTransitions,
            };
        }

        private void AddWorkItemForTeam(Team team)
        {
            var workItem = new WorkItem
            {
                Id = idCounter++,
                ReferenceId = Guid.NewGuid().ToString(),
                StateCategory = StateCategories.ToDo,
                State = "To Do",
                Team = team,
                TeamId = team.Id
            };

            workItems.Add(workItem);
        }

        private Team CreateTeam()
        {
            var team = new Team { Name = "Team", Id = idCounter++ };

            team.WorkItemTypes.Add("User Story");

            var workTrackingConnection = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.Jira };
            team.WorkTrackingSystemConnection = workTrackingConnection;

            teams.Add(team);

            return team;
        }

        private static void AddAdditionalField(Portfolio portfolio, int id, string reference,
            string displayName)
        {
            portfolio.WorkTrackingSystemConnection.AdditionalFieldDefinitions.Add(
                new AdditionalFieldDefinition
                {
                    DisplayName = displayName,
                    Reference = reference,
                    Id = id,
                });
        }

        private Portfolio CreatePortfolio()
        {
            return CreatePortfolio(DateTime.Now);
        }

        private Portfolio CreatePortfolio(DateTime lastUpdateTime)
        {
            var portfolio = new Portfolio
            {
                Id = idCounter++,
                Name = "Release 1",
            };

            portfolio.WorkItemTypes.Add("Feature");

            var workTrackingConnection = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.Jira };
            portfolio.WorkTrackingSystemConnection = workTrackingConnection;

            portfolio.UpdateTime = lastUpdateTime;

            return portfolio;
        }

        private WorkItemService CreateSubject()
        {
            var workTrackingConnectorFactoryMock = new Mock<IWorkTrackingConnectorFactory>();
            workTrackingConnectorFactoryMock.Setup(x => x.GetWorkTrackingConnector(It.IsAny<WorkTrackingSystems>())).Returns(workTrackingConnectorMock.Object);

            return new WorkItemService(Mock.Of<ILogger<WorkItemService>>(), workTrackingConnectorFactoryMock.Object, featureRepositoryMock.Object, workItemRepositoryMock.Object, projectMetricsServiceMock.Object, teamRepositoryMock.Object, stateTransitionRepositoryMock.Object, featureStateTransitionRepositoryMock.Object, domainEventDispatcherMock.Object, new BlockedItemService(new RuleEvaluator<WorkItem>(), new WorkItemFieldProvider()));
        }
    }
}
