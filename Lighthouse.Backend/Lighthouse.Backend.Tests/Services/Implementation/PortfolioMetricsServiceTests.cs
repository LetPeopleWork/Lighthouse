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
    public class PortfolioMetricsServiceTests
    {
        private Mock<ILogger<PortfolioMetricsService>> logger;
        private Mock<IRepository<Feature>> featureRepository;
        private Mock<IAppSettingService> appSettingService;
        private Mock<IForecastService> forecastServiceMock;

        private PortfolioMetricsService subject;
        private Portfolio project;
        private List<Feature> features;

        [SetUp]
        public void Setup()
        {
            logger = new Mock<ILogger<PortfolioMetricsService>>();
            featureRepository = new Mock<IRepository<Feature>>();
            appSettingService = new Mock<IAppSettingService>();

            appSettingService.Setup(m => m.GetFeatureRefreshSettings()).Returns(new RefreshSettings { Interval = 30 });

            forecastServiceMock = new Mock<IForecastService>();
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(sp => sp.GetService(typeof(IForecastService)))
                .Returns(forecastServiceMock.Object);

            subject = new PortfolioMetricsService(logger.Object, featureRepository.Object, appSettingService.Object, serviceProvider.Object);

            featureRepository.Setup(x => x.GetAllByPredicate(
                    It.IsAny<Expression<Func<Feature, bool>>>()))
                .Returns((Expression<Func<Feature, bool>> predicate) => features.Where(predicate.Compile()).AsQueryable());

            SetupTestData();
        }

        [Test]
        public void GetThroughputForProject_ReturnsRunChartDataWithCorrectValues()
        {
            // Arrange
            var startDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(2023, 1, 10, 0, 0, 0, DateTimeKind.Utc);

            featureRepository.Setup(x => x.GetAllByPredicate(
                It.IsAny<Expression<Func<Feature, bool>>>()))
                .Returns((Expression<Func<Feature, bool>> predicate) => features.Where(predicate.Compile()).AsQueryable());

            // Act
            var result = subject.GetThroughputForPortfolio(project, startDate, endDate);

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.WorkItemsPerUnitOfTime, Has.Count.EqualTo(10));
                Assert.That(result.Total, Is.EqualTo(2));
            }
        }

        [Test]
        public void GetFeaturesInProgressOverTimeForProject_ReturnsCorrectRunChartData()
        {
            // Arrange
            var startDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(2023, 1, 5, 0, 0, 0, DateTimeKind.Utc);

            featureRepository.Setup(x => x.GetAllByPredicate(
                It.IsAny<Expression<Func<Feature, bool>>>()))
                .Returns((Expression<Func<Feature, bool>> predicate) => features.Where(predicate.Compile()).AsQueryable());

            // Act
            var result = subject.GetFeaturesInProgressOverTimeForPortfolio(project, startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.WorkItemsPerUnitOfTime, Has.Count.EqualTo(5));
            }
        }

        [Test]
        public void GetStartedItemsForProject_GivenStartDate_ReturnsStartedItemsPerDayFromThisRange()
        {
            var startDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(2023, 1, 3, 0, 0, 0, DateTimeKind.Utc);

            featureRepository.Setup(x => x.GetAllByPredicate(
                It.IsAny<Expression<Func<Feature, bool>>>()))
                .Returns((Expression<Func<Feature, bool>> predicate) => features.Where(predicate.Compile()).AsQueryable());

            var throughput = subject.GetStartedItemsForPortfolio(project, startDate, endDate);

            Assert.That(throughput.Total, Is.EqualTo(2));
        }

        [Test]
        public void GetInProgressFeaturesForProject_ReturnsActiveFeatures()
        {
            // Act
            var result = subject.GetInProgressFeaturesForPortfolio(project).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Has.Count.EqualTo(1));
                Assert.That(result[0].ReferenceId, Is.EqualTo("F3"));
            }
        }

        [Test]
        public void GetCycleTimePercentilesForProject_ReturnsCorrectPercentileValues()
        {
            var startDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(2023, 1, 31, 0, 0, 0, DateTimeKind.Utc);

            var result = subject.GetCycleTimePercentilesForPortfolio(project, startDate, endDate).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Has.Count.EqualTo(4));
                Assert.That(result[0].Percentile, Is.EqualTo(50));
                Assert.That(result[1].Percentile, Is.EqualTo(70));
                Assert.That(result[2].Percentile, Is.EqualTo(85));
                Assert.That(result[3].Percentile, Is.EqualTo(95));
            }
        }

        [Test]
        public void GetCycleTimePercentilesForProject_NoClosedItems_ReturnsEmpty()
        {
            var startDate = new DateTime(2077, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(2077, 1, 31, 0, 0, 0, DateTimeKind.Utc);

            var result = subject.GetCycleTimePercentilesForPortfolio(project, startDate, endDate).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Has.Count.EqualTo(0));
            }
        }

        [Test]
        public void GetSizePercentilesForProject_ReturnsCorrectPercentileValues()
        {
            var startDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(2023, 1, 31, 0, 0, 0, DateTimeKind.Utc);

            var feature1 = features[0];
            var feature2 = features[1];

            var team = new Team();
            feature1.AddOrUpdateWorkForTeam(team, 3, 5);
            feature2.AddOrUpdateWorkForTeam(team, 3, 15);

            var result = subject.GetSizePercentilesForPortfolio(project, startDate, endDate).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Has.Count.EqualTo(4));
                Assert.That(result[0].Percentile, Is.EqualTo(50));
                Assert.That(result[1].Percentile, Is.EqualTo(70));
                Assert.That(result[2].Percentile, Is.EqualTo(85));
                Assert.That(result[3].Percentile, Is.EqualTo(95));
            }
        }

        [Test]
        public void GetSizePercentilesForProject_NoClosedItems_ReturnsEmpty()
        {
            var startDate = new DateTime(2077, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(2077, 1, 31, 0, 0, 0, DateTimeKind.Utc);

            var result = subject.GetSizePercentilesForPortfolio(project, startDate, endDate).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Has.Count.EqualTo(0));
            }
        }

        [Test]
        public void GetCycleTimeDataForProject_ReturnsClosedFeatures()
        {
            var startDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(2023, 1, 31, 0, 0, 0, DateTimeKind.Utc);

            var result = subject.GetCycleTimeDataForPortfolio(project, startDate, endDate).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Has.Count.EqualTo(2));
                Assert.That(result.Any(f => f.ReferenceId == "F1"), Is.True);
                Assert.That(result.Any(f => f.ReferenceId == "F2"), Is.True);
            }
        }

        [Test]
        public void GetCycleTimeDataForProject_FeatureClosedAtEndDate_ReturnsFeature()
        {
            var startDate = DateTime.UtcNow.AddDays(-1);
            var endDate = DateTime.UtcNow;

            var closedFeatures = features.Where(f => f.StateCategory == StateCategories.Done).AsQueryable();

            closedFeatures.First().ClosedDate = DateTime.Now;

            var result = subject.GetCycleTimeDataForPortfolio(project, startDate, endDate).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Has.Count.EqualTo(1));
                Assert.That(result.Any(f => f.ReferenceId == "F1"), Is.True);
                Assert.That(result.Any(f => f.ReferenceId == "F2"), Is.False);
            }
        }

        [Test]
        public void GetCycleTimeDataForProject_FeatureClosedAtStartDate_ReturnsFeature()
        {
            var startDate = DateTime.Today.AddDays(-1);
            var endDate = DateTime.Today;

            var closedFeatures = features.Where(f => f.StateCategory == StateCategories.Done).AsQueryable();

            closedFeatures.First().ClosedDate = DateTime.Now.AddDays(-1);

            var result = subject.GetCycleTimeDataForPortfolio(project, startDate, endDate).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Has.Count.EqualTo(1));
                Assert.That(result.Any(f => f.ReferenceId == "F1"), Is.True);
                Assert.That(result.Any(f => f.ReferenceId == "F2"), Is.False);
            }
        }

        [Test]
        public void InvalidateProjectMetrics_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => subject.InvalidatePortfolioMetrics(project));
        }

        [Test]
        public void GetMultiItemForecastPredictabilityScoreForProject_ReturnsScoreBasedOnProjectssThroughputAndHowManyForecast()
        {
            var startDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(2023, 1, 10, 0, 0, 0, DateTimeKind.Utc);

            featureRepository.Setup(x => x.GetAllByPredicate(
                It.IsAny<Expression<Func<Feature, bool>>>()))
                .Returns((Expression<Func<Feature, bool>> predicate) => features.Where(predicate.Compile()).AsQueryable());

            var howManyForecast = new HowManyForecast();
            var expectedResult = new ForecastPredictabilityScore(howManyForecast);

            forecastServiceMock.Setup(x => x.HowMany(It.IsAny<RunChartData>(), 10)).Returns(howManyForecast);

            var score = subject.GetMultiItemForecastPredictabilityScoreForPortfolio(project, startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(score, Is.Not.Null);
                Assert.That(score.PredictabilityScore, Is.EqualTo(expectedResult.PredictabilityScore));
                foreach (var percentile in score.Percentiles)
                {
                    Assert.That(percentile.Value, Is.EqualTo(expectedResult.Percentiles.Single(p => p.Percentile == percentile.Percentile).Value));
                }
            }
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
            feature1.Portfolios.Add(project);
            features.Add(feature1);

            var feature2 = new Feature
            {
                Id = 2,
                StateCategory = StateCategories.Done,
            };
            feature2.Portfolios.Add(project);
            features.Add(feature2);

            var totalAge = subject.GetTotalWorkItemAge(project);

            Assert.That(totalAge, Is.Zero);
        }

        [Test]
        public void GetTotalWorkItemAge_FeaturesOfOtherProject_ReturnsZero()
        {
            var otherProject = new Portfolio { Id = 999, Name = "Other Project" };
            features.Clear();
            var feature = new Feature
            {
                Id = 1,
                StateCategory = StateCategories.Doing,
                StartedDate = DateTime.UtcNow.AddDays(-5),
            };
            feature.Portfolios.Add(otherProject);
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
            feature.Portfolios.Add(project);
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
            feature1.Portfolios.Add(project);
            features.Add(feature1);

            var feature2 = new Feature
            {
                Id = 2,
                StateCategory = StateCategories.Doing,
                StartedDate = DateTime.UtcNow.AddDays(-5),
            };
            feature2.Portfolios.Add(project);
            features.Add(feature2);

            var feature3 = new Feature
            {
                Id = 3,
                StateCategory = StateCategories.Doing,
                StartedDate = DateTime.UtcNow.AddDays(-2),
            };
            feature3.Portfolios.Add(project);
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
            feature1.Portfolios.Add(project);
            features.Add(feature1);

            var feature2 = new Feature
            {
                Id = 2,
                StateCategory = StateCategories.Done,
                StartedDate = DateTime.UtcNow.AddDays(-15),
                ClosedDate = DateTime.UtcNow.AddDays(-3),
            };
            feature2.Portfolios.Add(project);
            features.Add(feature2);

            var feature3 = new Feature
            {
                Id = 3,
                StateCategory = StateCategories.ToDo,
            };
            feature3.Portfolios.Add(project);
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
            feature.Portfolios.Add(project);
            features.Add(feature);

            var totalAge = subject.GetTotalWorkItemAge(project);

            Assert.That(totalAge, Is.EqualTo(10));
        }

        [Test]
        public void GetAllFeaturesForSizeChart_ReturnsOnlyDoneFeaturesInDateRange()
        {
            // Arrange
            var startDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(2023, 1, 31, 0, 0, 0, DateTimeKind.Utc);

            // Act
            var result = subject.GetAllFeaturesForSizeChart(project, startDate, endDate).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Has.Count.EqualTo(3)); // F1, F2 (Done in range), F3 (Doing)
                Assert.That(result.Any(f => f.ReferenceId == "F1"), Is.True);
                Assert.That(result.Any(f => f.ReferenceId == "F2"), Is.True);
                Assert.That(result.Any(f => f.ReferenceId == "F3"), Is.True);
            }
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
            features[features.Count - 1].Portfolios.Add(project);

            var startDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(2023, 1, 31, 0, 0, 0, DateTimeKind.Utc);

            // Act
            var result = subject.GetAllFeaturesForSizeChart(project, startDate, endDate).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Has.Count.EqualTo(4)); // F1, F2 (Done), F3 (Doing), F4 (To-Do)
                Assert.That(result.Any(f => f.ReferenceId == "F4"), Is.True);
                Assert.That(result.Any(f => f.StateCategory == StateCategories.ToDo), Is.True);
            }
        }

        [Test]
        public void GetAllFeaturesForSizeChart_IncludesDoingFeatures()
        {
            // Arrange
            var startDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(2023, 1, 31, 0, 0, 0, DateTimeKind.Utc);

            // Act
            var result = subject.GetAllFeaturesForSizeChart(project, startDate, endDate).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Any(f => f.ReferenceId == "F3"), Is.True);
                Assert.That(result.Any(f => f.StateCategory == StateCategories.Doing), Is.True);
            }
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
                StartedDate = new DateTime(2022, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                ClosedDate = new DateTime(2022, 12, 15, 0, 0, 0, DateTimeKind.Utc),
                StateCategory = StateCategories.Done,
            });
            features[features.Count - 1].Portfolios.Add(project);

            var startDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(2023, 1, 31, 0, 0, 0, DateTimeKind.Utc);

            // Act
            var result = subject.GetAllFeaturesForSizeChart(project, startDate, endDate).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Any(f => f.ReferenceId == "F5"), Is.False);
            }
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
            features[features.Count - 1].Portfolios.Add(project);

            features.Add(new Feature
            {
                Id = 7,
                Name = "Feature 7",
                ReferenceId = "F7",
                StateCategory = StateCategories.Doing,
                StartedDate = DateTime.UtcNow.AddDays(-2)
            });
            features[features.Count - 1].Portfolios.Add(project);

            var startDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(2023, 1, 31, 0, 0, 0, DateTimeKind.Utc);

            // Act
            var result = subject.GetAllFeaturesForSizeChart(project, startDate, endDate).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Count(f => f.StateCategory == StateCategories.Done), Is.EqualTo(2)); // F1, F2
                Assert.That(result.Count(f => f.StateCategory == StateCategories.Doing), Is.EqualTo(2)); // F3, F7
                Assert.That(result.Count(f => f.StateCategory == StateCategories.ToDo), Is.EqualTo(1)); // F6
            }
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
            features[features.Count - 1].Portfolios.Add(project);

            features.Add(new Feature
            {
                Id = 2,
                Name = "Feature 2",
                ReferenceId = "F2",
                StateCategory = StateCategories.Doing,
                StartedDate = DateTime.UtcNow.AddDays(-2)
            });
            features[features.Count - 1].Portfolios.Add(project);

            var startDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(2023, 1, 31, 0, 0, 0, DateTimeKind.Utc);

            // Act
            var result = subject.GetAllFeaturesForSizeChart(project, startDate, endDate).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Has.Count.EqualTo(2));
                Assert.That(result.Any(f => f.StateCategory == StateCategories.ToDo), Is.True);
                Assert.That(result.Any(f => f.StateCategory == StateCategories.Doing), Is.True);
            }
        }

        [Test]
        public void GetAllFeaturesForSizeChart_NoFeatures_ReturnsEmpty()
        {
            // Arrange
            features.Clear();
            var startDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(2023, 1, 31, 0, 0, 0, DateTimeKind.Utc);

            // Act
            var result = subject.GetAllFeaturesForSizeChart(project, startDate, endDate).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Is.Empty);
            }
        }

        [Test]
        public void GetAllFeaturesForSizeChart_FeaturesOfOtherProject_NotIncluded()
        {
            // Arrange
            var otherProject = new Portfolio { Id = 999, Name = "Other Project" };
            features.Add(new Feature
            {
                Id = 8,
                Name = "Feature 8",
                ReferenceId = "F8",
                StateCategory = StateCategories.Doing,
                StartedDate = DateTime.UtcNow.AddDays(-2)
            });
            features[features.Count - 1].Portfolios.Add(otherProject);

            var startDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(2023, 1, 31, 0, 0, 0, DateTimeKind.Utc);

            // Act
            var result = subject.GetAllFeaturesForSizeChart(project, startDate, endDate).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Any(f => f.ReferenceId == "F8"), Is.False);
            }
        }

        // --- Process Behaviour Chart Tests ---

        [Test]
        public void GetThroughputProcessBehaviourChart_BaselineDatesNotSet_ShortRange_ReturnsBaselineInvalid()
        {
            project.ProcessBehaviourChartBaselineStartDate = null;
            project.ProcessBehaviourChartBaselineEndDate = null;

            var result = subject.GetThroughputProcessBehaviourChart(project, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(BaselineStatus.BaselineInvalid));
                Assert.That(result.DataPoints, Is.Empty);
                Assert.That(result.BaselineConfigured, Is.False);
            }
        }

        [Test]
        public void GetThroughputProcessBehaviourChart_BaselineDatesNotSet_LongRange_ReturnsReadyWithImplicitBaseline()
        {
            project.ProcessBehaviourChartBaselineStartDate = null;
            project.ProcessBehaviourChartBaselineEndDate = null;

            var displayStart = DateTime.UtcNow.AddDays(-30).Date;
            var displayEnd = DateTime.UtcNow.Date;

            var result = subject.GetThroughputProcessBehaviourChart(project, displayStart, displayEnd);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(BaselineStatus.Ready));
                Assert.That(result.BaselineConfigured, Is.False);
                Assert.That(result.DataPoints, Has.Length.EqualTo(31));
            }
        }

        [Test]
        public void GetThroughputProcessBehaviourChart_ValidBaseline_BaselineConfiguredIsTrue()
        {
            project.ProcessBehaviourChartBaselineStartDate = DateTime.UtcNow.AddDays(-60).Date;
            project.ProcessBehaviourChartBaselineEndDate = DateTime.UtcNow.AddDays(-16).Date;

            var result = subject.GetThroughputProcessBehaviourChart(project, DateTime.UtcNow.AddDays(-7).Date, DateTime.UtcNow.Date);

            Assert.That(result.BaselineConfigured, Is.True);
        }

        [Test]
        public void GetWipProcessBehaviourChart_BaselineDatesNotSet_ShortRange_ReturnsBaselineInvalid()
        {
            project.ProcessBehaviourChartBaselineStartDate = null;
            project.ProcessBehaviourChartBaselineEndDate = null;

            var result = subject.GetWipProcessBehaviourChart(project, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(BaselineStatus.BaselineInvalid));
                Assert.That(result.DataPoints, Is.Empty);
                Assert.That(result.BaselineConfigured, Is.False);
            }
        }

        [Test]
        public void GetWipProcessBehaviourChart_BaselineDatesNotSet_LongRange_ReturnsReadyWithImplicitBaseline()
        {
            project.ProcessBehaviourChartBaselineStartDate = null;
            project.ProcessBehaviourChartBaselineEndDate = null;

            var displayStart = DateTime.UtcNow.AddDays(-30).Date;
            var displayEnd = DateTime.UtcNow.Date;

            var result = subject.GetWipProcessBehaviourChart(project, displayStart, displayEnd);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(BaselineStatus.Ready));
                Assert.That(result.BaselineConfigured, Is.False);
                Assert.That(result.DataPoints, Has.Length.EqualTo(31));
            }
        }

        [Test]
        public void GetTotalWorkItemAgeProcessBehaviourChart_BaselineDatesNotSet_ShortRange_ReturnsBaselineInvalid()
        {
            project.ProcessBehaviourChartBaselineStartDate = null;
            project.ProcessBehaviourChartBaselineEndDate = null;

            var result = subject.GetTotalWorkItemAgeProcessBehaviourChart(project, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(BaselineStatus.BaselineInvalid));
                Assert.That(result.DataPoints, Is.Empty);
                Assert.That(result.BaselineConfigured, Is.False);
            }
        }

        [Test]
        public void GetTotalWorkItemAgeProcessBehaviourChart_BaselineDatesNotSet_LongRange_ReturnsReadyWithImplicitBaseline()
        {
            project.ProcessBehaviourChartBaselineStartDate = null;
            project.ProcessBehaviourChartBaselineEndDate = null;

            var displayStart = DateTime.UtcNow.AddDays(-30).Date;
            var displayEnd = DateTime.UtcNow.Date;

            var result = subject.GetTotalWorkItemAgeProcessBehaviourChart(project, displayStart, displayEnd);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(BaselineStatus.Ready));
                Assert.That(result.BaselineConfigured, Is.False);
                Assert.That(result.DataPoints, Has.Length.EqualTo(31));
            }
        }

        [Test]
        public void GetCycleTimeProcessBehaviourChart_BaselineDatesNotSet_ShortRange_ReturnsBaselineInvalid()
        {
            project.ProcessBehaviourChartBaselineStartDate = null;
            project.ProcessBehaviourChartBaselineEndDate = null;

            var result = subject.GetCycleTimeProcessBehaviourChart(project, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(BaselineStatus.BaselineInvalid));
                Assert.That(result.DataPoints, Is.Empty);
                Assert.That(result.BaselineConfigured, Is.False);
            }
        }

        [Test]
        public void GetCycleTimeProcessBehaviourChart_BaselineDatesNotSet_LongRange_ReturnsReadyWithImplicitBaseline()
        {
            project.ProcessBehaviourChartBaselineStartDate = null;
            project.ProcessBehaviourChartBaselineEndDate = null;

            var displayStart = DateTime.UtcNow.AddDays(-30).Date;
            var displayEnd = DateTime.UtcNow.Date;

            // Add a closed feature in the display range
            var feature = new Feature
            {
                Id = 99,
                Name = "Feature 99",
                ReferenceId = "F99",
                StartedDate = displayStart,
                ClosedDate = displayStart.AddDays(5),
                StateCategory = StateCategories.Done,
            };
            feature.Portfolios.Add(project);
            features.Add(feature);

            var result = subject.GetCycleTimeProcessBehaviourChart(project, displayStart, displayEnd);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(BaselineStatus.Ready));
                Assert.That(result.BaselineConfigured, Is.False);
                Assert.That(result.DataPoints, Has.Length.EqualTo(1));
            }
        }

        [Test]
        public void GetFeatureSizeProcessBehaviourChart_BaselineDatesNotSet_ShortRange_ReturnsBaselineInvalid()
        {
            project.ProcessBehaviourChartBaselineStartDate = null;
            project.ProcessBehaviourChartBaselineEndDate = null;

            var result = subject.GetFeatureSizeProcessBehaviourChart(project, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(BaselineStatus.BaselineInvalid));
                Assert.That(result.DataPoints, Is.Empty);
                Assert.That(result.BaselineConfigured, Is.False);
            }
        }

        [Test]
        public void GetFeatureSizeProcessBehaviourChart_NoFeaturesWithNonZeroSize_ReturnsInsufficientData()
        {
            project.ProcessBehaviourChartBaselineStartDate = null;
            project.ProcessBehaviourChartBaselineEndDate = null;

            // Default test features have Size == 0 (no FeatureWork assigned)
            var displayStart = DateTime.UtcNow.AddDays(-30).Date;
            var displayEnd = DateTime.UtcNow.Date;

            var feature = new Feature
            {
                Id = 99,
                Name = "Feature 99",
                ReferenceId = "F99",
                StartedDate = displayStart,
                ClosedDate = displayStart.AddDays(5),
                StateCategory = StateCategories.Done,
            };
            feature.Portfolios.Add(project);
            features.Add(feature);

            var result = subject.GetFeatureSizeProcessBehaviourChart(project, displayStart, displayEnd);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(BaselineStatus.InsufficientData));
                Assert.That(result.DataPoints, Is.Empty);
            }
        }

        [Test]
        public void GetFeatureSizeProcessBehaviourChart_BaselineDatesNotSet_LongRange_WithFeaturesWithSize_ReturnsReady()
        {
            project.ProcessBehaviourChartBaselineStartDate = null;
            project.ProcessBehaviourChartBaselineEndDate = null;

            var displayStart = DateTime.UtcNow.AddDays(-30).Date;
            var displayEnd = DateTime.UtcNow.Date;

            var team = new Team();
            var feature = new Feature
            {
                Id = 99,
                Name = "Feature 99",
                ReferenceId = "F99",
                StartedDate = displayStart,
                ClosedDate = displayStart.AddDays(5),
                StateCategory = StateCategories.Done,
            };
            feature.AddOrUpdateWorkForTeam(team, 2, 10);
            feature.Portfolios.Add(project);
            features.Add(feature);

            var result = subject.GetFeatureSizeProcessBehaviourChart(project, displayStart, displayEnd);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(BaselineStatus.Ready));
                Assert.That(result.BaselineConfigured, Is.False);
                Assert.That(result.XAxisKind, Is.EqualTo(XAxisKind.DateTime));
                Assert.That(result.DataPoints, Has.Length.EqualTo(1));
                Assert.That(result.DataPoints[0].YValue, Is.EqualTo(10));
            }
        }

        [Test]
        public void GetFeatureSizeProcessBehaviourChart_ValidBaseline_BaselineConfiguredIsTrue()
        {
            project.ProcessBehaviourChartBaselineStartDate = DateTime.UtcNow.AddDays(-60).Date;
            project.ProcessBehaviourChartBaselineEndDate = DateTime.UtcNow.AddDays(-16).Date;

            var team = new Team();
            var baselineFeature = new Feature
            {
                Id = 98,
                Name = "Baseline Feature",
                ReferenceId = "FB",
                StartedDate = project.ProcessBehaviourChartBaselineStartDate,
                ClosedDate = project.ProcessBehaviourChartBaselineStartDate.Value.AddDays(5),
                StateCategory = StateCategories.Done,
            };
            baselineFeature.AddOrUpdateWorkForTeam(team, 3, 8);
            baselineFeature.Portfolios.Add(project);
            features.Add(baselineFeature);

            var result = subject.GetFeatureSizeProcessBehaviourChart(project, DateTime.UtcNow.AddDays(-7).Date, DateTime.UtcNow.Date);

            Assert.That(result.BaselineConfigured, Is.True);
        }

        #region GetFeatureSizeEstimationData

        [Test]
        public void GetFeatureSizeEstimationData_NoEstimationFieldConfigured_ReturnsNotConfigured()
        {
            project.EstimationAdditionalFieldDefinitionId = null;

            var result = subject.GetFeatureSizeEstimationData(project, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(EstimationVsCycleTimeStatus.NotConfigured));
                Assert.That(result.FeatureEstimations, Is.Empty);
            }
        }

        [Test]
        public void GetFeatureSizeEstimationData_EstimationConfiguredButNoFeatures_ReturnsNoData()
        {
            project.EstimationAdditionalFieldDefinitionId = 42;
            features.Clear();

            var result = subject.GetFeatureSizeEstimationData(project, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(EstimationVsCycleTimeStatus.NoData));
                Assert.That(result.FeatureEstimations, Is.Empty);
            }
        }

        [Test]
        public void GetFeatureSizeEstimationData_NumericEstimates_ReturnsMappedFeatureEstimations()
        {
            const int fieldId = 42;
            project.EstimationAdditionalFieldDefinitionId = fieldId;
            project.UseNonNumericEstimation = false;

            var startDate = DateTime.UtcNow.AddDays(-30).Date;
            var endDate = DateTime.UtcNow.Date;

            // Feature 1 is Done with an estimate
            features[0].AdditionalFieldValues[fieldId] = "5";
            features[0].ClosedDate = startDate.AddDays(3);

            // Feature 3 is Doing with an estimate
            features[2].AdditionalFieldValues[fieldId] = "3";

            var result = subject.GetFeatureSizeEstimationData(project, startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(EstimationVsCycleTimeStatus.Ready));
                Assert.That(result.FeatureEstimations, Has.Count.EqualTo(2));
                Assert.That(result.FeatureEstimations.Any(e => e.FeatureId == features[0].Id && e.EstimationNumericValue == 5.0), Is.True);
                Assert.That(result.FeatureEstimations.Any(e => e.FeatureId == features[2].Id && e.EstimationNumericValue == 3.0), Is.True);
            }
        }

        [Test]
        public void GetFeatureSizeEstimationData_IncludesAllStates_NotJustClosedFeatures()
        {
            const int fieldId = 42;
            project.EstimationAdditionalFieldDefinitionId = fieldId;

            var startDate = DateTime.UtcNow.AddDays(-30).Date;
            var endDate = DateTime.UtcNow.Date;

            // Add a ToDo feature with estimate
            var todoFeature = new Feature
            {
                Id = 10,
                Name = "ToDo Feature",
                ReferenceId = "F10",
                StateCategory = StateCategories.ToDo,
            };
            todoFeature.Portfolios.Add(project);
            todoFeature.AdditionalFieldValues[fieldId] = "2";
            features.Add(todoFeature);

            // Done feature with estimate
            features[0].ClosedDate = startDate.AddDays(3);
            features[0].AdditionalFieldValues[fieldId] = "5";

            // Doing feature with estimate
            features[2].AdditionalFieldValues[fieldId] = "8";

            var result = subject.GetFeatureSizeEstimationData(project, startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(EstimationVsCycleTimeStatus.Ready));
                Assert.That(result.FeatureEstimations, Has.Count.EqualTo(3));
                Assert.That(result.FeatureEstimations.Any(e => e.FeatureId == todoFeature.Id), Is.True);
                Assert.That(result.FeatureEstimations.Any(e => e.FeatureId == features[0].Id), Is.True);
                Assert.That(result.FeatureEstimations.Any(e => e.FeatureId == features[2].Id), Is.True);
            }
        }

        [Test]
        public void GetFeatureSizeEstimationData_NonNumericMode_ReturnsOrdinalValues()
        {
            const int fieldId = 42;
            project.EstimationAdditionalFieldDefinitionId = fieldId;
            project.UseNonNumericEstimation = true;
            project.EstimationCategoryValues = ["XS", "S", "M", "L", "XL"];

            var startDate = DateTime.UtcNow.AddDays(-30).Date;
            var endDate = DateTime.UtcNow.Date;

            features[0].ClosedDate = startDate.AddDays(3);
            features[0].AdditionalFieldValues[fieldId] = "M";

            features[2].AdditionalFieldValues[fieldId] = "XL";

            var result = subject.GetFeatureSizeEstimationData(project, startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(EstimationVsCycleTimeStatus.Ready));
                Assert.That(result.UseNonNumericEstimation, Is.True);
                Assert.That(result.CategoryValues, Is.EqualTo(new List<string> { "XS", "S", "M", "L", "XL" }));

                var mEstimation = result.FeatureEstimations.First(e => e.FeatureId == features[0].Id);
                Assert.That(mEstimation.EstimationNumericValue, Is.EqualTo(2)); // index of "M"
                Assert.That(mEstimation.EstimationDisplayValue, Is.EqualTo("M"));

                var xlEstimation = result.FeatureEstimations.First(e => e.FeatureId == features[2].Id);
                Assert.That(xlEstimation.EstimationNumericValue, Is.EqualTo(4)); // index of "XL"
                Assert.That(xlEstimation.EstimationDisplayValue, Is.EqualTo("XL"));
            }
        }

        [Test]
        public void GetFeatureSizeEstimationData_InvalidAndMissingEstimates_ExcludedFromResults()
        {
            const int fieldId = 42;
            project.EstimationAdditionalFieldDefinitionId = fieldId;
            project.UseNonNumericEstimation = false;

            var startDate = DateTime.UtcNow.AddDays(-30).Date;
            var endDate = DateTime.UtcNow.Date;

            // Feature 1: valid estimate
            features[0].ClosedDate = startDate.AddDays(3);
            features[0].AdditionalFieldValues[fieldId] = "5";

            // Feature 2: invalid estimate
            features[1].ClosedDate = startDate.AddDays(7);
            features[1].AdditionalFieldValues[fieldId] = "not-a-number";

            // Feature 3 (Doing): no estimate at all (missing key)

            var result = subject.GetFeatureSizeEstimationData(project, startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(EstimationVsCycleTimeStatus.Ready));
                Assert.That(result.FeatureEstimations, Has.Count.EqualTo(1));
                Assert.That(result.FeatureEstimations[0].FeatureId, Is.EqualTo(features[0].Id));
            }
        }

        [Test]
        public void GetFeatureSizeEstimationData_PassesEstimationUnit()
        {
            const int fieldId = 42;
            project.EstimationAdditionalFieldDefinitionId = fieldId;
            project.EstimationUnit = "Story Points";

            var startDate = DateTime.UtcNow.AddDays(-30).Date;
            var endDate = DateTime.UtcNow.Date;

            features[0].ClosedDate = startDate.AddDays(3);
            features[0].AdditionalFieldValues[fieldId] = "5";

            var result = subject.GetFeatureSizeEstimationData(project, startDate, endDate);

            Assert.That(result.EstimationUnit, Is.EqualTo("Story Points"));
        }

        [Test]
        public void GetFeatureSizeEstimationData_AllInvalidEstimates_ReturnsNoData()
        {
            const int fieldId = 42;
            project.EstimationAdditionalFieldDefinitionId = fieldId;
            project.UseNonNumericEstimation = false;

            var startDate = DateTime.UtcNow.AddDays(-30).Date;
            var endDate = DateTime.UtcNow.Date;

            features[0].ClosedDate = startDate.AddDays(3);
            features[0].AdditionalFieldValues[fieldId] = "abc";

            features[2].AdditionalFieldValues[fieldId] = "xyz";

            var result = subject.GetFeatureSizeEstimationData(project, startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(EstimationVsCycleTimeStatus.NoData));
                Assert.That(result.FeatureEstimations, Is.Empty);
            }
        }

        [Test]
        public void GetFeatureSizeEstimationData_DecimalEstimates_PreservesDecimals()
        {
            const int fieldId = 42;
            project.EstimationAdditionalFieldDefinitionId = fieldId;

            var startDate = DateTime.UtcNow.AddDays(-30).Date;
            var endDate = DateTime.UtcNow.Date;

            features[0].ClosedDate = startDate.AddDays(3);
            features[0].AdditionalFieldValues[fieldId] = "3.5";

            var result = subject.GetFeatureSizeEstimationData(project, startDate, endDate);

            Assert.That(result.FeatureEstimations[0].EstimationNumericValue, Is.EqualTo(3.5));
        }

        [Test]
        public void GetFeatureSizeEstimationData_PerFeatureMapping_EachFeatureHasOwnEstimation()
        {
            const int fieldId = 42;
            project.EstimationAdditionalFieldDefinitionId = fieldId;

            var startDate = DateTime.UtcNow.AddDays(-30).Date;
            var endDate = DateTime.UtcNow.Date;

            // Two features with the same estimate value still get separate entries
            features[0].ClosedDate = startDate.AddDays(3);
            features[0].AdditionalFieldValues[fieldId] = "5";

            features[1].ClosedDate = startDate.AddDays(7);
            features[1].AdditionalFieldValues[fieldId] = "5";

            var result = subject.GetFeatureSizeEstimationData(project, startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.FeatureEstimations, Has.Count.EqualTo(2));
                Assert.That(result.FeatureEstimations[0].FeatureId, Is.Not.EqualTo(result.FeatureEstimations[1].FeatureId));
                Assert.That(result.FeatureEstimations[0].EstimationNumericValue, Is.EqualTo(5.0));
                Assert.That(result.FeatureEstimations[1].EstimationNumericValue, Is.EqualTo(5.0));
            }
        }

        #endregion

        private void SetupTestData()
        {
            project = new Portfolio
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

            features.ForEach(f => f.Portfolios.Add(project));
        }
    }
}