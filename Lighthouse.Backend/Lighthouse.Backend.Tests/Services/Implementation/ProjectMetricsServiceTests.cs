using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    [TestFixture]
    public class ProjectMetricsServiceTests
    {
        private Mock<ILogger<ProjectMetricsService>> logger;
        private Mock<IRepository<Feature>> featureRepository;
        private Mock<IAppSettingService> appSettingService;
        private Mock<IForecastService> forecastServiceMock;

        private ProjectMetricsService subject;
        private Project project;
        private List<Feature> features;

        [SetUp]
        public void Setup()
        {
            logger = new Mock<ILogger<ProjectMetricsService>>();
            featureRepository = new Mock<IRepository<Feature>>();
            appSettingService = new Mock<IAppSettingService>();

            appSettingService.Setup(m => m.GetFeaturRefreshSettings()).Returns(new RefreshSettings { Interval = 30 });

            forecastServiceMock = new Mock<IForecastService>();
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(sp => sp.GetService(typeof(IForecastService)))
                .Returns(forecastServiceMock.Object);

            subject = new ProjectMetricsService(logger.Object, featureRepository.Object, appSettingService.Object, serviceProvider.Object);

            featureRepository.Setup(x => x.GetAllByPredicate(
                    It.IsAny<Expression<Func<Feature, bool>>>()))
                .Returns((Expression<Func<Feature, bool>> predicate) => features.Where(predicate.Compile()).AsQueryable());

            SetupTestData();
        }

        [Test]
        public void GetThroughputForProject_ReturnsRunChartDataWithCorrectValues()
        {
            // Arrange
            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 1, 10);

            featureRepository.Setup(x => x.GetAllByPredicate(
                It.IsAny<Expression<Func<Feature, bool>>>()))
                .Returns((Expression<Func<Feature, bool>> predicate) => features.Where(predicate.Compile()).AsQueryable());

            // Act
            var result = subject.GetThroughputForProject(project, startDate, endDate);

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.WorkItemsPerUnitOfTime, Has.Count.EqualTo(10));
                Assert.That(result.Total, Is.EqualTo(2));
            };
        }

        [Test]
        public void GetFeaturesInProgressOverTimeForProject_ReturnsCorrectRunChartData()
        {
            // Arrange
            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 1, 5);

            featureRepository.Setup(x => x.GetAllByPredicate(
                It.IsAny<Expression<Func<Feature, bool>>>()))
                .Returns((Expression<Func<Feature, bool>> predicate) => features.Where(predicate.Compile()).AsQueryable());

            // Act
            var result = subject.GetFeaturesInProgressOverTimeForProject(project, startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.WorkItemsPerUnitOfTime, Has.Count.EqualTo(5));
            };
        }

        [Test]
        public void GetStartedItemsForProject_GivenStartDate_ReturnsStartedItemsPerDayFromThisRange()
        {
            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 1, 3);

            featureRepository.Setup(x => x.GetAllByPredicate(
                It.IsAny<Expression<Func<Feature, bool>>>()))
                .Returns((Expression<Func<Feature, bool>> predicate) => features.Where(predicate.Compile()).AsQueryable());

            var throughput = subject.GetStartedItemsForProject(project, startDate, endDate);

            Assert.That(throughput.Total, Is.EqualTo(2));
        }

        [Test]
        public void GetInProgressFeaturesForProject_ReturnsActiveFeatures()
        {
            // Act
            var result = subject.GetInProgressFeaturesForProject(project).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Has.Count.EqualTo(1));
                Assert.That(result.First().ReferenceId, Is.EqualTo("F3"));
            };
        }

        [Test]
        public void GetCycleTimePercentilesForProject_ReturnsCorrectPercentileValues()
        {
            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 1, 31);

            var result = subject.GetCycleTimePercentilesForProject(project, startDate, endDate).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Has.Count.EqualTo(4));
                Assert.That(result[0].Percentile, Is.EqualTo(50));
                Assert.That(result[1].Percentile, Is.EqualTo(70));
                Assert.That(result[2].Percentile, Is.EqualTo(85));
                Assert.That(result[3].Percentile, Is.EqualTo(95));
            };
        }

        [Test]
        public void GetCycleTimePercentilesForProject_NoClosedItems_ReturnsEmpty()
        {
            var startDate = new DateTime(2077, 1, 1);
            var endDate = new DateTime(2077, 1, 31);

            var result = subject.GetCycleTimePercentilesForProject(project, startDate, endDate).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Has.Count.EqualTo(0));
            };
        }

        [Test]
        public void GetSizePercentilesForProject_ReturnsCorrectPercentileValues()
        {
            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 1, 31);

            var feature1 = features[0];
            var feature2 = features[1];

            var team = new Team();
            feature1.AddOrUpdateWorkForTeam(team, 3, 5);
            feature2.AddOrUpdateWorkForTeam(team, 3, 15);

            var result = subject.GetSizePercentilesForProject(project, startDate, endDate).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Has.Count.EqualTo(4));
                Assert.That(result[0].Percentile, Is.EqualTo(50));
                Assert.That(result[1].Percentile, Is.EqualTo(70));
                Assert.That(result[2].Percentile, Is.EqualTo(85));
                Assert.That(result[3].Percentile, Is.EqualTo(95));
            };
        }

        [Test]
        public void GetSizePercentilesForProject_NoClosedItems_ReturnsEmpty()
        {
            var startDate = new DateTime(2077, 1, 1);
            var endDate = new DateTime(2077, 1, 31);

            var result = subject.GetSizePercentilesForProject(project, startDate, endDate).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Has.Count.EqualTo(0));
            };
        }

        [Test]
        public void GetCycleTimeDataForProject_ReturnsClosedFeatures()
        {
            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 1, 31);

            var result = subject.GetCycleTimeDataForProject(project, startDate, endDate).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Has.Count.EqualTo(2));
                Assert.That(result.Any(f => f.ReferenceId == "F1"), Is.True);
                Assert.That(result.Any(f => f.ReferenceId == "F2"), Is.True);
            };
        }

        [Test]
        public void GetCycleTimeDataForProject_FeatureClosedAtEndDate_ReturnsFeature()
        {
            var startDate = DateTime.UtcNow.AddDays(-1);
            var endDate = DateTime.UtcNow;

            var closedFeatures = features.Where(f => f.StateCategory == StateCategories.Done).AsQueryable();

            closedFeatures.First().ClosedDate = DateTime.Now;

            var result = subject.GetCycleTimeDataForProject(project, startDate, endDate).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Has.Count.EqualTo(1));
                Assert.That(result.Any(f => f.ReferenceId == "F1"), Is.True);
                Assert.That(result.Any(f => f.ReferenceId == "F2"), Is.False);
            };
        }

        [Test]
        public void GetCycleTimeDataForProject_FeatureClosedAtStartDate_ReturnsFeature()
        {
            var startDate = DateTime.Today.AddDays(-1);
            var endDate = DateTime.Today;

            var closedFeatures = features.Where(f => f.StateCategory == StateCategories.Done).AsQueryable();

            closedFeatures.First().ClosedDate = DateTime.Now.AddDays(-1);

            var result = subject.GetCycleTimeDataForProject(project, startDate, endDate).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Has.Count.EqualTo(1));
                Assert.That(result.Any(f => f.ReferenceId == "F1"), Is.True);
                Assert.That(result.Any(f => f.ReferenceId == "F2"), Is.False);
            };
        }

        [Test]
        public void InvalidateProjectMetrics_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => subject.InvalidateProjectMetrics(project));
        }

        [Test]
        public void GetMultiItemForecastPredictabilityScoreForProject_ReturnsScoreBasedOnProjectssThroughputAndHowManyForecast()
        {
            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 1, 10);

            featureRepository.Setup(x => x.GetAllByPredicate(
                It.IsAny<Expression<Func<Feature, bool>>>()))
                .Returns((Expression<Func<Feature, bool>> predicate) => features.Where(predicate.Compile()).AsQueryable());

            var howManyForecast = new HowManyForecast();
            var expectedResult = new ForecastPredictabilityScore(howManyForecast);

            forecastServiceMock.Setup(x => x.HowMany(It.IsAny<RunChartData>(), 10)).Returns(howManyForecast);

            var score = subject.GetMultiItemForecastPredictabilityScoreForProject(project, startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(score, Is.Not.Null);
                Assert.That(score.PredictabilityScore, Is.EqualTo(expectedResult.PredictabilityScore));
                foreach (var percentile in score.Percentiles)
                {
                    Assert.That(percentile.Value, Is.EqualTo(expectedResult.Percentiles.Single(p => p.Percentile == percentile.Percentile).Value));
                }
            }
            ;
        }

        [Test]
        public void GetTotalWorkItemAge_NoFeaturesInDoing_ReturnsZero()
        {
            features.Clear();
            var feature1 = new Feature
            {
                Id = 1,
                StateCategory = StateCategories.ToDo,
            };
            feature1.Projects.Add(project);
            features.Add(feature1);

            var feature2 = new Feature
            {
                Id = 2,
                StateCategory = StateCategories.Done,
            };
            feature2.Projects.Add(project);
            features.Add(feature2);

            var totalAge = subject.GetTotalWorkItemAge(project);

            Assert.That(totalAge, Is.Zero);
        }

        [Test]
        public void GetTotalWorkItemAge_FeaturesOfOtherProject_ReturnsZero()
        {
            var otherProject = new Project { Id = 999, Name = "Other Project" };
            features.Clear();
            var feature = new Feature
            {
                Id = 1,
                StateCategory = StateCategories.Doing,
                StartedDate = DateTime.UtcNow.AddDays(-5),
            };
            feature.Projects.Add(otherProject);
            features.Add(feature);

            var totalAge = subject.GetTotalWorkItemAge(project);

            Assert.That(totalAge, Is.Zero);
        }

        [Test]
        public void GetTotalWorkItemAge_SingleFeatureInProgress_ReturnsFeatureAge()
        {
            features.Clear();
            var feature = new Feature
            {
                Id = 1,
                StateCategory = StateCategories.Doing,
                StartedDate = DateTime.UtcNow.AddDays(-5),
            };
            feature.Projects.Add(project);
            features.Add(feature);

            var totalAge = subject.GetTotalWorkItemAge(project);

            Assert.That(totalAge, Is.EqualTo(6));
        }

        [Test]
        public void GetTotalWorkItemAge_MultipleFeaturesInProgress_ReturnsSumOfAges()
        {
            features.Clear();
            var feature1 = new Feature
            {
                Id = 1,
                StateCategory = StateCategories.Doing,
                StartedDate = DateTime.UtcNow.AddDays(-10),
            };
            feature1.Projects.Add(project);
            features.Add(feature1);

            var feature2 = new Feature
            {
                Id = 2,
                StateCategory = StateCategories.Doing,
                StartedDate = DateTime.UtcNow.AddDays(-5),
            };
            feature2.Projects.Add(project);
            features.Add(feature2);

            var feature3 = new Feature
            {
                Id = 3,
                StateCategory = StateCategories.Doing,
                StartedDate = DateTime.UtcNow.AddDays(-2),
            };
            feature3.Projects.Add(project);
            features.Add(feature3);

            var totalAge = subject.GetTotalWorkItemAge(project);

            // 11 + 6 + 3 = 20
            Assert.That(totalAge, Is.EqualTo(20));
        }

        [Test]
        public void GetTotalWorkItemAge_MixedStateFeatures_OnlyCountsDoingFeatures()
        {
            features.Clear();
            var feature1 = new Feature
            {
                Id = 1,
                StateCategory = StateCategories.Doing,
                StartedDate = DateTime.UtcNow.AddDays(-7),
            };
            feature1.Projects.Add(project);
            features.Add(feature1);

            var feature2 = new Feature
            {
                Id = 2,
                StateCategory = StateCategories.Done,
                StartedDate = DateTime.UtcNow.AddDays(-15),
                ClosedDate = DateTime.UtcNow.AddDays(-3),
            };
            feature2.Projects.Add(project);
            features.Add(feature2);

            var feature3 = new Feature
            {
                Id = 3,
                StateCategory = StateCategories.ToDo,
            };
            feature3.Projects.Add(project);
            features.Add(feature3);

            var totalAge = subject.GetTotalWorkItemAge(project);

            Assert.That(totalAge, Is.EqualTo(8));
        }

        [Test]
        public void GetTotalWorkItemAge_FeatureWithNoStartedDate_UsesCreatedDate()
        {
            features.Clear();
            var feature = new Feature
            {
                Id = 1,
                StateCategory = StateCategories.Doing,
                StartedDate = null,
                CreatedDate = DateTime.UtcNow.AddDays(-9),
            };
            feature.Projects.Add(project);
            features.Add(feature);

            var totalAge = subject.GetTotalWorkItemAge(project);

            Assert.That(totalAge, Is.EqualTo(10));
        }

        [Test]
        public void GetAllFeaturesForSizeChart_ReturnsOnlyDoneFeaturesInDateRange()
        {
            // Arrange
            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 1, 31);

            // Act
            var result = subject.GetAllFeaturesForSizeChart(project, startDate, endDate).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Has.Count.EqualTo(3)); // F1, F2 (Done in range), F3 (Doing)
                Assert.That(result.Any(f => f.ReferenceId == "F1"), Is.True);
                Assert.That(result.Any(f => f.ReferenceId == "F2"), Is.True);
                Assert.That(result.Any(f => f.ReferenceId == "F3"), Is.True);
            };
        }

        [Test]
        public void GetAllFeaturesForSizeChart_IncludesToDoFeatures()
        {
            // Arrange
            features.Add(new Feature
            {
                Id = 4,
                Name = "Feature 4",
                ReferenceId = "F4",
                StateCategory = StateCategories.ToDo,
                CreatedDate = DateTime.UtcNow
            });
            features.Last().Projects.Add(project);

            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 1, 31);

            // Act
            var result = subject.GetAllFeaturesForSizeChart(project, startDate, endDate).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Has.Count.EqualTo(4)); // F1, F2 (Done), F3 (Doing), F4 (ToDo)
                Assert.That(result.Any(f => f.ReferenceId == "F4"), Is.True);
                Assert.That(result.Any(f => f.StateCategory == StateCategories.ToDo), Is.True);
            };
        }

        [Test]
        public void GetAllFeaturesForSizeChart_IncludesDoingFeatures()
        {
            // Arrange
            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 1, 31);

            // Act
            var result = subject.GetAllFeaturesForSizeChart(project, startDate, endDate).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Any(f => f.ReferenceId == "F3"), Is.True);
                Assert.That(result.Any(f => f.StateCategory == StateCategories.Doing), Is.True);
            };
        }

        [Test]
        public void GetAllFeaturesForSizeChart_ExcludesDoneFeaturesOutsideDateRange()
        {
            // Arrange
            features.Add(new Feature
            {
                Id = 5,
                Name = "Feature 5",
                ReferenceId = "F5",
                StartedDate = new DateTime(2022, 12, 1),
                ClosedDate = new DateTime(2022, 12, 15),
                StateCategory = StateCategories.Done,
            });
            features.Last().Projects.Add(project);

            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 1, 31);

            // Act
            var result = subject.GetAllFeaturesForSizeChart(project, startDate, endDate).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Any(f => f.ReferenceId == "F5"), Is.False);
            };
        }

        [Test]
        public void GetAllFeaturesForSizeChart_IncludesMixedStateFeatures()
        {
            // Arrange
            features.Add(new Feature
            {
                Id = 6,
                Name = "Feature 6",
                ReferenceId = "F6",
                StateCategory = StateCategories.ToDo,
                CreatedDate = DateTime.UtcNow
            });
            features.Last().Projects.Add(project);

            features.Add(new Feature
            {
                Id = 7,
                Name = "Feature 7",
                ReferenceId = "F7",
                StateCategory = StateCategories.Doing,
                StartedDate = DateTime.UtcNow.AddDays(-2)
            });
            features.Last().Projects.Add(project);

            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 1, 31);

            // Act
            var result = subject.GetAllFeaturesForSizeChart(project, startDate, endDate).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Count(f => f.StateCategory == StateCategories.Done), Is.EqualTo(2)); // F1, F2
                Assert.That(result.Count(f => f.StateCategory == StateCategories.Doing), Is.EqualTo(2)); // F3, F7
                Assert.That(result.Count(f => f.StateCategory == StateCategories.ToDo), Is.EqualTo(1)); // F6
            };
        }

        [Test]
        public void GetAllFeaturesForSizeChart_OnlyToDoAndDoingFeatures_ReturnsAll()
        {
            // Arrange
            features.Clear();
            features.Add(new Feature
            {
                Id = 1,
                Name = "Feature 1",
                ReferenceId = "F1",
                StateCategory = StateCategories.ToDo,
                CreatedDate = DateTime.UtcNow
            });
            features.Last().Projects.Add(project);

            features.Add(new Feature
            {
                Id = 2,
                Name = "Feature 2",
                ReferenceId = "F2",
                StateCategory = StateCategories.Doing,
                StartedDate = DateTime.UtcNow.AddDays(-2)
            });
            features.Last().Projects.Add(project);

            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 1, 31);

            // Act
            var result = subject.GetAllFeaturesForSizeChart(project, startDate, endDate).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Has.Count.EqualTo(2));
                Assert.That(result.Any(f => f.StateCategory == StateCategories.ToDo), Is.True);
                Assert.That(result.Any(f => f.StateCategory == StateCategories.Doing), Is.True);
            };
        }

        [Test]
        public void GetAllFeaturesForSizeChart_NoFeatures_ReturnsEmpty()
        {
            // Arrange
            features.Clear();
            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 1, 31);

            // Act
            var result = subject.GetAllFeaturesForSizeChart(project, startDate, endDate).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Is.Empty);
            };
        }

        [Test]
        public void GetAllFeaturesForSizeChart_FeaturesOfOtherProject_NotIncluded()
        {
            // Arrange
            var otherProject = new Project { Id = 999, Name = "Other Project" };
            features.Add(new Feature
            {
                Id = 8,
                Name = "Feature 8",
                ReferenceId = "F8",
                StateCategory = StateCategories.Doing,
                StartedDate = DateTime.UtcNow.AddDays(-2)
            });
            features.Last().Projects.Add(otherProject);

            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 1, 31);

            // Act
            var result = subject.GetAllFeaturesForSizeChart(project, startDate, endDate).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Any(f => f.ReferenceId == "F8"), Is.False);
            };
        }

        private void SetupTestData()
        {
            project = new Project
            {
                Id = 1,
                Name = "Test Project"
            };

            var jan2 = new DateTime(2023, 1, 2, 0, 0, 0, DateTimeKind.Utc);
            var jan4 = new DateTime(2023, 1, 4, 0, 0, 0, DateTimeKind.Utc);
            var jan5 = new DateTime(2023, 1, 5, 0, 0, 0, DateTimeKind.Utc);
            var jan10 = new DateTime(2023, 1, 10, 0, 0, 0, DateTimeKind.Utc);

            features = new List<Feature>
            {
                new Feature
                {
                    Id = 1,
                    Name = "Feature 1",
                    ReferenceId = "F1",
                    StartedDate = jan2,
                    ClosedDate = jan5,
                    StateCategory = StateCategories.Done,
                },
                new Feature
                {
                    Id = 2,
                    Name = "Feature 2",
                    ReferenceId = "F2",
                    StartedDate = jan4,
                    ClosedDate = jan10,
                    StateCategory = StateCategories.Done,
                },
                new Feature
                {
                    Id = 3,
                    Name = "Feature 3",
                    ReferenceId = "F3",
                    StartedDate = jan2,
                    StateCategory = StateCategories.Doing,
                }
            };

            features.ForEach(f => f.Projects.Add(project));
        }
    }
}