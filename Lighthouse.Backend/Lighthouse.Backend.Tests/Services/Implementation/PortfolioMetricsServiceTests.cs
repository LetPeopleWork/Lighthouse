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
        private Mock<IFeatureStateTransitionRepository> featureStateTransitionRepository;

        private PortfolioMetricsService subject;
        private Portfolio portfolio;
        private List<Feature> features;

        private static readonly string[] ExpectedBlockedEligibleReferenceIds = ["F3", "F4", "F5"];

        [SetUp]
        public void Setup()
        {
            logger = new Mock<ILogger<PortfolioMetricsService>>();
            featureRepository = new Mock<IRepository<Feature>>();
            appSettingService = new Mock<IAppSettingService>();

            appSettingService.Setup(m => m.GetFeatureRefreshSettings()).Returns(new RefreshSettings { Interval = 30 });

            forecastServiceMock = new Mock<IForecastService>();
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(sp => sp.GetService(typeof(Lighthouse.Backend.Cache.Cache<string, object>)))
                .Returns(new Lighthouse.Backend.Cache.Cache<string, object>());
            serviceProvider.Setup(sp => sp.GetService(typeof(IForecastService)))
                .Returns(forecastServiceMock.Object);

            featureStateTransitionRepository = new Mock<IFeatureStateTransitionRepository>();

            subject = new PortfolioMetricsService(logger.Object, featureRepository.Object, appSettingService.Object, serviceProvider.Object, featureStateTransitionRepository.Object);

            featureRepository.Setup(x => x.GetAllByPredicate(
                    It.IsAny<Expression<Func<Feature, bool>>>()))
                .Returns((Expression<Func<Feature, bool>> predicate) => features.Where(predicate.Compile()).AsQueryable());

            SetupTestData();
        }        
        
        [TearDown]
        public void TearDown()
        {
            subject.InvalidatePortfolioMetrics(portfolio);
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
            var result = subject.GetThroughputForPortfolio(portfolio, startDate, endDate);

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
            var result = subject.GetFeaturesInProgressOverTimeForPortfolio(portfolio, startDate, endDate);

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

            var throughput = subject.GetStartedItemsForPortfolio(portfolio, startDate, endDate);

            Assert.That(throughput.Total, Is.EqualTo(2));
        }

        [Test]
        public void GetInProgressFeaturesForProject_ReturnsActiveFeatures()
        {
            // Act
            var result = subject.GetInProgressFeaturesForPortfolio(portfolio, DateTime.UtcNow.Date).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Has.Count.EqualTo(1));
                Assert.That(result[0].ReferenceId, Is.EqualTo("F3"));
            }
        }

        [Test]
        public void GetBlockedEligibleFeaturesForPortfolio_ReturnsToDoAndDoingFeatures_ExcludesDone()
        {
            // A feature can sit in To Do precisely because it is blocked, so To Do + Doing are eligible;
            // Done (F1, F2) must be excluded. F3 is Doing; add a To Do feature to prove To Do is included.
            var toDoFeature = new Feature
            {
                Id = 4,
                Name = "Feature 4",
                ReferenceId = "F4",
                StartedDate = new DateTime(2023, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                StateCategory = StateCategories.ToDo,
            };
            toDoFeature.Portfolios.Add(portfolio);
            features.Add(toDoFeature);

            // A feature shared with ANOTHER portfolio must still be included (membership is Any-of, not
            // All-of) — pins the Portfolios.Any(...) predicate against the Any->All mutation.
            var sharedFeature = new Feature
            {
                Id = 5,
                Name = "Feature 5",
                ReferenceId = "F5",
                StartedDate = new DateTime(2023, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                StateCategory = StateCategories.Doing,
            };
            sharedFeature.Portfolios.Add(portfolio);
            sharedFeature.Portfolios.Add(new Portfolio { Id = 2, Name = "Other Portfolio" });
            features.Add(sharedFeature);

            var result = subject.GetBlockedEligibleFeaturesForPortfolio(portfolio).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Select(f => f.ReferenceId), Is.EquivalentTo(ExpectedBlockedEligibleReferenceIds));
                Assert.That(result.Select(f => f.StateCategory), Has.None.EqualTo(StateCategories.Done));
            }
        }

        [Test]
        public void GetCycleTimePercentilesForProject_ReturnsCorrectPercentileValues()
        {
            var startDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(2023, 1, 31, 0, 0, 0, DateTimeKind.Utc);

            var result = subject.GetCycleTimePercentilesForPortfolio(portfolio, startDate, endDate).ToList();

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

            var result = subject.GetCycleTimePercentilesForPortfolio(portfolio, startDate, endDate).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Has.Count.EqualTo(0));
            }
        }

        [Test]
        public void GetWorkItemAgePercentilesForPortfolio_InProgressFeaturesOfKnownAges_ReturnsNearestRankPercentiles()
        {
            features.Clear();
            foreach (var age in new[] { 1, 2, 2, 3, 3, 4, 5, 6, 7, 9 })
            {
                AddInProgressFeatureAged(age);
            }

            var percentiles = subject.GetWorkItemAgePercentilesForPortfolio(portfolio, DateTime.UtcNow.Date).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(percentiles[0].Percentile, Is.EqualTo(50));
                Assert.That(percentiles[0].Value, Is.EqualTo(3));
                Assert.That(percentiles[1].Percentile, Is.EqualTo(70));
                Assert.That(percentiles[1].Value, Is.EqualTo(5));
                Assert.That(percentiles[2].Percentile, Is.EqualTo(85));
                Assert.That(percentiles[2].Value, Is.EqualTo(6));
                Assert.That(percentiles[3].Percentile, Is.EqualTo(95));
                Assert.That(percentiles[3].Value, Is.EqualTo(7));
            }
        }

        [Test]
        public void GetWorkItemAgePercentilesForPortfolio_NoInProgressFeatures_ReturnsFourZeroValuedEntries()
        {
            features.Clear();
            var done = new Feature
            {
                Id = 1,
                StateCategory = StateCategories.Done,
                StartedDate = DateTime.UtcNow.AddDays(-30),
                ClosedDate = DateTime.UtcNow.AddDays(-2),
            };
            done.Portfolios.Add(portfolio);
            features.Add(done);

            var percentiles = subject.GetWorkItemAgePercentilesForPortfolio(portfolio, DateTime.UtcNow.Date).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(percentiles, Has.Count.EqualTo(4));
                Assert.That(percentiles.Select(p => p.Value), Is.All.Zero);
            }
        }

        [Test]
        public void GetWorkItemAgePercentilesForPortfolio_KeyedOnEndDateOnly_DoesNotCollideWithCycleTimePercentilesCache()
        {
            features.Clear();
            AddInProgressFeatureAged(2);
            var closed = new Feature
            {
                Id = 99,
                StateCategory = StateCategories.Done,
                StartedDate = DateTime.UtcNow.AddDays(-100),
                ClosedDate = DateTime.UtcNow,
            };
            closed.Portfolios.Add(portfolio);
            features.Add(closed);

            var endDate = DateTime.UtcNow.Date;
            var cycleTimePercentiles = subject.GetCycleTimePercentilesForPortfolio(portfolio, endDate.AddDays(-101), endDate).ToList();
            var agePercentiles = subject.GetWorkItemAgePercentilesForPortfolio(portfolio, endDate).ToList();

            Assert.That(agePercentiles[3].Value, Is.Not.EqualTo(cycleTimePercentiles[3].Value),
                "WorkItemAgePercentiles_{endDate} must be a distinct cache key from CycleTimePercentiles_{startDate}_{endDate}; a collision would echo the closed feature's cycle time as the WIP age.");
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

            var result = subject.GetSizePercentilesForPortfolio(portfolio, startDate, endDate).ToList();

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

            var result = subject.GetSizePercentilesForPortfolio(portfolio, startDate, endDate).ToList();

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

            var result = subject.GetCycleTimeDataForPortfolio(portfolio, startDate, endDate).ToList();

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

            var result = subject.GetCycleTimeDataForPortfolio(portfolio, startDate, endDate).ToList();

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

            var result = subject.GetCycleTimeDataForPortfolio(portfolio, startDate, endDate).ToList();

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
            Assert.DoesNotThrow(() => subject.InvalidatePortfolioMetrics(portfolio));
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

            forecastServiceMock.Setup(x => x.HowMany(It.IsAny<RunChartData>(), 30)).Returns(howManyForecast);

            var score = subject.GetMultiItemForecastPredictabilityScoreForPortfolio(portfolio, startDate, endDate);

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
            feature1.Portfolios.Add(portfolio);
            features.Add(feature1);

            var feature2 = new Feature
            {
                Id = 2,
                StateCategory = StateCategories.Done,
            };
            feature2.Portfolios.Add(portfolio);
            features.Add(feature2);

            var totalAge = subject.GetTotalWorkItemAge(portfolio, DateTime.UtcNow.Date);

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

            var totalAge = subject.GetTotalWorkItemAge(portfolio, DateTime.UtcNow.Date);

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
            feature.Portfolios.Add(portfolio);
            features.Add(feature);

            var totalAge = subject.GetTotalWorkItemAge(portfolio, DateTime.UtcNow.Date);

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
            feature1.Portfolios.Add(portfolio);
            features.Add(feature1);

            var feature2 = new Feature
            {
                Id = 2,
                StateCategory = StateCategories.Doing,
                StartedDate = DateTime.UtcNow.AddDays(-5),
            };
            feature2.Portfolios.Add(portfolio);
            features.Add(feature2);

            var feature3 = new Feature
            {
                Id = 3,
                StateCategory = StateCategories.Doing,
                StartedDate = DateTime.UtcNow.AddDays(-2),
            };
            feature3.Portfolios.Add(portfolio);
            features.Add(feature3);

            var totalAge = subject.GetTotalWorkItemAge(portfolio, DateTime.UtcNow.Date);

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
            feature1.Portfolios.Add(portfolio);
            features.Add(feature1);

            var feature2 = new Feature
            {
                Id = 2,
                StateCategory = StateCategories.Done,
                StartedDate = DateTime.UtcNow.AddDays(-15),
                ClosedDate = DateTime.UtcNow.AddDays(-3),
            };
            feature2.Portfolios.Add(portfolio);
            features.Add(feature2);

            var feature3 = new Feature
            {
                Id = 3,
                StateCategory = StateCategories.ToDo,
            };
            feature3.Portfolios.Add(portfolio);
            features.Add(feature3);

            var totalAge = subject.GetTotalWorkItemAge(portfolio, DateTime.UtcNow.Date);

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
            feature.Portfolios.Add(portfolio);
            features.Add(feature);

            var totalAge = subject.GetTotalWorkItemAge(portfolio, DateTime.UtcNow.Date);

            Assert.That(totalAge, Is.EqualTo(10));
        }

        [Test]
        public void GetAllFeaturesForSizeChart_ReturnsOnlyDoneFeaturesInDateRange()
        {
            // Arrange
            var startDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(2023, 1, 31, 0, 0, 0, DateTimeKind.Utc);

            // Act
            var result = subject.GetAllFeaturesForSizeChart(portfolio, startDate, endDate).ToList();

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
            features[features.Count - 1].Portfolios.Add(portfolio);

            var startDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(2023, 1, 31, 0, 0, 0, DateTimeKind.Utc);

            // Act
            var result = subject.GetAllFeaturesForSizeChart(portfolio, startDate, endDate).ToList();

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
            var result = subject.GetAllFeaturesForSizeChart(portfolio, startDate, endDate).ToList();

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
            features[features.Count - 1].Portfolios.Add(portfolio);

            var startDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(2023, 1, 31, 0, 0, 0, DateTimeKind.Utc);

            // Act
            var result = subject.GetAllFeaturesForSizeChart(portfolio, startDate, endDate).ToList();

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
            features[features.Count - 1].Portfolios.Add(portfolio);

            features.Add(new Feature
            {
                Id = 7,
                Name = "Feature 7",
                ReferenceId = "F7",
                StateCategory = StateCategories.Doing,
                StartedDate = DateTime.UtcNow.AddDays(-2)
            });
            features[features.Count - 1].Portfolios.Add(portfolio);

            var startDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(2023, 1, 31, 0, 0, 0, DateTimeKind.Utc);

            // Act
            var result = subject.GetAllFeaturesForSizeChart(portfolio, startDate, endDate).ToList();

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
            features[features.Count - 1].Portfolios.Add(portfolio);

            features.Add(new Feature
            {
                Id = 2,
                Name = "Feature 2",
                ReferenceId = "F2",
                StateCategory = StateCategories.Doing,
                StartedDate = DateTime.UtcNow.AddDays(-2)
            });
            features[features.Count - 1].Portfolios.Add(portfolio);

            var startDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(2023, 1, 31, 0, 0, 0, DateTimeKind.Utc);

            // Act
            var result = subject.GetAllFeaturesForSizeChart(portfolio, startDate, endDate).ToList();

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
            var result = subject.GetAllFeaturesForSizeChart(portfolio, startDate, endDate).ToList();

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
            var result = subject.GetAllFeaturesForSizeChart(portfolio, startDate, endDate).ToList();

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
            portfolio.ProcessBehaviourChartBaselineStartDate = null;
            portfolio.ProcessBehaviourChartBaselineEndDate = null;

            var result = subject.GetThroughputProcessBehaviourChart(portfolio, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

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
            portfolio.ProcessBehaviourChartBaselineStartDate = null;
            portfolio.ProcessBehaviourChartBaselineEndDate = null;

            var displayStart = DateTime.UtcNow.AddDays(-30).Date;
            var displayEnd = DateTime.UtcNow.Date;

            var result = subject.GetThroughputProcessBehaviourChart(portfolio, displayStart, displayEnd);

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
            portfolio.ProcessBehaviourChartBaselineStartDate = DateTime.UtcNow.AddDays(-60).Date;
            portfolio.ProcessBehaviourChartBaselineEndDate = DateTime.UtcNow.AddDays(-16).Date;

            var result = subject.GetThroughputProcessBehaviourChart(portfolio, DateTime.UtcNow.AddDays(-7).Date, DateTime.UtcNow.Date);

            Assert.That(result.BaselineConfigured, Is.True);
        }

        [Test]
        public void GetWipProcessBehaviourChart_BaselineDatesNotSet_ShortRange_ReturnsBaselineInvalid()
        {
            portfolio.ProcessBehaviourChartBaselineStartDate = null;
            portfolio.ProcessBehaviourChartBaselineEndDate = null;

            var result = subject.GetWipProcessBehaviourChart(portfolio, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

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
            portfolio.ProcessBehaviourChartBaselineStartDate = null;
            portfolio.ProcessBehaviourChartBaselineEndDate = null;

            var displayStart = DateTime.UtcNow.AddDays(-30).Date;
            var displayEnd = DateTime.UtcNow.Date;

            var result = subject.GetWipProcessBehaviourChart(portfolio, displayStart, displayEnd);

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
            portfolio.ProcessBehaviourChartBaselineStartDate = null;
            portfolio.ProcessBehaviourChartBaselineEndDate = null;

            var result = subject.GetTotalWorkItemAgeProcessBehaviourChart(portfolio, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

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
            portfolio.ProcessBehaviourChartBaselineStartDate = null;
            portfolio.ProcessBehaviourChartBaselineEndDate = null;

            var displayStart = DateTime.UtcNow.AddDays(-30).Date;
            var displayEnd = DateTime.UtcNow.Date;

            var result = subject.GetTotalWorkItemAgeProcessBehaviourChart(portfolio, displayStart, displayEnd);

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
            portfolio.ProcessBehaviourChartBaselineStartDate = null;
            portfolio.ProcessBehaviourChartBaselineEndDate = null;

            var result = subject.GetCycleTimeProcessBehaviourChart(portfolio, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

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
            portfolio.ProcessBehaviourChartBaselineStartDate = null;
            portfolio.ProcessBehaviourChartBaselineEndDate = null;

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
            feature.Portfolios.Add(portfolio);
            features.Add(feature);

            var result = subject.GetCycleTimeProcessBehaviourChart(portfolio, displayStart, displayEnd);

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
            portfolio.ProcessBehaviourChartBaselineStartDate = null;
            portfolio.ProcessBehaviourChartBaselineEndDate = null;

            var result = subject.GetFeatureSizeProcessBehaviourChart(portfolio, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

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
            portfolio.ProcessBehaviourChartBaselineStartDate = null;
            portfolio.ProcessBehaviourChartBaselineEndDate = null;

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
            feature.Portfolios.Add(portfolio);
            features.Add(feature);

            var result = subject.GetFeatureSizeProcessBehaviourChart(portfolio, displayStart, displayEnd);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(BaselineStatus.InsufficientData));
                Assert.That(result.DataPoints, Is.Empty);
            }
        }

        [Test]
        public void GetFeatureSizeProcessBehaviourChart_BaselineDatesNotSet_LongRange_WithFeaturesWithSize_ReturnsReady()
        {
            portfolio.ProcessBehaviourChartBaselineStartDate = null;
            portfolio.ProcessBehaviourChartBaselineEndDate = null;

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
            feature.Portfolios.Add(portfolio);
            features.Add(feature);

            var result = subject.GetFeatureSizeProcessBehaviourChart(portfolio, displayStart, displayEnd);

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
            portfolio.ProcessBehaviourChartBaselineStartDate = DateTime.UtcNow.AddDays(-60).Date;
            portfolio.ProcessBehaviourChartBaselineEndDate = DateTime.UtcNow.AddDays(-16).Date;

            var team = new Team();
            var baselineFeature = new Feature
            {
                Id = 98,
                Name = "Baseline Feature",
                ReferenceId = "FB",
                StartedDate = portfolio.ProcessBehaviourChartBaselineStartDate,
                ClosedDate = portfolio.ProcessBehaviourChartBaselineStartDate.Value.AddDays(5),
                StateCategory = StateCategories.Done,
            };
            baselineFeature.AddOrUpdateWorkForTeam(team, 3, 8);
            baselineFeature.Portfolios.Add(portfolio);
            features.Add(baselineFeature);

            var result = subject.GetFeatureSizeProcessBehaviourChart(portfolio, DateTime.UtcNow.AddDays(-7).Date, DateTime.UtcNow.Date);

            Assert.That(result.BaselineConfigured, Is.True);
        }

        #region GetFeatureSizeEstimationData

        [Test]
        public void GetFeatureSizeEstimationData_NoEstimationFieldConfigured_ReturnsNotConfigured()
        {
            portfolio.EstimationAdditionalFieldDefinitionId = null;

            var result = subject.GetFeatureSizeEstimationData(portfolio, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(EstimationVsCycleTimeStatus.NotConfigured));
                Assert.That(result.FeatureEstimations, Is.Empty);
            }
        }

        [Test]
        public void GetFeatureSizeEstimationData_EstimationConfiguredButNoFeatures_ReturnsNoData()
        {
            portfolio.EstimationAdditionalFieldDefinitionId = 42;
            features.Clear();

            var result = subject.GetFeatureSizeEstimationData(portfolio, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

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
            portfolio.EstimationAdditionalFieldDefinitionId = fieldId;
            portfolio.UseNonNumericEstimation = false;

            var startDate = DateTime.UtcNow.AddDays(-30).Date;
            var endDate = DateTime.UtcNow.Date;

            // Feature 1 is Done with an estimate
            features[0].AdditionalFieldValues[fieldId] = "5";
            features[0].ClosedDate = startDate.AddDays(3);

            // Feature 3 is Doing with an estimate
            features[2].AdditionalFieldValues[fieldId] = "3";

            var result = subject.GetFeatureSizeEstimationData(portfolio, startDate, endDate);

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
            portfolio.EstimationAdditionalFieldDefinitionId = fieldId;

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
            todoFeature.Portfolios.Add(portfolio);
            todoFeature.AdditionalFieldValues[fieldId] = "2";
            features.Add(todoFeature);

            // Done feature with estimate
            features[0].ClosedDate = startDate.AddDays(3);
            features[0].AdditionalFieldValues[fieldId] = "5";

            // Doing feature with estimate
            features[2].AdditionalFieldValues[fieldId] = "8";

            var result = subject.GetFeatureSizeEstimationData(portfolio, startDate, endDate);

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
            portfolio.EstimationAdditionalFieldDefinitionId = fieldId;
            portfolio.UseNonNumericEstimation = true;
            portfolio.EstimationCategoryValues = ["XS", "S", "M", "L", "XL"];

            var startDate = DateTime.UtcNow.AddDays(-30).Date;
            var endDate = DateTime.UtcNow.Date;

            features[0].ClosedDate = startDate.AddDays(3);
            features[0].AdditionalFieldValues[fieldId] = "M";

            features[2].AdditionalFieldValues[fieldId] = "XL";

            var result = subject.GetFeatureSizeEstimationData(portfolio, startDate, endDate);

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
            portfolio.EstimationAdditionalFieldDefinitionId = fieldId;
            portfolio.UseNonNumericEstimation = false;

            var startDate = DateTime.UtcNow.AddDays(-30).Date;
            var endDate = DateTime.UtcNow.Date;

            // Feature 1: valid estimate
            features[0].ClosedDate = startDate.AddDays(3);
            features[0].AdditionalFieldValues[fieldId] = "5";

            // Feature 2: invalid estimate
            features[1].ClosedDate = startDate.AddDays(7);
            features[1].AdditionalFieldValues[fieldId] = "not-a-number";

            // Feature 3 (Doing): no estimate at all (missing key)

            var result = subject.GetFeatureSizeEstimationData(portfolio, startDate, endDate);

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
            portfolio.EstimationAdditionalFieldDefinitionId = fieldId;
            portfolio.EstimationUnit = "Story Points";

            var startDate = DateTime.UtcNow.AddDays(-30).Date;
            var endDate = DateTime.UtcNow.Date;

            features[0].ClosedDate = startDate.AddDays(3);
            features[0].AdditionalFieldValues[fieldId] = "5";

            var result = subject.GetFeatureSizeEstimationData(portfolio, startDate, endDate);

            Assert.That(result.EstimationUnit, Is.EqualTo("Story Points"));
        }

        [Test]
        public void GetFeatureSizeEstimationData_AllInvalidEstimates_ReturnsNoData()
        {
            const int fieldId = 42;
            portfolio.EstimationAdditionalFieldDefinitionId = fieldId;
            portfolio.UseNonNumericEstimation = false;

            var startDate = DateTime.UtcNow.AddDays(-30).Date;
            var endDate = DateTime.UtcNow.Date;

            features[0].ClosedDate = startDate.AddDays(3);
            features[0].AdditionalFieldValues[fieldId] = "abc";

            features[2].AdditionalFieldValues[fieldId] = "xyz";

            var result = subject.GetFeatureSizeEstimationData(portfolio, startDate, endDate);

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
            portfolio.EstimationAdditionalFieldDefinitionId = fieldId;

            var startDate = DateTime.UtcNow.AddDays(-30).Date;
            var endDate = DateTime.UtcNow.Date;

            features[0].ClosedDate = startDate.AddDays(3);
            features[0].AdditionalFieldValues[fieldId] = "3.5";

            var result = subject.GetFeatureSizeEstimationData(portfolio, startDate, endDate);

            Assert.That(result.FeatureEstimations[0].EstimationNumericValue, Is.EqualTo(3.5));
        }

        [Test]
        public void GetFeatureSizeEstimationData_PerFeatureMapping_EachFeatureHasOwnEstimation()
        {
            const int fieldId = 42;
            portfolio.EstimationAdditionalFieldDefinitionId = fieldId;

            var startDate = DateTime.UtcNow.AddDays(-30).Date;
            var endDate = DateTime.UtcNow.Date;

            // Two features with the same estimate value still get separate entries
            features[0].ClosedDate = startDate.AddDays(3);
            features[0].AdditionalFieldValues[fieldId] = "5";

            features[1].ClosedDate = startDate.AddDays(7);
            features[1].AdditionalFieldValues[fieldId] = "5";

            var result = subject.GetFeatureSizeEstimationData(portfolio, startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.FeatureEstimations, Has.Count.EqualTo(2));
                Assert.That(result.FeatureEstimations[0].FeatureId, Is.Not.EqualTo(result.FeatureEstimations[1].FeatureId));
                Assert.That(result.FeatureEstimations[0].EstimationNumericValue, Is.EqualTo(5.0));
                Assert.That(result.FeatureEstimations[1].EstimationNumericValue, Is.EqualTo(5.0));
            }
        }

        #endregion

        #region GetFeatureSizePercentilesInfoForPortfolio

        [Test]
        public void GetFeatureSizePercentilesInfoForPortfolio_ReturnsPercentilesWithComparison()
        {
            var startDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(2023, 1, 31, 0, 0, 0, DateTimeKind.Utc);

            var team = new Team();
            features[0].AddOrUpdateWorkForTeam(team, 3, 5);
            features[1].AddOrUpdateWorkForTeam(team, 3, 15);

            var result = subject.GetFeatureSizePercentilesInfoForPortfolio(portfolio, startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Percentiles, Has.Length.EqualTo(4));
                Assert.That(result.Percentiles[0].Percentile, Is.EqualTo(50));
                Assert.That(result.Comparison, Is.Not.Null);
                Assert.That(result.Comparison.MetricLabel, Is.EqualTo("Feature Size Percentiles"));
            }
        }

        [Test]
        public void GetFeatureSizePercentilesInfoForPortfolio_NoClosedItems_ReturnsEmptyPercentiles()
        {
            var startDate = new DateTime(2077, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(2077, 1, 31, 0, 0, 0, DateTimeKind.Utc);

            var result = subject.GetFeatureSizePercentilesInfoForPortfolio(portfolio, startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Percentiles, Has.Length.EqualTo(0));
                Assert.That(result.Comparison.Direction, Is.EqualTo("none"));
            }
        }

        [Test]
        public void GetFeatureSizePercentilesInfoForPortfolio_ComparisonContainsDetailRows()
        {
            var startDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(2023, 1, 31, 0, 0, 0, DateTimeKind.Utc);

            var team = new Team();
            features[0].AddOrUpdateWorkForTeam(team, 3, 5);
            features[1].AddOrUpdateWorkForTeam(team, 3, 15);

            var result = subject.GetFeatureSizePercentilesInfoForPortfolio(portfolio, startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Comparison.DetailRows, Is.Not.Null);
            }
        }

        #endregion

        #region CumulativeStateTime

        [Test]
        public void GetCumulativeStateTimeForPortfolio_FeaturesWalkingWorkflowWithOneInFlight_ProducesShapeIdenticalToTeamScope()
        {
            const string analyzing = "Analyzing";
            const string building = "Building";
            const string validating = "Validating";
            const string done = "Done";
            const double daysTolerance = 0.1;

            var windowEnd = new DateTime(2026, 5, 25, 0, 0, 0, DateTimeKind.Utc);
            var windowStart = windowEnd.AddDays(-180);
            var inWindow = windowStart.AddDays(20);

            var cumulativePortfolio = new Portfolio
            {
                Id = 7,
                Name = "Cumulative Portfolio",
                DoingStates = [analyzing, building, validating],
                DoneStates = [done],
            };

            var transitions = new List<FeatureStateTransition>();
            var cumulativeFeatures = new List<Feature>
            {
                CompletedFeature(cumulativePortfolio, transitions, id: 101, referenceId: "EPIC-1", startedDate: inWindow, analyzingDays: 5, buildingDays: 10, validatingDays: 15),
                CompletedFeature(cumulativePortfolio, transitions, id: 102, referenceId: "EPIC-2", startedDate: inWindow.AddDays(2), analyzingDays: 5, buildingDays: 20, validatingDays: 15),
                CompletedFeature(cumulativePortfolio, transitions, id: 103, referenceId: "EPIC-3", startedDate: inWindow.AddDays(4), analyzingDays: 10, buildingDays: 30, validatingDays: 0),
                InFlightFeature(cumulativePortfolio, id: 104, referenceId: "EPIC-WIP", state: building, currentStateEnteredAt: windowEnd.AddDays(-40)),
            };

            featureRepository.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Feature, bool>>>()))
                .Returns((Expression<Func<Feature, bool>> predicate) => cumulativeFeatures.Where(predicate.Compile()).AsQueryable());
            featureStateTransitionRepository.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<FeatureStateTransition, bool>>>()))
                .Returns((Expression<Func<FeatureStateTransition, bool>> predicate) => transitions.Where(predicate.Compile()).AsQueryable());

            var result = subject.GetCumulativeStateTimeForPortfolio(cumulativePortfolio, windowStart, windowEnd, null);

            var buildingRow = result.States.Single(row => row.State == building);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.States.Select(row => row.State), Is.EqualTo(new[] { analyzing, building, validating }));
                Assert.That(buildingRow.CompletedContributionDays, Is.EqualTo(60.0).Within(daysTolerance));
                Assert.That(buildingRow.OngoingContributionDays, Is.EqualTo(40.0).Within(daysTolerance));
                Assert.That(buildingRow.MedianDays, Is.Not.Null);
            }
        }

        private static Feature CompletedFeature(
            Portfolio portfolio,
            List<FeatureStateTransition> transitions,
            int id,
            string referenceId,
            DateTime startedDate,
            int analyzingDays,
            int buildingDays,
            int validatingDays)
        {
            var analyzingExit = startedDate.AddDays(analyzingDays);
            var buildingExit = analyzingExit.AddDays(buildingDays);
            var validatingExit = buildingExit.AddDays(validatingDays);

            var feature = new Feature
            {
                Id = id,
                ReferenceId = referenceId,
                Name = $"Epic {referenceId}",
                State = "Done",
                StateCategory = StateCategories.Done,
                CreatedDate = startedDate.AddDays(-1),
                StartedDate = startedDate,
                ClosedDate = validatingExit,
            };
            feature.Portfolios.Add(portfolio);

            transitions.Add(new FeatureStateTransition { FeatureId = id, FromState = "Analyzing", ToState = "Building", TransitionedAt = analyzingExit });
            transitions.Add(new FeatureStateTransition { FeatureId = id, FromState = "Building", ToState = "Validating", TransitionedAt = buildingExit });
            transitions.Add(validatingDays > 0
                ? new FeatureStateTransition { FeatureId = id, FromState = "Validating", ToState = "Done", TransitionedAt = validatingExit }
                : new FeatureStateTransition { FeatureId = id, FromState = "Building", ToState = "Done", TransitionedAt = buildingExit });

            return feature;
        }

        private static Feature InFlightFeature(Portfolio portfolio, int id, string referenceId, string state, DateTime currentStateEnteredAt)
        {
            var feature = new Feature
            {
                Id = id,
                ReferenceId = referenceId,
                Name = $"Epic {referenceId}",
                State = state,
                StateCategory = StateCategories.Doing,
                CreatedDate = currentStateEnteredAt.AddDays(-1),
                StartedDate = currentStateEnteredAt,
                CurrentStateEnteredAt = currentStateEnteredAt,
            };
            feature.Portfolios.Add(portfolio);
            return feature;
        }

        [Test]
        public void GetFlowEfficiencyInfoForPortfolio_MappingNameMarkedAsWaitState_ReportsActiveOverTotalDoingTime()
        {
            const string analyzing = "Analyzing";
            const string awaitingApproval = "Awaiting Approval";
            const string queuedForBuild = "Queued for Build";
            const string done = "Done";

            var windowEnd = new DateTime(2026, 5, 25, 0, 0, 0, DateTimeKind.Utc);
            var windowStart = windowEnd.AddDays(-180);
            var inWindow = windowStart.AddDays(10);

            var flowPortfolio = new Portfolio
            {
                Id = 9,
                Name = "Flow Efficiency Portfolio",
                DoingStates = [analyzing, awaitingApproval, queuedForBuild],
                DoneStates = [done],
                WaitStates = ["Waiting"],
                StateMappings = [new StateMapping { Name = "Waiting", States = [awaitingApproval, queuedForBuild] }],
            };

            var transitions = new List<FeatureStateTransition>();
            var flowFeatures = new List<Feature>
            {
                SingleDoingVisitFeature(flowPortfolio, transitions, id: 201, referenceId: "EPIC-A", doingState: analyzing, startedDate: inWindow, days: 180),
                SingleDoingVisitFeature(flowPortfolio, transitions, id: 202, referenceId: "EPIC-B", doingState: awaitingApproval, startedDate: inWindow, days: 120),
                SingleDoingVisitFeature(flowPortfolio, transitions, id: 203, referenceId: "EPIC-C", doingState: queuedForBuild, startedDate: inWindow, days: 100),
            };

            featureRepository.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Feature, bool>>>()))
                .Returns((Expression<Func<Feature, bool>> predicate) => flowFeatures.Where(predicate.Compile()).AsQueryable());
            featureStateTransitionRepository.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<FeatureStateTransition, bool>>>()))
                .Returns((Expression<Func<FeatureStateTransition, bool>> predicate) => transitions.Where(predicate.Compile()).AsQueryable());

            var result = subject.GetFlowEfficiencyInfoForPortfolio(flowPortfolio, windowStart, windowEnd);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsConfigured, Is.True);
                Assert.That(result.HasDataInScope, Is.True);
                Assert.That(result.TotalDoingDays, Is.EqualTo(400.0).Within(FlowEfficiencyDaysTolerance));
                Assert.That(result.WaitDays, Is.EqualTo(220.0).Within(FlowEfficiencyDaysTolerance));
                Assert.That(result.EfficiencyPercent, Is.EqualTo(45.0).Within(FlowEfficiencyPercentTolerance));
            }
        }

        private const double FlowEfficiencyDaysTolerance = 0.1;

        private const double FlowEfficiencyPercentTolerance = 0.6;

        private static Feature SingleDoingVisitFeature(
            Portfolio portfolio,
            List<FeatureStateTransition> transitions,
            int id,
            string referenceId,
            string doingState,
            DateTime startedDate,
            int days)
        {
            var exit = startedDate.AddDays(days);

            var feature = new Feature
            {
                Id = id,
                ReferenceId = referenceId,
                Name = $"Epic {referenceId}",
                State = "Done",
                StateCategory = StateCategories.Done,
                CreatedDate = startedDate.AddDays(-1),
                StartedDate = startedDate,
                ClosedDate = exit,
            };
            feature.Portfolios.Add(portfolio);

            transitions.Add(new FeatureStateTransition { FeatureId = id, FromState = "Analyzing", ToState = doingState, TransitionedAt = startedDate });
            transitions.Add(new FeatureStateTransition { FeatureId = id, FromState = doingState, ToState = "Done", TransitionedAt = exit });

            return feature;
        }

        #endregion

        private int inProgressFeatureSequence = 1000;

        private Feature AddInProgressFeatureAged(int ageDays)
        {
            var feature = new Feature
            {
                Id = ++inProgressFeatureSequence,
                StateCategory = StateCategories.Doing,
                StartedDate = DateTime.UtcNow.Date.AddDays(-(ageDays - 1)),
            };
            feature.Portfolios.Add(portfolio);
            features.Add(feature);
            return feature;
        }

        private void SetupTestData()
        {
            portfolio = new Portfolio
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

            features.ForEach(f => f.Portfolios.Add(portfolio));
        }

        // ── GetFeatureStatesAsOf (UPSTREAM-7) ────────────────────────────────

        private void SetupFeatureTransitions(params FeatureStateTransition[] transitions)
        {
            var stored = transitions.ToList();
            featureStateTransitionRepository
                .Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<FeatureStateTransition, bool>>>()))
                .Returns((Expression<Func<FeatureStateTransition, bool>> predicate) =>
                    stored.Where(predicate.Compile()).AsQueryable());
        }

        [Test]
        public void GetFeatureStatesAsOf_FeatureMovedOnSince_ReportsStateItHadOnThatDay()
        {
            var asOfDate = new DateTime(2023, 1, 6, 0, 0, 0, DateTimeKind.Utc);
            SetupFeatureTransitions(
                new FeatureStateTransition { FeatureId = 3, ToState = "Active", TransitionedAt = new DateTime(2023, 1, 2, 0, 0, 0, DateTimeKind.Utc) },
                new FeatureStateTransition { FeatureId = 3, ToState = "Closed", TransitionedAt = new DateTime(2023, 1, 9, 0, 0, 0, DateTimeKind.Utc) });

            var result = subject.GetFeatureStatesAsOf(portfolio, features, asOfDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result[3].State, Is.EqualTo("Active"));
                Assert.That(result[3].StateCategory, Is.EqualTo(StateCategories.Doing));
            }
        }

        [Test]
        public void GetFeatureStatesAsOf_SeveralTransitionsBefore_ReportsTheLastOne()
        {
            var asOfDate = new DateTime(2023, 1, 8, 0, 0, 0, DateTimeKind.Utc);
            var reEnteredAt = new DateTime(2023, 1, 7, 0, 0, 0, DateTimeKind.Utc);
            SetupFeatureTransitions(
                new FeatureStateTransition { FeatureId = 3, ToState = "Active", TransitionedAt = new DateTime(2023, 1, 2, 0, 0, 0, DateTimeKind.Utc) },
                new FeatureStateTransition { FeatureId = 3, ToState = "Resolved", TransitionedAt = new DateTime(2023, 1, 5, 0, 0, 0, DateTimeKind.Utc) },
                new FeatureStateTransition { FeatureId = 3, ToState = "Active", TransitionedAt = reEnteredAt });

            var result = subject.GetFeatureStatesAsOf(portfolio, features, asOfDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result[3].State, Is.EqualTo("Active"));
                Assert.That(result[3].EnteredAt, Is.EqualTo(reEnteredAt));
            }
        }

        [Test]
        public void GetFeatureStatesAsOf_TransitionLaterOnTheDay_StillCounts()
        {
            var asOfDate = new DateTime(2023, 1, 6, 0, 0, 0, DateTimeKind.Utc);
            SetupFeatureTransitions(
                new FeatureStateTransition { FeatureId = 3, ToState = "Active", TransitionedAt = new DateTime(2023, 1, 2, 0, 0, 0, DateTimeKind.Utc) },
                new FeatureStateTransition { FeatureId = 3, ToState = "Resolved", TransitionedAt = asOfDate.AddHours(13) });

            var result = subject.GetFeatureStatesAsOf(portfolio, features, asOfDate);

            Assert.That(result[3].State, Is.EqualTo("Resolved"));
        }

        [Test]
        public void GetFeatureStatesAsOf_NoHistoryForThatDay_FeatureIsAbsentSoCurrentStateStands()
        {
            var asOfDate = new DateTime(2023, 1, 6, 0, 0, 0, DateTimeKind.Utc);
            SetupFeatureTransitions(
                new FeatureStateTransition { FeatureId = 3, ToState = "Resolved", TransitionedAt = new DateTime(2023, 1, 9, 0, 0, 0, DateTimeKind.Utc) });

            var result = subject.GetFeatureStatesAsOf(portfolio, features, asOfDate);

            Assert.That(result, Does.Not.ContainKey(3));
        }

        [Test]
        public void GetFeatureStatesAsOf_NoTransitionsAtAll_ReturnsNothing()
        {
            SetupFeatureTransitions();

            var result = subject.GetFeatureStatesAsOf(portfolio, features, new DateTime(2023, 1, 6, 0, 0, 0, DateTimeKind.Utc));

            Assert.That(result, Is.Empty);
        }
    }
}