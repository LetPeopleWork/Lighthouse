using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.WorkItemRules;
using Lighthouse.Backend.Services.Implementation.WorkItems;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkItems
{
    public class WorkItemServiceTest
    {
        private static readonly ParentCandidate[] TwoIssuesNobodyHoldsAsAFeature =
            [new ParentCandidate("INC-5602", null), new ParentCandidate("INC-5610", null)];

        private const string TheStoredFeature = "FEAT-88";
        private const string TheLoneLink = "INC-5620";
        private const string TheTrackersOwnParent = "EPIC-7";
        private const string TheRecordUnderAFeatureAndAnIncident = "ITEM-1";
        private const string TheRecordUnderTwoIncidents = "ITEM-2";
        private const string TheRecordWithOneLink = "ITEM-3";
        private const string TheRecordWithNoLink = "ITEM-4";
        private const string TheRecordThatSatStill = "ITEM-5";
        private const int TheFieldThatNamesTheLinkType = 7;
        private const string TheFeatureUnderAFeatureAndAnIncident = "FTR-1";
        private const string TheFeatureUnderTwoIncidents = "FTR-2";
        private const string TheFeatureWithOneLink = "FTR-3";
        private const string TheFeatureWithNoLink = "FTR-4";
        private const string TheFeatureThatSatStill = "FTR-5";
        private const string TheIssueAbove = "INIT-1";

        private static readonly DateTime TheFirstReadingDay = new(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc);

        private static readonly ParentCandidate[] AStoredFeatureAndAnIncident =
            [new ParentCandidate(TheStoredFeature, null), new ParentCandidate("INC-5602", null)];

        private static readonly ParentCandidate[] OneIncident = [new ParentCandidate(TheLoneLink, null)];

        private static readonly string[] BothDependencyPhrases = ["depends on", "is depended on by"];

        private static readonly string[] TheLinksSeenWhereNothingMatched = ["is blocked by", "relates to"];

        private static readonly string[] EveryKindOfRecord =
            [TheRecordUnderAFeatureAndAnIncident, TheRecordUnderTwoIncidents, TheRecordWithOneLink, TheRecordWithNoLink];

        private static readonly string[] EveryKindOfFeature =
            [TheFeatureUnderAFeatureAndAnIncident, TheFeatureUnderTwoIncidents, TheFeatureWithOneLink, TheFeatureWithNoLink];

        private Mock<IFeatureRepository> featureRepositoryMock;
        private Mock<IWorkItemRepository> workItemRepositoryMock;
        private Mock<IWorkTrackingConnector> workTrackingConnectorMock;
        private Mock<IPortfolioMetricsService> projectMetricsServiceMock;
        private Mock<IRepository<Team>> teamRepositoryMock;
        private Mock<IWorkItemStateTransitionRepository> stateTransitionRepositoryMock;
        private Mock<IFeatureStateTransitionRepository> featureStateTransitionRepositoryMock;
        private Mock<IDomainEventDispatcher> domainEventDispatcherMock;
        private RecordingLogger<WorkItemService> whatTheServiceWrote;

        private int idCounter;
        private List<WorkItem> workItems;
        private List<Team> teams;
        private List<WorkItemStateTransition> storedTransitions;

        [SetUp]
        public void SetUp()
        {
            workTrackingConnectorMock = new Mock<IWorkTrackingConnector>();
            featureRepositoryMock = new Mock<IFeatureRepository>();
            workItemRepositoryMock = new Mock<IWorkItemRepository>();
            projectMetricsServiceMock = new Mock<IPortfolioMetricsService>();
            teamRepositoryMock = new Mock<IRepository<Team>>();
            stateTransitionRepositoryMock = new Mock<IWorkItemStateTransitionRepository>();
            featureStateTransitionRepositoryMock = new Mock<IFeatureStateTransitionRepository>();
            domainEventDispatcherMock = new Mock<IDomainEventDispatcher>();
            whatTheServiceWrote = new RecordingLogger<WorkItemService>();

            featureStateTransitionRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<FeatureStateTransition, bool>>>()))
                .Returns(new List<FeatureStateTransition>().AsQueryable());

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(It.IsAny<Portfolio>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new List<Feature>()));

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
            stateTransitionRepositoryMock.Setup(x => x.Remove(It.IsAny<WorkItemStateTransition?>()))
                .Callback((WorkItemStateTransition? transition) => storedTransitions.Remove(transition!));
        }

        [Test]
        public async Task UpdateFeaturesForProject_SingleTeamInvolved_FindsFeauture()
        {
            var team = CreateTeam();

            var project = CreatePortfolio();
            var feature = new Feature(team, 12) { ReferenceId = "12" };

            SetupWorkForFeature(feature, 1, 0, team);

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project, CancellationToken.None)).Returns(Task.FromResult(new List<Feature> { feature }));

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

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project, CancellationToken.None)).Returns(Task.FromResult(new List<Feature>()));

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

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project, CancellationToken.None)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));

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

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project, CancellationToken.None)).Returns(Task.FromResult(new List<Feature> { feature }));

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

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project, CancellationToken.None)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));

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

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project, CancellationToken.None)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));

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

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(portfolio, CancellationToken.None)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));

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

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project, CancellationToken.None)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));

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
        [TestCase(null)]
        [TestCase(0)]
        [TestCase(-5)]
        public async Task UpdateFeaturesForProject_UseCalculatedDefault_PercentileHistoryNotAPositiveLength_SamplesTheDefaultWindow(int? percentileHistoryInDays)
        {
            var team = CreateTeam();
            var project = CreatePortfolio();

            project.UsePercentileToCalculateDefaultAmountOfWorkItems = true;
            project.PercentileHistoryInDays = percentileHistoryInDays;

            var feature = new Feature(team, 0) { ReferenceId = "42" };
            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project, CancellationToken.None)).Returns(Task.FromResult(new List<Feature> { feature }));

            var sampledWindows = new List<TimeSpan>();
            projectMetricsServiceMock.Setup(x => x.GetCycleTimeDataForPortfolio(project, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Callback<Portfolio, DateTime, DateTime>((_, startDate, endDate) => sampledWindows.Add(endDate - startDate))
                .Returns(new List<Feature>());

            var subject = CreateSubject();
            await subject.UpdateFeaturesForPortfolio(project);

            Assert.That(sampledWindows, Has.Count.EqualTo(1));
            Assert.That(sampledWindows[0], Is.EqualTo(TimeSpan.FromDays(Portfolio.DefaultPercentileHistoryInDays)).Within(TimeSpan.FromSeconds(5)));
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

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project, CancellationToken.None)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));
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

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project, CancellationToken.None)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));

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

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project, CancellationToken.None)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));

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

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project, CancellationToken.None)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));

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

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project, CancellationToken.None)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));
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

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project, CancellationToken.None)).Returns(Task.FromResult(new List<Feature> { feature }));
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


            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(It.IsAny<Portfolio>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new List<Feature>()));

            var subject = CreateSubject();
            await subject.UpdateFeaturesForPortfolio(project);

            workTrackingConnectorMock.Verify(x => x.GetFeaturesForProject(project, CancellationToken.None), Times.Exactly(1));
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

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project, CancellationToken.None)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));

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

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project, CancellationToken.None)).Returns(Task.FromResult(new List<Feature> { feature }));

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

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project, CancellationToken.None)).Returns(Task.FromResult(new List<Feature> { feature }));
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

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project, CancellationToken.None)).Returns(Task.FromResult(new List<Feature> { feature }));
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

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(portfolio, CancellationToken.None)).Returns(Task.FromResult(new List<Feature> { feature }));
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

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(portfolio, CancellationToken.None)).Returns(Task.FromResult(new List<Feature> { feature }));
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

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(portfolio, CancellationToken.None)).Returns(Task.FromResult(new List<Feature> { feature }));

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

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(portfolio, CancellationToken.None)).Returns(Task.FromResult(new List<Feature> { feature }));

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

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(portfolio, CancellationToken.None)).Returns(Task.FromResult(new List<Feature> { feature }));

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

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(portfolio, CancellationToken.None)).Returns(Task.FromResult(new List<Feature> { feature }));

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

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project, CancellationToken.None)).Returns(Task.FromResult(new List<Feature> { feature }));
            workTrackingConnectorMock.Setup(x => x.GetParentFeaturesDetails(project, new[] { feature.ParentReferenceId }, CancellationToken.None)).ReturnsAsync(
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

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project, CancellationToken.None)).Returns(Task.FromResult(new List<Feature> { feature }));
            workTrackingConnectorMock.Setup(x => x.GetParentFeaturesDetails(project, new[] { feature.ParentReferenceId }, CancellationToken.None)).ReturnsAsync(
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

            workTrackingConnectorMock.Setup(x => x.GetWorkItemsForTeam(team, CancellationToken.None)).ReturnsAsync(workItems.ToList());

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

            workTrackingConnectorMock.Setup(x => x.GetWorkItemsForTeam(team, CancellationToken.None)).ReturnsAsync(workItems);

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

            workTrackingConnectorMock.Setup(x => x.GetWorkItemsForTeam(team, CancellationToken.None)).ReturnsAsync([]);

            var subject = CreateSubject();

            await subject.UpdateWorkItemsForTeam(team);

            workItemRepositoryMock.Verify(x => x.Remove(It.IsAny<int>()), Times.Exactly(2));
            workItemRepositoryMock.Verify(x => x.Add(It.IsAny<WorkItem>()), Times.Never);
            workItemRepositoryMock.Verify(x => x.Update(It.IsAny<WorkItem>()), Times.Never);
            workItemRepositoryMock.Verify(x => x.Save(), Times.AtLeastOnce);
        }

        [Test]
        public void UpdateWorkItemsForTeam_TrackerCouldNotBeAsked_RemovesNothing()
        {
            var team = CreateTeam();

            AddWorkItemForTeam(team);
            AddWorkItemForTeam(team);

            workTrackingConnectorMock.Setup(x => x.GetWorkItemsForTeam(team, CancellationToken.None))
                .ThrowsAsync(new InvalidOperationException("The tracker could not be asked."));

            var subject = CreateSubject();

            Assert.That(async () => await subject.UpdateWorkItemsForTeam(team),
                Throws.Exception,
                "A cycle that could not read the tracker has to fail rather than complete. One team's update "
                + "is skipped and retried; the alternative is a cycle that reports success having emptied it.");

            workItemRepositoryMock.Verify(x => x.Remove(It.IsAny<int>()), Times.Never,
                "Removal is a set difference against what the query returned. Run against a fetch that threw, "
                + "it deletes every Work Item the team has - and their blocked spells do not come back.");
        }

        /// <summary>
        /// A cheap refresh downloads only the records whose remote stamp moved, and it is the refresh an
        /// instance runs by default - so the count it reports has to be taken from that download. Reporting
        /// none would tell every operator on the cheap path that their tracker is in perfect order.
        ///
        /// The mode is asserted beside the count because without it this fixture could quietly fall back to
        /// the whole-query path and keep passing there, leaving the cheap path unread.
        /// </summary>
        [Test]
        public async Task UpdateWorkItemsForTeam_CheaperRefresh_CountsWhatItDownloadedAndCouldNotPlace()
        {
            const string theRecordThatMoved = "ITEM-1";
            const string theRecordThatSatStill = "ITEM-2";
            var whenEveryStoredRecordLastChanged = new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc);
            var whenTheRecordThatMovedChanged = whenEveryStoredRecordLastChanged.AddDays(30);

            var team = CreateTeam();
            team.FetchFingerprint = FetchFingerprint.For(team);

            AddStampedWorkItemForTeam(team, theRecordThatMoved, whenEveryStoredRecordLastChanged);
            AddStampedWorkItemForTeam(team, theRecordThatSatStill, whenEveryStoredRecordLastChanged);

            TheTrackerCanBeScanned();

            workTrackingConnectorMock
                .Setup(connector => connector.SweepWorkItemsForTeam(team, It.IsAny<CancellationToken>()))
                .ReturnsAsync([
                    new RemoteRecordStamp(theRecordThatMoved, whenTheRecordThatMovedChanged),
                    new RemoteRecordStamp(theRecordThatSatStill, whenEveryStoredRecordLastChanged)]);

            workTrackingConnectorMock
                .Setup(connector => connector.GetWorkItemsForTeam(team, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([ADownloadThatNobodyCouldPlace(theRecordThatMoved, whenTheRecordThatMovedChanged)]);

            featureRepositoryMock
                .Setup(repository => repository.GetStoredFeatureCreatedDates())
                .Returns(new StoredFeatures(new Dictionary<string, DateTime?>()));

            var subject = CreateSubject();

            var outcome = await subject.UpdateWorkItemsForTeam(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.Mode, Is.EqualTo(SyncMode.Delta),
                    "positive control: one stored record moved and the other did not, so this refresh has to be the cheap one - "
                    + "on the whole-query path the count below is already covered and proves nothing.");
                Assert.That(outcome.RecordsWhoseLinksNamedMoreThanOneParent, Is.EqualTo(1),
                    "The one record this refresh downloaded came back with links naming two parents, and a row that says none "
                    + "sends whoever reads refresh history looking for a tracker problem that is right in front of them.");
            }
        }

        /// <summary>
        /// A whole-query refresh places the records whose links named several issues, counts each as placed
        /// or not placed, and leaves every record with one link or none exactly as the tracker gave it, so a
        /// rule written against a lone link keeps matching. A record nobody could place keeps what the
        /// tracker said its parent is, never a guess from its links.
        /// </summary>
        [Test]
        public async Task UpdateWorkItemsForTeam_FullRefresh_PlacesAndCountsOnlyTheRecordsWhoseLinksNameSeveralIssues()
        {
            var team = CreateTeam();
            team.ParentOverrideAdditionalFieldDefinitionId = TheFieldThatNamesTheLinkType;

            workTrackingConnectorMock
                .Setup(connector => connector.GetWorkItemsForTeam(team, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ADownloadOfEveryKindOfRecord(TheFirstReadingDay));
            TheStoredFeaturesAre(TheStoredFeature);

            var subject = CreateSubject();

            var outcome = await subject.UpdateWorkItemsForTeam(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.Mode, Is.EqualTo(SyncMode.Full),
                    "positive control: this fixture is about the whole-query path, so it has to have taken it.");
                ThePlacingStepTouchedOnlyTheRecordsWhoseLinksNameSeveralIssues(outcome);
            }

            featureRepositoryMock.Verify(repository => repository.GetStoredFeatureCreatedDates(), Times.Once,
                "One read of the stored Features answers every record of the fetch; one per record would be a query per Work Item.");
        }

        /// <summary>
        /// The cheap refresh places what it downloaded in exactly the same way, and counts nothing it did
        /// not download: the record that sat still is neither placed nor counted.
        /// </summary>
        [Test]
        public async Task UpdateWorkItemsForTeam_CheaperRefresh_PlacesAndCountsOnlyTheRecordsItDownloaded()
        {
            var whenEveryStoredRecordLastChanged = TheFirstReadingDay.AddDays(-30);

            var team = CreateTeam();
            team.ParentOverrideAdditionalFieldDefinitionId = TheFieldThatNamesTheLinkType;
            team.FetchFingerprint = FetchFingerprint.For(team);

            foreach (var referenceId in EveryKindOfRecord)
            {
                AddStampedWorkItemForTeam(team, referenceId, whenEveryStoredRecordLastChanged);
            }

            AddStampedWorkItemForTeam(team, TheRecordThatSatStill, whenEveryStoredRecordLastChanged);

            TheTrackerCanBeScanned();

            workTrackingConnectorMock
                .Setup(connector => connector.SweepWorkItemsForTeam(team, It.IsAny<CancellationToken>()))
                .ReturnsAsync([
                    .. EveryKindOfRecord.Select(referenceId => new RemoteRecordStamp(referenceId, TheFirstReadingDay)),
                    new RemoteRecordStamp(TheRecordThatSatStill, whenEveryStoredRecordLastChanged)]);

            workTrackingConnectorMock
                .Setup(connector => connector.GetWorkItemsForTeam(team, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ADownloadOfEveryKindOfRecord(TheFirstReadingDay));
            TheStoredFeaturesAre(TheStoredFeature);

            var subject = CreateSubject();

            var outcome = await subject.UpdateWorkItemsForTeam(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.Mode, Is.EqualTo(SyncMode.Delta),
                    "positive control: four stored records moved and one did not, so this refresh has to be the cheap one.");
                ThePlacingStepTouchedOnlyTheRecordsWhoseLinksNameSeveralIssues(outcome);
                Assert.That(WhatTheStoredRecordShows(TheRecordThatSatStill), Is.EqualTo((string.Empty, (string?)null)),
                    "A record the refresh did not download is not read, so nothing about it can have changed.");
            }

            featureRepositoryMock.Verify(repository => repository.GetStoredFeatureCreatedDates(), Times.Once,
                "One read of the stored Features answers every record the cheap refresh downloaded.");
        }

        /// <summary>
        /// A refresh where every link named one issue has no question to answer, so it does not ask the
        /// database which Features it holds.
        /// </summary>
        [Test]
        public async Task UpdateWorkItemsForTeam_NoRecordWhoseLinksNameSeveralIssues_NeverReadsTheStoredFeatures()
        {
            var team = CreateTeam();
            team.ParentOverrideAdditionalFieldDefinitionId = TheFieldThatNamesTheLinkType;

            workTrackingConnectorMock
                .Setup(connector => connector.GetWorkItemsForTeam(team, It.IsAny<CancellationToken>()))
                .ReturnsAsync([
                    ADownload(TheRecordWithOneLink, TheLoneLink, OneIncident, TheLoneLink, TheFirstReadingDay),
                    ADownload(TheRecordWithNoLink, TheTrackersOwnParent, [], string.Empty, TheFirstReadingDay)]);

            var subject = CreateSubject();

            var outcome = await subject.UpdateWorkItemsForTeam(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.RecordsPlacedFromLinksNamingMoreThanOneParent, Is.Zero);
                Assert.That(outcome.RecordsWhoseLinksNamedMoreThanOneParent, Is.Zero);
            }

            featureRepositoryMock.Verify(repository => repository.GetStoredFeatureCreatedDates(), Times.Never,
                "Nothing needed placing, so a read of every stored Feature would be a query spent on no question.");
        }

        private void ThePlacingStepTouchedOnlyTheRecordsWhoseLinksNameSeveralIssues(SyncOutcome outcome)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.RecordsPlacedFromLinksNamingMoreThanOneParent, Is.EqualTo(1),
                    "One downloaded record named a stored Feature and an incident, so one was placed.");
                Assert.That(outcome.RecordsWhoseLinksNamedMoreThanOneParent, Is.EqualTo(1),
                    "One downloaded record named two incidents and no stored Feature, so one could not be placed.");
                Assert.That(WhatTheStoredRecordShows(TheRecordUnderAFeatureAndAnIncident), Is.EqualTo((TheStoredFeature, (string?)TheStoredFeature)),
                    "The placed record hangs under the Feature, and the Additional Field shows the key that was used.");
                Assert.That(WhatTheStoredRecordShows(TheRecordUnderTwoIncidents), Is.EqualTo((TheTrackersOwnParent, (string?)string.Empty)),
                    "A record nobody could place keeps the tracker's own parent and shows nothing, exactly as the tracker gave it.");
                Assert.That(WhatTheStoredRecordShows(TheRecordWithOneLink), Is.EqualTo((TheLoneLink, (string?)TheLoneLink)),
                    "A lone link is not a question: the record keeps exactly what the tracker gave it.");
                Assert.That(WhatTheStoredRecordShows(TheRecordWithNoLink), Is.EqualTo((TheTrackersOwnParent, (string?)string.Empty)),
                    "A record with no link is not a question: the record keeps exactly what the tracker gave it.");
            }
        }

        private (string Parent, string? Shown) WhatTheStoredRecordShows(string referenceId)
        {
            var stored = workItems.Single(workItem => workItem.ReferenceId == referenceId);

            return (stored.ParentReferenceId, stored.AdditionalFieldValues.GetValueOrDefault(TheFieldThatNamesTheLinkType));
        }

        private void TheStoredFeaturesAre(string referenceId)
            => featureRepositoryMock
                .Setup(repository => repository.GetStoredFeatureCreatedDates())
                .Returns(new StoredFeatures(new Dictionary<string, DateTime?> { [referenceId] = TheFirstReadingDay.AddYears(-1) }));

        private static List<WorkItem> ADownloadOfEveryKindOfRecord(DateTime lastChangedRemote)
            =>
            [
                ADownload(TheRecordUnderAFeatureAndAnIncident, TheTrackersOwnParent, AStoredFeatureAndAnIncident, string.Empty, lastChangedRemote),
                ADownload(TheRecordUnderTwoIncidents, TheTrackersOwnParent, TwoIssuesNobodyHoldsAsAFeature, string.Empty, lastChangedRemote),
                ADownload(TheRecordWithOneLink, TheLoneLink, OneIncident, TheLoneLink, lastChangedRemote),
                ADownload(TheRecordWithNoLink, TheTrackersOwnParent, [], string.Empty, lastChangedRemote),
            ];

        /// <summary>
        /// What a connector hands back: the parent the tracker reports, every issue the links named, and what
        /// the Additional Field shows. For a lone link the connector already hung the record under it.
        /// </summary>
        private static WorkItem ADownload(string referenceId, string trackersOwnParent, ParentCandidate[] candidates, string shown, DateTime lastChangedRemote)
            => new()
            {
                ReferenceId = referenceId,
                State = "To Do",
                StateCategory = StateCategories.ToDo,
                LastChangedRemote = lastChangedRemote,
                ParentReferenceId = trackersOwnParent,
                ParentCandidatesFromLinks = candidates,
                AdditionalFieldValues = { [TheFieldThatNamesTheLinkType] = shown },
            };

        /// <summary>
        /// The portfolio half of the same thing, through its own door. Nothing is set aside at this level:
        /// the issues above a Feature are stored only once a refresh has read them, so setting aside what is
        /// not stored would throw away every parent the first time it is seen. The pick always ends
        /// somewhere: two incidents nobody stores fall to the lowest key. Its refresh history is a page of its
        /// own, read by people who never open a Team, so the placement is counted on its row.
        /// </summary>
        [Test]
        public async Task UpdateFeaturesForPortfolio_FullRefresh_PlacesAndCountsOnlyTheFeaturesWhoseLinksNameSeveralIssues()
        {
            var portfolio = CreatePortfolio();
            portfolio.ParentOverrideAdditionalFieldDefinitionId = TheFieldThatNamesTheLinkType;
            var stored = TheFeaturesTheRefreshStores();

            workTrackingConnectorMock
                .Setup(connector => connector.GetFeaturesForProject(portfolio, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ADownloadOfEveryKindOfFeature(TheFirstReadingDay));
            TheStoredFeaturesAre(TheStoredFeature);

            NothingAboveTheFeaturesIsDownloaded();

            var subject = CreateSubject();

            var outcome = await subject.UpdateFeaturesForPortfolio(portfolio);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.Mode, Is.EqualTo(SyncMode.Full),
                    "positive control: this fixture is about the whole-query path, so it has to have taken it.");
                ThePortfolioPlacedEveryFeatureWhoseLinksNameSeveralIssues(outcome, stored);
            }

            featureRepositoryMock.Verify(repository => repository.GetStoredFeatureCreatedDates(), Times.Once,
                "One read of the stored Features answers every Feature of the fetch; one per Feature would be a query per record.");
        }

        /// <summary>
        /// The cheap refresh places what it downloaded in exactly the same way, and counts nothing it did not
        /// download. It is the refresh an instance runs by default, so a count taken anywhere else would tell
        /// every operator on the cheap path nothing at all.
        /// </summary>
        [Test]
        public async Task UpdateFeaturesForPortfolio_CheaperRefresh_PlacesAndCountsOnlyTheFeaturesItDownloaded()
        {
            var whenEveryStoredRecordLastChanged = TheFirstReadingDay.AddDays(-30);

            var portfolio = CreatePortfolio();
            portfolio.ParentOverrideAdditionalFieldDefinitionId = TheFieldThatNamesTheLinkType;
            portfolio.FetchFingerprint = FetchFingerprint.For(portfolio);
            portfolio.UpdateFeatures([
                .. EveryKindOfFeature.Select(referenceId => AStampedFeature(referenceId, whenEveryStoredRecordLastChanged)),
                AStampedFeature(TheFeatureThatSatStill, whenEveryStoredRecordLastChanged)]);
            var stored = TheFeaturesTheRefreshStores();

            TheTrackerCanBeScanned();

            workTrackingConnectorMock
                .Setup(connector => connector.SweepFeaturesForPortfolio(portfolio, It.IsAny<CancellationToken>()))
                .ReturnsAsync([
                    .. EveryKindOfFeature.Select(referenceId => new RemoteRecordStamp(referenceId, TheFirstReadingDay)),
                    new RemoteRecordStamp(TheFeatureThatSatStill, whenEveryStoredRecordLastChanged)]);

            workTrackingConnectorMock
                .Setup(connector => connector.GetFeaturesForProject(portfolio, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ADownloadOfEveryKindOfFeature(TheFirstReadingDay));
            TheStoredFeaturesAre(TheStoredFeature);

            NothingAboveTheFeaturesIsDownloaded();

            var subject = CreateSubject();

            var outcome = await subject.UpdateFeaturesForPortfolio(portfolio);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.Mode, Is.EqualTo(SyncMode.Delta),
                    "positive control: three stored Features moved and one did not, so this refresh has to be the cheap one.");
                ThePortfolioPlacedEveryFeatureWhoseLinksNameSeveralIssues(outcome, stored);
                Assert.That(stored.Select(feature => feature.ReferenceId), Does.Not.Contain(TheFeatureThatSatStill),
                    "A Feature the refresh did not download is not read, so nothing about it can have changed.");
            }

            featureRepositoryMock.Verify(repository => repository.GetStoredFeatureCreatedDates(), Times.Once,
                "One read of the stored Features answers every Feature the cheap refresh downloaded.");
        }

        /// <summary>
        /// A Portfolio refresh where every link named one issue has no question to answer, so it does not ask
        /// the database which Features it holds.
        /// </summary>
        [Test]
        public async Task UpdateFeaturesForPortfolio_NoFeatureWhoseLinksNameSeveralIssues_NeverReadsTheStoredFeatures()
        {
            var portfolio = CreatePortfolio();
            portfolio.ParentOverrideAdditionalFieldDefinitionId = TheFieldThatNamesTheLinkType;

            workTrackingConnectorMock
                .Setup(connector => connector.GetFeaturesForProject(portfolio, It.IsAny<CancellationToken>()))
                .ReturnsAsync([
                    ADownloadedFeature(TheFeatureWithOneLink, TheLoneLink, OneIncident, TheLoneLink, TheFirstReadingDay),
                    ADownloadedFeature(TheFeatureWithNoLink, TheTrackersOwnParent, [], string.Empty, TheFirstReadingDay)]);

            NothingAboveTheFeaturesIsDownloaded();

            var subject = CreateSubject();

            var outcome = await subject.UpdateFeaturesForPortfolio(portfolio);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.RecordsPlacedFromLinksNamingMoreThanOneParent, Is.Zero);
                Assert.That(outcome.RecordsWhoseLinksNamedMoreThanOneParent, Is.Zero);
            }

            featureRepositoryMock.Verify(repository => repository.GetStoredFeatureCreatedDates(), Times.Never,
                "Nothing needed placing, so a read of every stored Feature would be a query spent on no question.");
        }

        /// <summary>
        /// Paging over an unordered query can hand the same Feature over twice in one full download. It is
        /// one Feature, so it is placed, counted, named and stored once.
        /// </summary>
        [Test]
        public async Task UpdateFeaturesForPortfolio_FullRefreshSendsAFeatureTwice_PlacesCountsAndStoresItOnce()
        {
            var portfolio = CreatePortfolio();
            portfolio.ParentOverrideAdditionalFieldDefinitionId = TheFieldThatNamesTheLinkType;
            var stored = TheFeaturesTheRefreshStores();

            workTrackingConnectorMock
                .Setup(connector => connector.GetFeaturesForProject(portfolio, It.IsAny<CancellationToken>()))
                .ReturnsAsync([
                    ADownloadedFeature(TheFeatureUnderTwoIncidents, TheTrackersOwnParent, TwoIssuesNobodyHoldsAsAFeature, string.Empty, TheFirstReadingDay),
                    ADownloadedFeature(TheFeatureUnderTwoIncidents, TheTrackersOwnParent, TwoIssuesNobodyHoldsAsAFeature, string.Empty, TheFirstReadingDay)]);
            TheStoredFeaturesAre(TheStoredFeature);

            NothingAboveTheFeaturesIsDownloaded();

            var subject = CreateSubject();

            var outcome = await subject.UpdateFeaturesForPortfolio(portfolio);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.Mode, Is.EqualTo(SyncMode.Full),
                    "positive control: the duplicate arrives on the whole-query path, so it has to have taken it.");
                Assert.That(outcome.RecordsPlacedFromLinksNamingMoreThanOneParent, Is.EqualTo(1),
                    "One Feature was placed, however many copies of it the tracker sent.");
                Assert.That(stored.FindAll(feature => feature.ReferenceId == TheFeatureUnderTwoIncidents), Has.Count.EqualTo(1),
                    "It is one Feature, so it is stored once.");
                Assert.That(TimesTheLinesName(TheFeatureUnderTwoIncidents), Is.EqualTo(1),
                    "The line names it once, so it agrees with the count beside it.");
            }
        }

        private int TimesTheLinesName(string referenceId)
            => whatTheServiceWrote.Warnings.Sum(line => line.Split($"{referenceId} under ", StringSplitOptions.None).Length - 1);

        /// <summary>
        /// A Dependency Field naming a link type, or a phrase both its ends share, cannot say which end waits,
        /// so every Feature the refresh reads shows the same misconfiguration. The administrator is told once
        /// for the whole refresh, with the two phrases they have to choose between.
        /// </summary>
        [TestCase("Dependency", "depends on", "is depended on by")]
        [TestCase("relates to", "relates to", "relates to")]
        public async Task UpdateFeaturesForPortfolio_FullRefreshWhereTheDependencyFieldPointsBothWays_RecordsOneFindingAndOneWarning(
            string named, string inward, string outward)
        {
            var portfolio = CreatePortfolio();
            string[] bothPhrases = [inward, outward];

            workTrackingConnectorMock
                .Setup(connector => connector.GetFeaturesForProject(portfolio, It.IsAny<CancellationToken>()))
                .ReturnsAsync([.. EveryKindOfFeature.Select(referenceId => AFeatureWhoseDependencyFieldPointsBothWays(referenceId, named, bothPhrases, TheFirstReadingDay))]);

            NothingAboveTheFeaturesIsDownloaded();

            var subject = CreateSubject();

            var outcome = await subject.UpdateFeaturesForPortfolio(portfolio);
            var theFinding = outcome.Findings.Count > 0 ? outcome.Findings[0] : null;
            var linesNamingIt = whatTheServiceWrote.Warnings.Where(line => line.Contains($"'{named}'", StringComparison.Ordinal)).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.Mode, Is.EqualTo(SyncMode.Full), "positive control: only the whole-query refresh judges a setting.");
                Assert.That(outcome.Findings, Has.Count.EqualTo(1), "One finding per refresh, never one per Feature.");
                Assert.That(theFinding?.Kind, Is.EqualTo(RefreshFindingKind.NamedLinkPointsBothWays));
                Assert.That(theFinding?.Setting, Is.EqualTo(RefreshFindingSetting.DependencyField));
                Assert.That(theFinding?.Named, Is.EqualTo(named));
                Assert.That(theFinding?.Seen, Is.EqualTo(bothPhrases));
                Assert.That(linesNamingIt, Has.Count.EqualTo(1), "One warning line for the whole refresh, never one per Feature.");
                Assert.That(linesNamingIt.FirstOrDefault() ?? string.Empty, Does.Contain(inward).And.Contain(outward));
            }
        }

        /// <summary>
        /// The line is read by an administrator, so it names the setting as the screen does and says in a
        /// plain sentence what is wrong and what to type instead.
        /// </summary>
        [Test]
        public async Task UpdateFeaturesForPortfolio_FullRefreshWhereTheDependencyFieldPointsBothWays_WarnsInPlainLanguage()
        {
            var portfolio = CreatePortfolio();

            workTrackingConnectorMock
                .Setup(connector => connector.GetFeaturesForProject(portfolio, It.IsAny<CancellationToken>()))
                .ReturnsAsync([AFeatureWhoseDependencyFieldPointsBothWays("FTR-1", "Dependency", BothDependencyPhrases, TheFirstReadingDay)]);

            NothingAboveTheFeaturesIsDownloaded();

            await CreateSubject().UpdateFeaturesForPortfolio(portfolio);

            Assert.That(whatTheServiceWrote.Warnings, Has.One.EqualTo(
                $"Portfolio {portfolio.Name}: its Dependency Field names 'Dependency', which points both ways. "
                + "Name one end: 'depends on' or 'is depended on by'."));
        }

        [Test]
        public async Task UpdateFeaturesForPortfolio_FullRefreshWhereTheNamedLinkMatchedNothing_WarnsInPlainLanguage()
        {
            var portfolio = CreatePortfolio();

            workTrackingConnectorMock
                .Setup(connector => connector.GetFeaturesForProject(portfolio, It.IsAny<CancellationToken>()))
                .ReturnsAsync([new Feature
                {
                    ReferenceId = "FTR-1",
                    LastChangedRemote = TheFirstReadingDay,
                    RefreshFindings = [new RefreshFinding(RefreshFindingKind.NamedLinkMatchedNothing, RefreshFindingSetting.DependencyField, "depends on", TheLinksSeenWhereNothingMatched)],
                }]);

            NothingAboveTheFeaturesIsDownloaded();

            await CreateSubject().UpdateFeaturesForPortfolio(portfolio);

            Assert.That(whatTheServiceWrote.Warnings, Has.One.EqualTo(
                $"Portfolio {portfolio.Name}: its Dependency Field names 'depends on', and no Feature carries that link. "
                + "Links seen: is blocked by, relates to."));
        }

        /// <summary>
        /// Each finding is worded once. A line for a named link that matched nothing is not followed by the
        /// catch-all line the kinds without a sentence of their own get.
        /// </summary>
        [Test]
        public async Task UpdateFeaturesForPortfolio_FullRefreshWhereTheNamedLinkMatchedNothing_WritesOnlyThatOneLine()
        {
            var portfolio = CreatePortfolio();

            workTrackingConnectorMock
                .Setup(connector => connector.GetFeaturesForProject(portfolio, It.IsAny<CancellationToken>()))
                .ReturnsAsync([new Feature
                {
                    ReferenceId = "FTR-1",
                    LastChangedRemote = TheFirstReadingDay,
                    RefreshFindings = [new RefreshFinding(RefreshFindingKind.NamedLinkMatchedNothing, RefreshFindingSetting.DependencyField, "depends on", TheLinksSeenWhereNothingMatched)],
                }]);

            NothingAboveTheFeaturesIsDownloaded();

            await CreateSubject().UpdateFeaturesForPortfolio(portfolio);

            Assert.That(whatTheServiceWrote.Warnings.Where(line => line.Contains("'depends on'", StringComparison.Ordinal)), Has.Exactly(1).Items);
        }

        /// <summary>
        /// A kind of finding with no sentence of its own still reaches the log, naming the setting, what was
        /// typed into it and what the refresh saw instead.
        /// </summary>
        [Test]
        public async Task UpdateFeaturesForPortfolio_FullRefreshWithAFindingOfAnotherKind_WarnsWhatTheRefreshSaw()
        {
            var portfolio = CreatePortfolio();

            workTrackingConnectorMock
                .Setup(connector => connector.GetFeaturesForProject(portfolio, It.IsAny<CancellationToken>()))
                .ReturnsAsync([new Feature
                {
                    ReferenceId = "FTR-1",
                    LastChangedRemote = TheFirstReadingDay,
                    RefreshFindings = [new RefreshFinding(RefreshFindingKind.RelationTypeCannotBeReadHere, RefreshFindingSetting.ParentOverrideField, "Contoso.DependsOn", BothDependencyPhrases)],
                }]);

            NothingAboveTheFeaturesIsDownloaded();

            await CreateSubject().UpdateFeaturesForPortfolio(portfolio);

            Assert.That(whatTheServiceWrote.Warnings, Has.One.EqualTo(
                $"Portfolio {portfolio.Name}: its Parent Override Field names 'Contoso.DependsOn', which the refresh could not read as configured. "
                + "What it saw: 'depends on', 'is depended on by'"));
        }

        /// <summary>
        /// A Portfolio that ignores dependencies does not read its Dependency Field at all, so whatever that
        /// field matched or missed is nothing its administrator needs to act on.
        /// </summary>
        [Test]
        public async Task UpdateFeaturesForPortfolio_PortfolioIgnoringDependencies_RecordsAndWarnsNoDependencyFieldFinding()
        {
            var portfolio = CreatePortfolio();
            portfolio.IgnoreDependencies = true;

            workTrackingConnectorMock
                .Setup(connector => connector.GetFeaturesForProject(portfolio, It.IsAny<CancellationToken>()))
                .ReturnsAsync([new Feature
                {
                    ReferenceId = "FTR-1",
                    LastChangedRemote = TheFirstReadingDay,
                    RefreshFindings = [new RefreshFinding(RefreshFindingKind.NamedLinkMatchedNothing, RefreshFindingSetting.DependencyField, "depends on", TheLinksSeenWhereNothingMatched)],
                }]);

            NothingAboveTheFeaturesIsDownloaded();

            var outcome = await CreateSubject().UpdateFeaturesForPortfolio(portfolio);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.Findings, Is.Empty);
                Assert.That(whatTheServiceWrote.Warnings, Has.None.Contains("Dependency Field"));
            }
        }

        /// <summary>
        /// The same misconfiguration in two different settings is two things for the administrator to fix,
        /// so each setting keeps its own finding.
        /// </summary>
        [Test]
        public async Task UpdateFeaturesForPortfolio_TwoSettingsShowTheSameKindOfFinding_RecordsOneFindingForEachSetting()
        {
            var portfolio = CreatePortfolio();
            var inTheDependencyField = new RefreshFinding(RefreshFindingKind.NamedLinkPointsBothWays, RefreshFindingSetting.DependencyField, "Dependency", BothDependencyPhrases);
            var inTheParentOverrideField = new RefreshFinding(RefreshFindingKind.NamedLinkPointsBothWays, RefreshFindingSetting.ParentOverrideField, "Dependency", BothDependencyPhrases);

            workTrackingConnectorMock
                .Setup(connector => connector.GetFeaturesForProject(portfolio, It.IsAny<CancellationToken>()))
                .ReturnsAsync([.. EveryKindOfFeature.Select(referenceId => new Feature
                {
                    ReferenceId = referenceId,
                    LastChangedRemote = TheFirstReadingDay,
                    RefreshFindings = [inTheDependencyField, inTheParentOverrideField],
                })]);

            NothingAboveTheFeaturesIsDownloaded();

            var outcome = await CreateSubject().UpdateFeaturesForPortfolio(portfolio);

            Assert.That(outcome.Findings.Select(finding => finding.Setting),
                Is.EquivalentTo(new List<RefreshFindingSetting> { RefreshFindingSetting.DependencyField, RefreshFindingSetting.ParentOverrideField }));
        }

        /// <summary>
        /// A cheap refresh reads only the Features that moved, which says nothing about how the Portfolio is
        /// configured, so it records no finding and says nothing - the last whole-query refresh's word stands.
        /// </summary>
        [Test]
        public async Task UpdateFeaturesForPortfolio_CheaperRefreshWhereTheDependencyFieldPointsBothWays_RecordsNoFinding()
        {
            var whenEveryStoredRecordLastChanged = TheFirstReadingDay.AddDays(-30);

            var portfolio = CreatePortfolio();
            portfolio.FetchFingerprint = FetchFingerprint.For(portfolio);
            portfolio.UpdateFeatures([.. EveryKindOfFeature.Select(referenceId => AStampedFeature(referenceId, whenEveryStoredRecordLastChanged))]);

            TheTrackerCanBeScanned();

            workTrackingConnectorMock
                .Setup(connector => connector.SweepFeaturesForPortfolio(portfolio, It.IsAny<CancellationToken>()))
                .ReturnsAsync([.. EveryKindOfFeature.Select(referenceId => new RemoteRecordStamp(referenceId, TheFirstReadingDay))]);

            workTrackingConnectorMock
                .Setup(connector => connector.GetFeaturesForProject(portfolio, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([.. EveryKindOfFeature.Select(referenceId => AFeatureWhoseDependencyFieldPointsBothWays(referenceId, "Dependency", BothDependencyPhrases, TheFirstReadingDay))]);

            NothingAboveTheFeaturesIsDownloaded();

            var subject = CreateSubject();

            var outcome = await subject.UpdateFeaturesForPortfolio(portfolio);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.Mode, Is.EqualTo(SyncMode.Delta), "positive control: every stored Feature moved, so this refresh has to be the cheap one.");
                Assert.That(outcome.Findings, Is.Empty);
                Assert.That(whatTheServiceWrote.Warnings, Has.None.Contains("'Dependency'"));
            }
        }

        private static Feature AFeatureWhoseDependencyFieldPointsBothWays(string referenceId, string named, string[] seen, DateTime lastChangedRemote)
            => new()
            {
                ReferenceId = referenceId,
                LastChangedRemote = lastChangedRemote,
                RefreshFindings = [new RefreshFinding(RefreshFindingKind.NamedLinkPointsBothWays, RefreshFindingSetting.DependencyField, named, seen)],
            };

        private static void ThePortfolioPlacedEveryFeatureWhoseLinksNameSeveralIssues(SyncOutcome outcome, List<Feature> stored)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.RecordsPlacedFromLinksNamingMoreThanOneParent, Is.EqualTo(2),
                    "Two downloaded Features named several issues, and at this level the pick always ends somewhere, so both were placed.");
                Assert.That(outcome.RecordsWhoseLinksNamedMoreThanOneParent, Is.Zero,
                    "Nothing is set aside first at this level, so no Feature is left not placed.");
                Assert.That(WhatTheStoredFeatureShows(stored, TheFeatureUnderAFeatureAndAnIncident), Is.EqualTo((TheStoredFeature, (string?)TheStoredFeature)),
                    "The stored Feature has a created date and the incident does not, so the stored one wins, and the Additional Field shows the key that was used.");
                Assert.That(WhatTheStoredFeatureShows(stored, TheFeatureUnderTwoIncidents), Is.EqualTo(("INC-5602", (string?)"INC-5602")),
                    "Two incidents nobody stores fall to the lowest key, rather than being refused as a Team refuses them.");
                Assert.That(WhatTheStoredFeatureShows(stored, TheFeatureWithOneLink), Is.EqualTo((TheLoneLink, (string?)TheLoneLink)),
                    "A lone link is not a question: the Feature keeps exactly what the tracker gave it.");
                Assert.That(WhatTheStoredFeatureShows(stored, TheFeatureWithNoLink), Is.EqualTo((TheTrackersOwnParent, (string?)string.Empty)),
                    "A Feature with no link is not a question: it keeps exactly what the tracker gave it.");
            }
        }

        private static (string Parent, string? Shown) WhatTheStoredFeatureShows(List<Feature> stored, string referenceId)
        {
            var feature = stored.Single(candidate => candidate.ReferenceId == referenceId);

            return (feature.ParentReferenceId, feature.AdditionalFieldValues.GetValueOrDefault(TheFieldThatNamesTheLinkType));
        }

        private void NothingAboveTheFeaturesIsDownloaded()
        {
            workTrackingConnectorMock
                .Setup(connector => connector.SweepParentFeatures(It.IsAny<Portfolio>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            workTrackingConnectorMock
                .Setup(connector => connector.GetParentFeaturesDetails(It.IsAny<Portfolio>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
        }

        private List<Feature> TheFeaturesTheRefreshStores()
        {
            var stored = new List<Feature>();
            featureRepositoryMock
                .Setup(repository => repository.Add(It.IsAny<Feature>()))
                .Callback((Feature feature) => stored.Add(feature));

            return stored;
        }

        private static List<Feature> ADownloadOfEveryKindOfFeature(DateTime lastChangedRemote)
            =>
            [
                ADownloadedFeature(TheFeatureUnderAFeatureAndAnIncident, TheTrackersOwnParent, AStoredFeatureAndAnIncident, string.Empty, lastChangedRemote),
                ADownloadedFeature(TheFeatureUnderTwoIncidents, TheTrackersOwnParent, TwoIssuesNobodyHoldsAsAFeature, string.Empty, lastChangedRemote),
                ADownloadedFeature(TheFeatureWithOneLink, TheLoneLink, OneIncident, TheLoneLink, lastChangedRemote),
                ADownloadedFeature(TheFeatureWithNoLink, TheTrackersOwnParent, [], string.Empty, lastChangedRemote),
            ];

        private static Feature ADownloadedFeature(string referenceId, string trackersOwnParent, ParentCandidate[] candidates, string shown, DateTime lastChangedRemote)
            => new()
            {
                ReferenceId = referenceId,
                LastChangedRemote = lastChangedRemote,
                ParentReferenceId = trackersOwnParent,
                ParentCandidatesFromLinks = candidates,
                AdditionalFieldValues = { [TheFieldThatNamesTheLinkType] = shown },
            };

        /// <summary>
        /// The issues above a Portfolio's Features are downloaded in a sweep of their own, and their links can
        /// name several issues too, so they are placed by the same rule as the Features. They are not counted:
        /// the Portfolio's row counts its own Features. A tracker that sends the same issue twice in one
        /// download gets it placed and stored once.
        /// </summary>
        [Test]
        public async Task UpdateFeaturesForPortfolio_FullRefresh_PlacesTheIssuesAboveOnceAndCountsNoneOfThem()
        {
            var portfolio = CreatePortfolio();
            portfolio.ParentOverrideAdditionalFieldDefinitionId = TheFieldThatNamesTheLinkType;
            var stored = TheFeaturesTheRefreshStores();

            workTrackingConnectorMock
                .Setup(connector => connector.GetFeaturesForProject(portfolio, It.IsAny<CancellationToken>()))
                .ReturnsAsync([AFeatureUnderTheIssueAbove(TheFirstReadingDay)]);
            workTrackingConnectorMock
                .Setup(connector => connector.GetParentFeaturesDetails(portfolio, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([TheIssueAboveAsDownloaded(), TheIssueAboveAsDownloaded()]);
            TheStoredFeaturesAre(TheStoredFeature);

            var subject = CreateSubject();

            var outcome = await subject.UpdateFeaturesForPortfolio(portfolio);

            var storedIssuesAbove = stored.FindAll(feature => feature.ReferenceId == TheIssueAbove);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.Mode, Is.EqualTo(SyncMode.Full),
                    "positive control: this fixture is about the whole-query path, so it has to have taken it.");
                Assert.That(storedIssuesAbove, Has.Count.EqualTo(1),
                    "The tracker sent the issue above twice; it is one issue, so it is stored once.");
                Assert.That(TimesTheLinesName(TheIssueAbove), Is.EqualTo(1),
                    "The copies are dropped before anything is placed, so the line names the issue above once.");
                TheIssueAboveWasPlacedAndNotCounted(outcome, storedIssuesAbove[0]);
            }

            featureRepositoryMock.Verify(repository => repository.GetStoredFeatureCreatedDates(), Times.Once,
                "Only the issue above needed placing, so the stored Features are read once, for it.");
        }

        /// <summary>
        /// The cheap refresh downloads only the issues above that moved, and places them in the same way.
        /// </summary>
        [Test]
        public async Task UpdateFeaturesForPortfolio_CheaperRefresh_PlacesTheIssuesAboveItDownloadedAndCountsNoneOfThem()
        {
            var whenEveryStoredRecordLastChanged = TheFirstReadingDay.AddDays(-30);

            var portfolio = CreatePortfolio();
            portfolio.ParentOverrideAdditionalFieldDefinitionId = TheFieldThatNamesTheLinkType;
            portfolio.FetchFingerprint = FetchFingerprint.For(portfolio);
            var theStoredFeature = AFeatureUnderTheIssueAbove(whenEveryStoredRecordLastChanged);
            portfolio.UpdateFeatures([theStoredFeature]);
            var theStoredIssueAbove = AStampedFeature(TheIssueAbove, whenEveryStoredRecordLastChanged);
            theStoredIssueAbove.IsParentFeature = true;
            List<Feature> everythingStored = [theStoredFeature, theStoredIssueAbove];
            featureRepositoryMock
                .Setup(repository => repository.GetByPredicate(It.IsAny<Func<Feature, bool>>()))
                .Returns((Func<Feature, bool> predicate) => everythingStored.SingleOrDefault(predicate));

            TheTrackerCanBeScanned();

            workTrackingConnectorMock
                .Setup(connector => connector.SweepFeaturesForPortfolio(portfolio, It.IsAny<CancellationToken>()))
                .ReturnsAsync([new RemoteRecordStamp(TheFeatureWithOneLink, whenEveryStoredRecordLastChanged)]);
            workTrackingConnectorMock
                .Setup(connector => connector.SweepParentFeatures(portfolio, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([new RemoteRecordStamp(TheIssueAbove, TheFirstReadingDay)]);
            workTrackingConnectorMock
                .Setup(connector => connector.GetParentFeaturesDetails(portfolio, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([TheIssueAboveAsDownloaded()]);
            TheStoredFeaturesAre(TheStoredFeature);

            var subject = CreateSubject();

            var outcome = await subject.UpdateFeaturesForPortfolio(portfolio);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.Mode, Is.EqualTo(SyncMode.Delta),
                    "positive control: nothing about the fetch changed and the tracker could be scanned, so this has to be the cheap refresh.");
                Assert.That(theStoredIssueAbove.LastChangedRemote, Is.EqualTo(TheFirstReadingDay),
                    "positive control: the issue above moved, so the cheap refresh downloaded it and updated the stored one.");
                TheIssueAboveWasPlacedAndNotCounted(outcome, theStoredIssueAbove);
            }
        }

        /// <summary>
        /// A Portfolio Feature that is also the issue above another of its Features is read by both halves of
        /// one refresh. The Feature half already placed and stored it, so the sweep for the issues above
        /// leaves it alone: placing it again, against Features this refresh has just stored, could pick
        /// another parent and save it over the one the counted line names.
        /// </summary>
        [Test]
        public async Task UpdateFeaturesForPortfolio_AFeatureIsAlsoTheIssueAboveAnother_IsPlacedOnceAndKeepsTheParentItsLineNames()
        {
            var portfolio = CreatePortfolio();
            portfolio.ParentOverrideAdditionalFieldDefinitionId = TheFieldThatNamesTheLinkType;
            var stored = TheFeaturesTheRefreshStores();
            featureRepositoryMock
                .Setup(repository => repository.GetByPredicate(It.IsAny<Func<Feature, bool>>()))
                .Returns((Func<Feature, bool> predicate) => stored.SingleOrDefault(predicate));
            featureRepositoryMock
                .SetupSequence(repository => repository.GetStoredFeatureCreatedDates())
                .Returns(new StoredFeatures(new Dictionary<string, DateTime?>()))
                .Returns(new StoredFeatures(new Dictionary<string, DateTime?> { ["INC-5610"] = TheFirstReadingDay.AddYears(-1) }));

            workTrackingConnectorMock
                .Setup(connector => connector.GetFeaturesForProject(portfolio, It.IsAny<CancellationToken>()))
                .ReturnsAsync([TheIssueAboveAsDownloaded(), AFeatureUnderTheIssueAbove(TheFirstReadingDay)]);
            workTrackingConnectorMock
                .Setup(connector => connector.GetParentFeaturesDetails(portfolio, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Portfolio _, IEnumerable<string> keys, CancellationToken _) => keys.Contains(TheIssueAbove) ? [TheIssueAboveAsDownloaded()] : []);

            var subject = CreateSubject();

            var outcome = await subject.UpdateFeaturesForPortfolio(portfolio);

            var theIssueAbove = stored.Single(feature => feature.ReferenceId == TheIssueAbove);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(theIssueAbove.ParentReferenceId, Is.EqualTo("INC-5602"),
                    "Nothing was stored when the Feature half placed it, so the lowest key decided, and that is the parent it keeps.");
                Assert.That(whatTheServiceWrote.Warnings.Count(line => line.Contains($"{TheIssueAbove} under ", StringComparison.Ordinal)), Is.EqualTo(1),
                    "One refresh places a record once, so only one line names where it went.");
                Assert.That(outcome.RecordsPlacedFromLinksNamingMoreThanOneParent, Is.EqualTo(1),
                    "It is one of the Portfolio's own Features, so its row counts it, once.");
            }
        }

        private static void TheIssueAboveWasPlacedAndNotCounted(SyncOutcome outcome, Feature storedIssueAbove)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(storedIssueAbove.ParentReferenceId, Is.EqualTo("INC-5602"),
                    "Nothing is set aside above a Portfolio's Features, so two incidents nobody stores fall to the lowest key.");
                Assert.That(storedIssueAbove.AdditionalFieldValues.GetValueOrDefault(TheFieldThatNamesTheLinkType), Is.EqualTo("INC-5602"),
                    "The Additional Field shows the key that was used.");
                Assert.That(outcome.RecordsPlacedFromLinksNamingMoreThanOneParent, Is.Zero,
                    "The Portfolio's row counts its own Features, and none of them had more than one candidate.");
                Assert.That(outcome.RecordsWhoseLinksNamedMoreThanOneParent, Is.Zero,
                    "The issues above are not the Portfolio's own Features, so they are not counted as not placed either.");
            }
        }

        private static Feature AFeatureUnderTheIssueAbove(DateTime lastChangedRemote)
            => ADownloadedFeature(TheFeatureWithOneLink, TheIssueAbove, [new ParentCandidate(TheIssueAbove, null)], TheIssueAbove, lastChangedRemote);

        private static Feature TheIssueAboveAsDownloaded()
            => ADownloadedFeature(TheIssueAbove, TheTrackersOwnParent, TwoIssuesNobodyHoldsAsAFeature, string.Empty, TheFirstReadingDay);

        [Test]
        public void UpdateFeaturesForPortfolio_TrackerCouldNotBeAsked_KeepsTheFeaturesThePortfolioHolds()
        {
            var portfolio = CreatePortfolio();
            portfolio.UpdateFeatures([new Feature { Id = idCounter++, ReferenceId = "FTR-1" }]);

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(portfolio, CancellationToken.None))
                .ThrowsAsync(new InvalidOperationException("The tracker could not be asked."));

            var subject = CreateSubject();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(async () => await subject.UpdateFeaturesForPortfolio(portfolio), Throws.Exception);

                Assert.That(portfolio.Features, Has.Count.EqualTo(1),
                    "Membership is rebuilt from what the query still returns, so a fetch that threw would strip "
                    + "every claim - and a Feature no portfolio claims is deleted outright by the orphan cleanup.");
            }
        }

        [Test]
        public async Task UpdateWorkItemsForTeam_ConnectorReturnsSameItemTwice_PersistsItemOnceWithoutDuplicatingTransitions()
        {
            var team = CreateTeam();
            var duplicates = CreateSameItemReturnedTwice(team, "Doing", new DateTime(2026, 7, 20, 8, 0, 0, DateTimeKind.Utc));
            AssignIdentitiesOnSave();

            workTrackingConnectorMock.Setup(x => x.GetWorkItemsForTeam(team, CancellationToken.None)).ReturnsAsync(duplicates);

            var subject = CreateSubject();
            await subject.UpdateWorkItemsForTeam(team);

            var persisted = workItems.Where(wi => wi.ReferenceId == duplicates[0].ReferenceId).ToList();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(persisted, Has.Count.EqualTo(1));
                Assert.That(storedTransitions, Has.Count.EqualTo(1));
            }
        }

        [Test]
        public async Task UpdateWorkItemsForTeam_ConnectorReturnsSameItemTwice_FollowUpSyncStillPersistsItemOnce()
        {
            var team = CreateTeam();
            var duplicates = CreateSameItemReturnedTwice(team, "Doing", new DateTime(2026, 7, 20, 8, 0, 0, DateTimeKind.Utc));
            AssignIdentitiesOnSave();

            workTrackingConnectorMock.Setup(x => x.GetWorkItemsForTeam(team, CancellationToken.None)).ReturnsAsync(duplicates);

            var subject = CreateSubject();
            await subject.UpdateWorkItemsForTeam(team);
            await subject.UpdateWorkItemsForTeam(team);

            var persisted = workItems.Where(wi => wi.ReferenceId == duplicates[0].ReferenceId).ToList();
            Assert.That(persisted, Has.Count.EqualTo(1));
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

            workTrackingConnectorMock.Setup(x => x.GetWorkItemsForTeam(team, CancellationToken.None)).ReturnsAsync([incoming]);

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

            workTrackingConnectorMock.Setup(x => x.GetWorkItemsForTeam(team, CancellationToken.None)).ReturnsAsync([incoming]);

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
                .Setup(x => x.GetFeaturesForProject(project, CancellationToken.None))
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

            workTrackingConnectorMock.Setup(x => x.GetWorkItemsForTeam(team, CancellationToken.None)).ReturnsAsync([incoming]);

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

            workTrackingConnectorMock.Setup(x => x.GetWorkItemsForTeam(team, CancellationToken.None)).ReturnsAsync([incoming]);

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

            workTrackingConnectorMock.Setup(x => x.GetWorkItemsForTeam(team, CancellationToken.None)).ReturnsAsync([incoming]);

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
        [Category("story-6247")]
        public async Task UpdateWorkItemsForTeam_BlockedPastTheBlockedThreshold_PublishesWorkItemBecameStaleWithTheBlockedThreshold()
        {
            var team = CreateTeam();
            team.StalenessThresholdDays = 30;
            team.BlockedStalenessThresholdDays = 3;
            team.BlockedRuleSetJson = BlockedByStateRuleSetJson("Blocked");

            var enteredBlocked = DateTime.UtcNow.AddDays(-10);
            var incoming = CreateWorkItemWithTransitions(team, "Blocked", ("Doing", "Blocked", enteredBlocked));
            incoming.StateCategory = StateCategories.Doing;

            workTrackingConnectorMock.Setup(x => x.GetWorkItemsForTeam(team, CancellationToken.None)).ReturnsAsync([incoming]);

            var subject = CreateSubject();
            await subject.UpdateWorkItemsForTeam(team);

            domainEventDispatcherMock.Verify(
                x => x.PublishAsync(
                    It.Is<WorkItemBecameStale>(e => e.WorkItemId == incoming.Id && e.ThresholdDays == 3),
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

            workTrackingConnectorMock.Setup(x => x.GetWorkItemsForTeam(team, CancellationToken.None)).ReturnsAsync([existing]);

            var subject = CreateSubject();
            await subject.UpdateWorkItemsForTeam(team);

            var movedToReview = CreateIncomingFor(existing, "Review", StateCategories.Doing, ("Doing", "Review", DateTime.UtcNow.AddDays(-1)));
            workTrackingConnectorMock.Setup(x => x.GetWorkItemsForTeam(team, CancellationToken.None)).ReturnsAsync([movedToReview]);
            await subject.UpdateWorkItemsForTeam(team);

            var reenteredDoingStale = CreateIncomingFor(existing, "Doing", StateCategories.Doing, ("Review", "Doing", DateTime.UtcNow.AddDays(-20)));
            workTrackingConnectorMock.Setup(x => x.GetWorkItemsForTeam(team, CancellationToken.None)).ReturnsAsync([reenteredDoingStale]);
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
            team.BlockedRuleSetJson = BlockedByStateRuleSetJson("Blocked");

            var existing = CreateWorkItemWithTransitions(team, "Doing");
            existing.State = "Doing";
            existing.StateCategory = StateCategories.Doing;
            workItems.Add(existing);

            var blocked = CreateIncomingFor(existing, "Blocked", StateCategories.Doing);
            workTrackingConnectorMock.Setup(x => x.GetWorkItemsForTeam(team, CancellationToken.None)).ReturnsAsync([blocked]);

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
            team.BlockedRuleSetJson = BlockedByStateRuleSetJson("Blocked");

            var existing = CreateWorkItemWithTransitions(team, "To Do");
            existing.State = "To Do";
            existing.StateCategory = StateCategories.ToDo;
            workItems.Add(existing);

            var incoming = CreateIncomingFor(existing, "Doing", StateCategories.Doing);
            workTrackingConnectorMock.Setup(x => x.GetWorkItemsForTeam(team, CancellationToken.None)).ReturnsAsync([incoming]);

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
            team.BlockedRuleSetJson = BlockedByStateRuleSetJson("Blocked");

            var existing = CreateWorkItemWithTransitions(team, "Blocked");
            existing.State = "Blocked";
            existing.StateCategory = StateCategories.Doing;
            workItems.Add(existing);

            var unblocked = CreateIncomingFor(existing, "Doing", StateCategories.Doing);
            workTrackingConnectorMock.Setup(x => x.GetWorkItemsForTeam(team, CancellationToken.None)).ReturnsAsync([unblocked]);

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

        private static readonly DateTime TheShiftedTime = new(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime TheCorrectedTime = new(2026, 2, 1, 9, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime WhenItMovedOn = new(2026, 2, 3, 14, 0, 0, DateTimeKind.Utc);

        [Test]
        public async Task A_Work_Item_read_with_its_whole_history_replaces_its_stored_transitions()
        {
            var team = CreateTeam();
            var stored = AStoredItemThatEntered(team, "In Progress", TheShiftedTime);

            await RefreshTheTeamWith(team, ItsWholeHistory(CreateIncomingFor(stored, "In Progress", StateCategories.Doing, ("To Do", "In Progress", TheCorrectedTime))));

            var transitionsLeft = TransitionsStoredFor(stored);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(transitionsLeft, Has.Count.EqualTo(1));
                Assert.That(transitionsLeft[0].TransitionedAt, Is.EqualTo(TheCorrectedTime));
                Assert.That(stored.CurrentStateEnteredAt, Is.EqualTo(TheCorrectedTime));
            }
        }

        [Test]
        public async Task A_Work_Item_read_without_its_whole_history_never_loses_a_stored_transition()
        {
            var team = CreateTeam();
            var stored = AStoredItemThatEntered(team, "In Progress", TheShiftedTime);

            await RefreshTheTeamWith(team, CreateIncomingFor(stored, "In Progress", StateCategories.Doing, ("To Do", "In Progress", TheCorrectedTime)));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TransitionsStoredFor(stored).Select(t => t.TransitionedAt), Is.EquivalentTo(new[] { TheShiftedTime, TheCorrectedTime }));
                stateTransitionRepositoryMock.Verify(x => x.Remove(It.IsAny<WorkItemStateTransition?>()), Times.Never);
            }
        }

        [Test]
        public async Task A_Work_Item_whose_whole_history_reads_as_empty_keeps_its_stored_transitions()
        {
            var team = CreateTeam();
            var stored = AStoredItemThatEntered(team, "In Progress", TheShiftedTime);

            await RefreshTheTeamWith(team, ItsWholeHistory(CreateIncomingFor(stored, "In Progress", StateCategories.Doing)));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TransitionsStoredFor(stored).Select(t => t.TransitionedAt), Is.EqualTo(new[] { TheShiftedTime }));
                Assert.That(stored.CurrentStateEnteredAt, Is.EqualTo(TheShiftedTime));
            }
        }

        [Test]
        public async Task A_stored_transition_the_whole_history_still_names_keeps_its_row()
        {
            var team = CreateTeam();
            var stored = AStoredItemThatEntered(team, "In Review", TheShiftedTime, fromState: "In Progress");
            var theRowStillNamed = StoreTransition(stored, "To Do", "In Progress", TheFirstReadingDay);

            await RefreshTheTeamWith(team, ItsWholeHistory(CreateIncomingFor(stored, "In Review", StateCategories.Doing,
                ("To Do", "In Progress", TheFirstReadingDay),
                ("In Progress", "In Review", TheCorrectedTime))));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TransitionsStoredFor(stored), Has.Count.EqualTo(2));
                Assert.That(TransitionsStoredFor(stored), Does.Contain(theRowStillNamed));
                stateTransitionRepositoryMock.Verify(x => x.Remove(It.IsAny<WorkItemStateTransition?>()), Times.Once);
                stateTransitionRepositoryMock.Verify(x => x.Remove(theRowStillNamed), Times.Never);
                stateTransitionRepositoryMock.Verify(x => x.Add(It.IsAny<WorkItemStateTransition>()), Times.Once);
                stateTransitionRepositoryMock.Verify(x => x.Add(It.Is<WorkItemStateTransition>(t => t.TransitionedAt == TheCorrectedTime && t.ToState == "In Review")), Times.Once);
            }
        }

        [Test]
        public async Task A_corrected_time_is_not_a_state_change_and_raises_no_WorkItemTransitioned()
        {
            var team = CreateTeam();
            var stored = AStoredItemThatEntered(team, "In Progress", TheShiftedTime);

            await RefreshTheTeamWith(team, ItsWholeHistory(CreateIncomingFor(stored, "In Progress", StateCategories.Doing, ("To Do", "In Progress", TheCorrectedTime))));

            domainEventDispatcherMock.Verify(
                x => x.PublishAsync(It.IsAny<WorkItemTransitioned>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task A_genuinely_new_transition_read_beside_a_correction_raises_exactly_its_one_WorkItemTransitioned()
        {
            var team = CreateTeam();
            var stored = AStoredItemThatEntered(team, "In Progress", TheShiftedTime);

            await RefreshTheTeamWith(team, ItsWholeHistory(CreateIncomingFor(stored, "In Review", StateCategories.Doing,
                ("To Do", "In Progress", TheCorrectedTime),
                ("In Progress", "In Review", WhenItMovedOn))));

            using (Assert.EnterMultipleScope())
            {
                domainEventDispatcherMock.Verify(
                    x => x.PublishAsync(It.IsAny<WorkItemTransitioned>(), It.IsAny<CancellationToken>()),
                    Times.Once);
                domainEventDispatcherMock.Verify(
                    x => x.PublishAsync(
                        It.Is<WorkItemTransitioned>(e => e.WorkItemId == stored.Id && e.FromState == "In Progress" && e.ToState == "In Review"),
                        It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }

        [Test]
        public async Task A_Feature_read_with_its_whole_history_replaces_its_stored_transitions()
        {
            var team = CreateTeam();
            var portfolio = CreatePortfolio();
            var storedFeature = new Feature(team, 1) { Id = idCounter++, ReferenceId = "FTR-CORRECTED", State = "In Progress" };
            var storedFeatureTransitions = new List<FeatureStateTransition>
            {
                new() { FeatureId = storedFeature.Id, FromState = "To Do", ToState = "In Progress", TransitionedAt = TheShiftedTime },
            };
            GivenTheFeatureTransitionsStoredAre(storedFeatureTransitions);
            featureRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<Feature, bool>>()))
                .Returns((Func<Feature, bool> predicate) => new List<Feature> { storedFeature }.SingleOrDefault(predicate));

            var incoming = new Feature(team, 1)
            {
                ReferenceId = storedFeature.ReferenceId,
                State = "In Progress",
                SyncedTransitions = [new WorkItemStateTransition { FromState = "To Do", ToState = "In Progress", TransitionedAt = TheCorrectedTime }],
                TransitionsAreItsWholeHistory = true,
            };
            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(portfolio, It.IsAny<CancellationToken>())).ReturnsAsync([incoming]);

            await CreateSubject().UpdateFeaturesForPortfolio(portfolio);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(storedFeatureTransitions.Select(t => t.TransitionedAt), Is.EqualTo(new[] { TheCorrectedTime }));
                Assert.That(storedFeature.CurrentStateEnteredAt, Is.EqualTo(TheCorrectedTime));
            }
        }

        [Test]
        public async Task A_move_into_the_same_state_from_another_state_is_not_taken_for_a_correction()
        {
            var team = CreateTeam();
            var stored = AStoredItemThatEntered(team, "In Progress", TheShiftedTime);

            await RefreshTheTeamWith(team, ItsWholeHistory(CreateIncomingFor(stored, "In Progress", StateCategories.Doing, ("Blocked", "In Progress", WhenItMovedOn))));

            domainEventDispatcherMock.Verify(
                x => x.PublishAsync(
                    It.Is<WorkItemTransitioned>(e => e.FromState == "Blocked" && e.ToState == "In Progress"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task One_dropped_row_stands_in_for_only_one_of_two_moves_between_the_same_states()
        {
            var team = CreateTeam();
            var stored = AStoredItemThatEntered(team, "In Progress", TheShiftedTime);

            await RefreshTheTeamWith(team, ItsWholeHistory(CreateIncomingFor(stored, "In Progress", StateCategories.Doing,
                ("To Do", "In Progress", TheCorrectedTime),
                ("To Do", "In Progress", WhenItMovedOn))));

            domainEventDispatcherMock.Verify(
                x => x.PublishAsync(It.IsAny<WorkItemTransitioned>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task A_Feature_read_without_its_whole_history_never_loses_a_stored_transition()
        {
            var storedFeatureTransitions = await RefreshAFeatureStoredAs(
                [("To Do", "In Progress", TheShiftedTime)],
                [("To Do", "In Progress", TheCorrectedTime)],
                transitionsAreItsWholeHistory: false);

            Assert.That(storedFeatureTransitions.Select(t => t.TransitionedAt), Is.EquivalentTo(new[] { TheShiftedTime, TheCorrectedTime }));
        }

        [Test]
        public async Task A_stored_Feature_transition_the_whole_history_still_names_keeps_its_row()
        {
            var storedFeatureTransitions = await RefreshAFeatureStoredAs(
                [("To Do", "In Progress", TheFirstReadingDay), ("In Progress", "In Review", TheShiftedTime)],
                [("To Do", "In Progress", TheFirstReadingDay), ("In Progress", "In Review", TheCorrectedTime)],
                transitionsAreItsWholeHistory: true);

            Assert.That(storedFeatureTransitions.Select(t => (t.ToState, t.TransitionedAt)),
                Is.EquivalentTo(new[] { ("In Progress", TheFirstReadingDay), ("In Review", TheCorrectedTime) }));
        }

        private async Task<List<FeatureStateTransition>> RefreshAFeatureStoredAs(
            (string FromState, string ToState, DateTime At)[] stored,
            (string FromState, string ToState, DateTime At)[] read,
            bool transitionsAreItsWholeHistory)
        {
            var team = CreateTeam();
            var portfolio = CreatePortfolio();
            var storedFeature = new Feature(team, 1) { Id = idCounter++, ReferenceId = "FTR-RECONCILED", State = read[^1].ToState };
            var storedFeatureTransitions = stored
                .Select(t => new FeatureStateTransition { FeatureId = storedFeature.Id, FromState = t.FromState, ToState = t.ToState, TransitionedAt = t.At })
                .ToList();
            GivenTheFeatureTransitionsStoredAre(storedFeatureTransitions);
            featureRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<Feature, bool>>()))
                .Returns((Func<Feature, bool> predicate) => new List<Feature> { storedFeature }.SingleOrDefault(predicate));

            var incoming = new Feature(team, 1)
            {
                ReferenceId = storedFeature.ReferenceId,
                State = storedFeature.State,
                SyncedTransitions = [.. read.Select(t => new WorkItemStateTransition { FromState = t.FromState, ToState = t.ToState, TransitionedAt = t.At })],
                TransitionsAreItsWholeHistory = transitionsAreItsWholeHistory,
            };
            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(portfolio, It.IsAny<CancellationToken>())).ReturnsAsync([incoming]);

            await CreateSubject().UpdateFeaturesForPortfolio(portfolio);

            return storedFeatureTransitions;
        }

        private void GivenTheFeatureTransitionsStoredAre(List<FeatureStateTransition> storedFeatureTransitions)
        {
            featureStateTransitionRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<FeatureStateTransition, bool>>>()))
                .Returns((Expression<Func<FeatureStateTransition, bool>> predicate) => storedFeatureTransitions.Where(predicate.Compile()).AsQueryable());
            featureStateTransitionRepositoryMock.Setup(x => x.Add(It.IsAny<FeatureStateTransition>()))
                .Callback((FeatureStateTransition transition) => storedFeatureTransitions.Add(transition));
            featureStateTransitionRepositoryMock.Setup(x => x.Remove(It.IsAny<FeatureStateTransition?>()))
                .Callback((FeatureStateTransition? transition) => storedFeatureTransitions.Remove(transition!));
        }

        private WorkItem AStoredItemThatEntered(Team team, string state, DateTime enteredAt, string fromState = "To Do")
        {
            var stored = CreateWorkItemWithTransitions(team, state);
            workItems.Add(stored);
            StoreTransition(stored, fromState, state, enteredAt);
            stored.CurrentStateEnteredAt = enteredAt;
            return stored;
        }

        private WorkItemStateTransition StoreTransition(WorkItem workItem, string fromState, string toState, DateTime transitionedAt)
        {
            var transition = new WorkItemStateTransition { WorkItemId = workItem.Id, FromState = fromState, ToState = toState, TransitionedAt = transitionedAt };
            storedTransitions.Add(transition);
            return transition;
        }

        private List<WorkItemStateTransition> TransitionsStoredFor(WorkItem workItem)
            => [.. storedTransitions.Where(t => t.WorkItemId == workItem.Id)];

        private static WorkItem ItsWholeHistory(WorkItem incoming)
        {
            incoming.TransitionsAreItsWholeHistory = true;
            return incoming;
        }

        private async Task RefreshTheTeamWith(Team team, WorkItem incoming)
        {
            workTrackingConnectorMock.Setup(x => x.SupportsTransitionHistory(It.IsAny<WorkTrackingSystemConnection>())).Returns(true);
            workTrackingConnectorMock.Setup(x => x.GetWorkItemsForTeam(team, It.IsAny<CancellationToken>())).ReturnsAsync([incoming]);

            await CreateSubject().UpdateWorkItemsForTeam(team);
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

        private List<WorkItem> CreateSameItemReturnedTwice(Team team, string state, DateTime enteredState)
        {
            var template = CreateWorkItemWithTransitions(team, state, ("To Do", state, enteredState));

            return
            [
                CreateIncomingFor(template, state, template.StateCategory, ("To Do", state, enteredState)),
                CreateIncomingFor(template, state, template.StateCategory, ("To Do", state, enteredState)),
            ];
        }

        // The database assigns identities on Save; without that, two rows for the same ReferenceId would
        // both keep Id 0 and their duplicated transitions would collapse instead of being observable.
        private void AssignIdentitiesOnSave()
        {
            workItemRepositoryMock.Setup(x => x.Save())
                .Callback(() =>
                {
                    foreach (var unpersisted in workItems.Where(wi => wi.Id == 0).ToList())
                    {
                        unpersisted.Id = idCounter++;
                    }
                })
                .Returns(Task.CompletedTask);
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

        /// <summary>
        /// A stored record the tracker has already given a change stamp for. Without one on every stored
        /// record there is nothing to compare against, and the refresh falls back to downloading the whole
        /// query.
        /// </summary>
        private void AddStampedWorkItemForTeam(Team team, string referenceId, DateTime lastChangedRemote)
        {
            workItems.Add(new WorkItem
            {
                Id = idCounter++,
                ReferenceId = referenceId,
                State = "To Do",
                StateCategory = StateCategories.ToDo,
                Team = team,
                TeamId = team.Id,
                LastChangedRemote = lastChangedRemote,
            });
        }

        private Feature AStampedFeature(string referenceId, DateTime lastChangedRemote)
            => new()
            {
                Id = idCounter++,
                ReferenceId = referenceId,
                LastChangedRemote = lastChangedRemote,
            };

        /// <summary>
        /// What a connector hands back for a record whose links named more than one issue to hang it
        /// under: the record itself, carrying every issue its links named. None of them is a Feature this
        /// instance holds, so the refresh cannot place it.
        /// </summary>
        private static WorkItem ADownloadThatNobodyCouldPlace(string referenceId, DateTime lastChangedRemote)
            => new()
            {
                ReferenceId = referenceId,
                State = "To Do",
                StateCategory = StateCategories.ToDo,
                LastChangedRemote = lastChangedRemote,
                ParentCandidatesFromLinks = TwoIssuesNobodyHoldsAsAFeature,
            };

        private void TheTrackerCanBeScanned()
            => workTrackingConnectorMock
                .Setup(connector => connector.SupportsIncrementalSync(It.IsAny<WorkTrackingSystemConnection>()))
                .Returns(true);

        private Team CreateTeam()
        {
            var team = new Team { Name = "Team", Id = idCounter++ };

            team.WorkItemTypes.Add("User Story");

            var workTrackingConnection = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.Jira };
            team.WorkTrackingSystemConnection = workTrackingConnection;

            teams.Add(team);

            return team;
        }

        private static string BlockedByStateRuleSetJson(string state)
        {
            var ruleSet = new WorkItemRuleSet
            {
                Mode = WorkItemRuleSet.ModeOr,
                Conditions = [new WorkItemRuleCondition { FieldKey = "workitem.state", Operator = RuleOperators.Equals, Value = state }],
            };

            return JsonSerializer.Serialize(ruleSet);
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
            return new WorkItemServiceTestBuilder()
                .WithConnector(workTrackingConnectorMock.Object)
                .WithFeatureRepository(featureRepositoryMock.Object)
                .WithWorkItemRepository(workItemRepositoryMock.Object)
                .WithPortfolioMetricsService(projectMetricsServiceMock.Object)
                .WithTeamRepository(teamRepositoryMock.Object)
                .WithStateTransitionRepository(stateTransitionRepositoryMock.Object)
                .WithFeatureStateTransitionRepository(featureStateTransitionRepositoryMock.Object)
                .WithDomainEventDispatcher(domainEventDispatcherMock.Object)
                .WithLogger(whatTheServiceWrote)
                .Build();
        }
    }
}
