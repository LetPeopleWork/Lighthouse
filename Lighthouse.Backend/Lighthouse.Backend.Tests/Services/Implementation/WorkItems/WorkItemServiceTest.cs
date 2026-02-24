using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.WorkItems;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
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

        private int idCounter;
        private List<WorkItem> workItems;

        [SetUp]
        public void SetUp()
        {
            workTrackingConnectorMock = new Mock<IWorkTrackingConnector>();
            featureRepositoryMock = new Mock<IRepository<Feature>>();
            workItemRepositoryMock = new Mock<IWorkItemRepository>();
            projectMetricsServiceMock = new Mock<IPortfolioMetricsService>();

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(It.IsAny<Portfolio>())).Returns(Task.FromResult(new List<Feature>()));

            workItems = [];

            workItemRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<WorkItem, bool>>>()))
                .Returns((Expression<Func<WorkItem, bool>> predicate) => workItems.Where(predicate.Compile()).AsQueryable());
            workItemRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<WorkItem, bool>>()))
                .Returns((Func<WorkItem, bool> predicate) => workItems.SingleOrDefault(predicate));
        }

        [Test]
        public async Task UpdateFeaturesForProject_SingleTeamInvolved_FindsFeauture()
        {
            var team = CreateTeam();

            var project = CreatePortfolio(team);
            var feature = new Feature(team, 12) { ReferenceId = "12" };

            SetupWorkForFeature(feature, 1, 0, team);

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature }));

            var subject = CreateSubject();
            await subject.UpdateFeaturesForProject(project);

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
            var project = CreatePortfolio(team);

            var existingFeature = new Feature(team, 12) { Id = 12 };

            project.Features.Add(existingFeature);

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature>()));

            var subject = CreateSubject();
            await subject.UpdateFeaturesForProject(project);

            Assert.That(project.Features, Is.Empty);
        }

        [Test]
        [TestCase("Analysis in Progress", 2, 42, 42)]
        [TestCase("Analysis Done", 7, 42, 7)]
        public async Task UpdateFeaturesForProject_HasRemainingWork_InStateToOverrideRealWork_UsesDefaultItems(string featureState, int remainingWork, int defaultWork, int expectedWork)
        {
            var team = CreateTeam();
            var project = CreatePortfolio(team);

            project.DefaultAmountOfWorkItemsPerFeature = defaultWork;
            project.OverrideRealChildCountStates.Add("Analysis in Progress");

            var feature1 = new Feature(team, 2) { ReferenceId = "12", State = featureState };
            var feature2 = new Feature(team, 2) { ReferenceId = "1337" };

            SetupWorkForFeature(feature1, 10, remainingWork, team);
            SetupWorkForFeature(feature2, 15, 12, team);

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));

            var subject = CreateSubject();
            await subject.UpdateFeaturesForProject(project);

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
            var project = CreatePortfolio(team);


            project.DoneStates.Clear();
            project.DoneStates.Add("Done");

            project.DefaultAmountOfWorkItemsPerFeature = 42;

            var feature = new Feature(team, remainingWork) { ReferenceId = "12", State = "Done", StateCategory = StateCategories.Done };
            SetupWorkForFeature(feature, totalWork, remainingWork, team);

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature }));

            var subject = CreateSubject();
            await subject.UpdateFeaturesForProject(project);

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
            var project = CreatePortfolio(team);


            project.DefaultAmountOfWorkItemsPerFeature = 7;
            project.OverrideRealChildCountStates.Add("Analysis in Progress");

            var feature1 = new Feature(team, 2) { ReferenceId = "12", State = "Analysis in Progress" };
            var feature2 = new Feature(team, 2) { ReferenceId = "1337" };

            var features = new List<Feature>() { feature1, feature2 };

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));

            featureRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<Feature, bool>>())).Returns((Func<Feature, bool> predicate) => features.SingleOrDefault(predicate));

            var subject = CreateSubject();
            await subject.UpdateFeaturesForProject(project);

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
            var project = CreatePortfolio(team);
            project.DefaultAmountOfWorkItemsPerFeature = 12;


            var feature1 = new Feature(team, 0) { ReferenceId = "42" };
            var feature2 = new Feature(team, 2) { ReferenceId = "12" };

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));

            var subject = CreateSubject();
            await subject.UpdateFeaturesForProject(project);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(project.Features, Has.Count.EqualTo(2));

                var feature = project.Features[0];
                Assert.That(feature.FeatureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(12));
                Assert.That(feature.IsUsingDefaultFeatureSize, Is.True);
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
            var project = CreatePortfolio(team);

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
            await subject.UpdateFeaturesForProject(project);

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
            var project = CreatePortfolio(team);

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
            await subject.UpdateFeaturesForProject(project);

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
            var project = CreatePortfolio(team);
            project.DefaultAmountOfWorkItemsPerFeature = 12;
            project.SizeEstimateAdditionalFieldDefinitionId = 1;


            var feature1 = new Feature(team, 0) { ReferenceId = "42", EstimatedSize = 0 };
            var feature2 = new Feature(team, 2) { ReferenceId = "12" };

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));

            var subject = CreateSubject();
            await subject.UpdateFeaturesForProject(project);

            Assert.That(project.Features, Has.Count.EqualTo(2));
            Assert.That(project.Features[0].FeatureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(12));
        }

        [Test]
        public async Task UpdateFeaturesForProject_NoRemainingWork_NoTotalWork_SizeEstimateFieldSet_SizeEstimateAvailable_AddsEstimatedWorkToFeature()
        {
            var team = CreateTeam();
            var project = CreatePortfolio(team);
            project.DefaultAmountOfWorkItemsPerFeature = 12;
            project.SizeEstimateAdditionalFieldDefinitionId = 1; // Size field configured and feature has size


            var feature1 = new Feature(team, 0) { ReferenceId = "42", EstimatedSize = 7 };
            var feature2 = new Feature(team, 2) { ReferenceId = "12" };

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));

            var subject = CreateSubject();
            await subject.UpdateFeaturesForProject(project);

            Assert.That(project.Features, Has.Count.EqualTo(2));
            Assert.That(project.Features[0].FeatureWork.Sum(x => x.RemainingWorkItems), Is.EqualTo(7));
        }

        [Test]
        public async Task UpdateFeaturesForProject_NoRemainingWork_HasTotalWork_DoesNotAddDefaultRemainingWorkToFeature()
        {
            var team = CreateTeam();
            var project = CreatePortfolio(team);
            project.DefaultAmountOfWorkItemsPerFeature = 12;


            var feature1 = new Feature(team, 0) { ReferenceId = "42" };
            var feature2 = new Feature(team, 2) { ReferenceId = "12" };

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));

            SetupWorkForFeature(feature1, 7, 0, team);

            var subject = CreateSubject();
            await subject.UpdateFeaturesForProject(project);

            Assert.That(project.Features, Has.Count.EqualTo(2));
            Assert.That(project.Features[0].FeatureWork.Sum(x => x.RemainingWorkItems), Is.Zero);
        }

        [Test]
        public async Task UpdateFeaturesForProject_NoTotalWork_MulitpleTeams_SplitsDefaultRemainingWorkByTeam()
        {
            var team1 = CreateTeam();
            var team2 = CreateTeam();
            var project = CreatePortfolio(team1, team2);


            project.DefaultAmountOfWorkItemsPerFeature = 12;

            var feature1 = new Feature([(team1, 0, 12), (team2, 0, 10)]) { ReferenceId = "17" };
            var feature2 = new Feature([(team1, 2, 13), (team2, 2, 3)]) { ReferenceId = "19" };

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));
            SetupWorkForFeature(feature1, 0, 0);
            SetupWorkForFeature(feature2, 12, 12);

            var subject = CreateSubject();
            await subject.UpdateFeaturesForProject(project);

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
            var project = CreatePortfolio(team);


            var remainingWorkItems = 12;
            var feature = new Feature(team, remainingWorkItems) { ReferenceId = "42" };

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature }));
            SetupWorkForFeature(feature, 12, remainingWorkItems, team);

            var subject = CreateSubject();
            await subject.UpdateFeaturesForProject(project);

            var actualFeature = project.Features.Single();
            Assert.That(actualFeature.GetRemainingWorkForTeam(team), Is.EqualTo(remainingWorkItems));
        }


        [Test]
        public async Task UpdateFeaturesForProject_MultipleTeamsInvolved()
        {
            var team1 = CreateTeam();
            var team2 = CreateTeam();
            var project = CreatePortfolio(team1, team2);


            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(It.IsAny<Portfolio>())).Returns(Task.FromResult(new List<Feature>()));

            var subject = CreateSubject();
            await subject.UpdateFeaturesForProject(project);

            workTrackingConnectorMock.Verify(x => x.GetFeaturesForProject(project), Times.Exactly(1));
        }

        [Test]
        public async Task UpdateFeaturesForProject_TwoTeamsInvolved_IndividualFeatures_FindsRemainingWorkByTeam()
        {
            var team1 = CreateTeam();
            var team2 = CreateTeam();

            var project = CreatePortfolio(team1, team2);


            var remainingWorkItemsFeature1 = 12;
            var remainingWorkItemsFeature2 = 7;
            var feature1 = new Feature(team1, remainingWorkItemsFeature1) { ReferenceId = "1" };
            var feature2 = new Feature(team2, remainingWorkItemsFeature1) { ReferenceId = "2" };

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature1, feature2 }));

            SetupWorkForFeature(feature1, 12, remainingWorkItemsFeature1, team1);
            SetupWorkForFeature(feature2, 12, remainingWorkItemsFeature2, team2);

            var subject = CreateSubject();
            await subject.UpdateFeaturesForProject(project);

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

            var project = CreatePortfolio(team1, team2);


            var remainingWorkItemsTeam1 = 12;
            var remainingWorkItemsTeam2 = 7;
            var feature = new Feature(team1, remainingWorkItemsTeam1) { ReferenceId = "1" };

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature }));

            SetupWorkForFeature(feature, 12, remainingWorkItemsTeam1, team1);
            SetupWorkForFeature(feature, 12, remainingWorkItemsTeam2, team2);

            var subject = CreateSubject();
            await subject.UpdateFeaturesForProject(project);

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

            var project = CreatePortfolio(team1, team2);
            project.DefaultAmountOfWorkItemsPerFeature = 12;


            var teams = new List<(Team team, int remainingItems, int totalItems)> { (team1, 0, 0), (team2, 0, 0) };
            var feature = new Feature(teams) { ReferenceId = "42" };

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature }));
            SetupWorkForFeature(feature, 0, 0);

            var subject = CreateSubject();
            await subject.UpdateFeaturesForProject(project);

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

            var project = CreatePortfolio(team1, team2);
            project.DefaultAmountOfWorkItemsPerFeature = 12;
            project.OwningTeam = team2;


            var teams = new List<(Team team, int remainingItems, int totalItems)> { (team1, 0, 0), (team2, 0, 0) };
            var feature = new Feature(teams) { ReferenceId = "42" };

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature }));
            SetupWorkForFeature(feature, 0, 0);

            var subject = CreateSubject();
            await subject.UpdateFeaturesForProject(project);

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

            var portfolio = CreatePortfolio(team1, team2);
            portfolio.DefaultAmountOfWorkItemsPerFeature = 12;
            portfolio.OwningTeam = null;
            portfolio.FeatureOwnerAdditionalFieldDefinitionId = 1;

            AddAdditionalField(portfolio, 1, "MyCustomField", "My Custom Field");

            var teams = new List<(Team team, int remainingItems, int totalItems)> { (team1, 0, 0), (team2, 0, 0) };
            var feature = new Feature(teams)
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
            await subject.UpdateFeaturesForProject(portfolio);

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

            var portfolio = CreatePortfolio(team1, team2);
            portfolio.DefaultAmountOfWorkItemsPerFeature = 12;
            portfolio.OwningTeam = team1;
            portfolio.FeatureOwnerAdditionalFieldDefinitionId = 12;

            AddAdditionalField(portfolio, 12, "MyCustomField", "My Custom Field");

            var teams = new List<(Team team, int remainingItems, int totalItems)> { (team1, 0, 0), (team2, 0, 0) };
            var feature = new Feature(teams)
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
            await subject.UpdateFeaturesForProject(portfolio);

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

            var portfolio = CreatePortfolio(team1, team2, team3);
            portfolio.DefaultAmountOfWorkItemsPerFeature = 12;
            portfolio.FeatureOwnerAdditionalFieldDefinitionId = 1;

            AddAdditionalField(portfolio, 1, "MyCustomField", "My Custom Field");

            var teams = new List<(Team team, int remainingItems, int totalItems)> { (team1, 0, 0), (team2, 0, 0), (team3, 0, 0) };
            var feature = new Feature(teams)
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
            await subject.UpdateFeaturesForProject(portfolio);

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

            var portfolio = CreatePortfolio(team1, team2, team3);
            portfolio.DefaultAmountOfWorkItemsPerFeature = 12;
            portfolio.FeatureOwnerAdditionalFieldDefinitionId = 1;
            portfolio.OwningTeam = team1;

            AddAdditionalField(portfolio, 1, "MyCustomField", "My Custom Field");

            var teams = new List<(Team team, int remainingItems, int totalItems)> { (team1, 0, 0), (team2, 0, 0), (team3, 0, 0) };
            var feature = new Feature(teams)
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
            await subject.UpdateFeaturesForProject(portfolio);

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

            var portfolio = CreatePortfolio(team1, team2, team3);
            portfolio.DefaultAmountOfWorkItemsPerFeature = 12;
            portfolio.FeatureOwnerAdditionalFieldDefinitionId = 1;
            portfolio.OwningTeam = team1;

            AddAdditionalField(portfolio, 1, "MyCustomField", "My Custom Field");

            var teams = new List<(Team team, int remainingItems, int totalItems)> { (team1, 0, 0), (team2, 0, 0), (team3, 0, 0) };
            var feature = new Feature(teams)
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
            await subject.UpdateFeaturesForProject(portfolio);

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

            var portfolio = CreatePortfolio(team1, team2, team3);
            portfolio.DefaultAmountOfWorkItemsPerFeature = 15;
            portfolio.FeatureOwnerAdditionalFieldDefinitionId = 1;

            AddAdditionalField(portfolio, 1, "MyCustomField", "My Custom Field");

            var teams = new List<(Team team, int remainingItems, int totalItems)> { (team1, 0, 0), (team2, 0, 0), (team3, 0, 0) };
            var feature = new Feature(teams)
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
            await subject.UpdateFeaturesForProject(portfolio);

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
            var project = CreatePortfolio(team);
            var feature = new Feature(team, 12) { ReferenceId = "12", ParentReferenceId = "1886" };
            var parentFeature = new Feature { ReferenceId = "1886" };

            featureRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<Feature, bool>>())).Returns((Func<Feature, bool> predicate) => null);

            SetupWorkForFeature(feature, 1, 0, team);

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature }));
            workTrackingConnectorMock.Setup(x => x.GetParentFeaturesDetails(project, new[] { feature.ParentReferenceId })).ReturnsAsync(
                [parentFeature]);

            var subject = CreateSubject();
            await subject.UpdateFeaturesForProject(project);

            featureRepositoryMock.Verify(x => x.Add(parentFeature));
            Assert.That(parentFeature.IsParentFeature, Is.True);
        }

        [Test]
        public async Task UpdateFeaturesForProject_FeatureHasParent_FeatureAlreadyExists_UpdatesParentFeature()
        {
            var team = CreateTeam();
            var project = CreatePortfolio(team);
            var feature = new Feature(team, 12) { ReferenceId = "12", ParentReferenceId = "1886" };
            var parentFeature = new Feature { ReferenceId = "1886" };

            featureRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<Feature, bool>>())).Returns((Func<Feature, bool> predicate) => new List<Feature> { parentFeature }.SingleOrDefault(predicate));

            SetupWorkForFeature(feature, 1, 0, team);

            workTrackingConnectorMock.Setup(x => x.GetFeaturesForProject(project)).Returns(Task.FromResult(new List<Feature> { feature }));
            workTrackingConnectorMock.Setup(x => x.GetParentFeaturesDetails(project, new[] { feature.ParentReferenceId })).ReturnsAsync(
                [parentFeature]);

            var subject = CreateSubject();
            await subject.UpdateFeaturesForProject(project);

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
            workItemRepositoryMock.Verify(x => x.Save(), Times.Once);
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
            workItemRepositoryMock.Verify(x => x.Save(), Times.Once);
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
            workItemRepositoryMock.Verify(x => x.Save(), Times.Once);
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

            return team;
        }

        private void AddAdditionalField(Portfolio portfolio, int id, string reference,
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

        private Portfolio CreatePortfolio(params Team[] teams)
        {
            return CreatePortfolio(DateTime.Now, teams);
        }

        private Portfolio CreatePortfolio(DateTime lastUpdateTime, params Team[] teams)
        {
            var portfolio = new Portfolio
            {
                Id = idCounter++,
                Name = "Release 1",
            };

            portfolio.WorkItemTypes.Add("Feature");
            portfolio.UpdateTeams(teams);

            foreach (var team in teams)
            {
                team.Portfolios.Add(portfolio);
            }

            var workTrackingConnection = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.Jira };
            portfolio.WorkTrackingSystemConnection = workTrackingConnection;

            portfolio.UpdateTime = lastUpdateTime;

            return portfolio;
        }

        private WorkItemService CreateSubject()
        {
            var workTrackingConnectorFactoryMock = new Mock<IWorkTrackingConnectorFactory>();
            workTrackingConnectorFactoryMock.Setup(x => x.GetWorkTrackingConnector(It.IsAny<WorkTrackingSystems>())).Returns(workTrackingConnectorMock.Object);

            return new WorkItemService(Mock.Of<ILogger<WorkItemService>>(), workTrackingConnectorFactoryMock.Object, featureRepositoryMock.Object, workItemRepositoryMock.Object, projectMetricsServiceMock.Object);
        }
    }
}
