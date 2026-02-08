using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    [TestFixture]
    public class PortfolioMetricsControllerTests
    {
        private Mock<IRepository<Portfolio>> projectRepository;
        private Mock<IPortfolioMetricsService> projectMetricsService;
        private PortfolioMetricsController subject;
        private Portfolio project;

        [SetUp]
        public void Setup()
        {
            projectRepository = new Mock<IRepository<Portfolio>>();
            projectMetricsService = new Mock<IPortfolioMetricsService>();
            subject = new PortfolioMetricsController(projectRepository.Object, projectMetricsService.Object);
            
            project = new Portfolio
            {
                Id = 1,
                Name = "Test Project"
            };
            
            projectRepository.Setup(x => x.GetById(1)).Returns(project);
            projectRepository.Setup(x => x.GetById(999)).Returns((Portfolio)null);
        }

        [Test]
        public void GetThroughput_WithValidInput_ReturnsOk()
        {
            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 1, 10);
            var expectedResult = new RunChartData(RunChartDataGenerator.GenerateRunChartData([1, 0, 0, 1, 0, 0, 0, 0, 0, 0]));
            
            projectMetricsService.Setup(x => x.GetThroughputForPortfolio(project, startDate, endDate))
                .Returns(expectedResult);

            var result = subject.GetThroughput(1, startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult?.Value, Is.EqualTo(expectedResult));
            };
        }

        [Test]
        public void GetThroughput_WithInvalidDateRange_ReturnsBadRequest()
        {
            var startDate = new DateTime(2023, 1, 10);
            var endDate = new DateTime(2023, 1, 1);  // End date before start date

            var result = subject.GetThroughput(1, startDate, endDate);

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public void GetThroughput_WithInvalidProjectId_ReturnsNotFound()
        {
            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 1, 10);

            var result = subject.GetThroughput(999, startDate, endDate);

            Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public void GetStartedItems_TeamIdDoesNotExist_ReturnsNotFound()
        {
            var response = subject.GetStartedItems(1337, DateTime.Now, DateTime.Now);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundResult = response.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            };
        }

        [Test]
        public void GetStartedItems_StartDateAfterEndDate_ReturnsBadRequest()
        {
            var response = subject.GetStartedItems(1337, DateTime.Now, DateTime.Now.AddDays(-1));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<BadRequestObjectResult>());

                var badRequestResult = response.Result as BadRequestObjectResult;
                Assert.That(badRequestResult.StatusCode, Is.EqualTo(400));
            };
        }

        [Test]
        public void GetStartedItems_TeamExists_GetsStartedItemsFromTeamMetricsService()
        {
            var project = new Portfolio { Id = 1 };
            projectRepository.Setup(repo => repo.GetById(1)).Returns(project);

            var expectedStartedItems = new RunChartData(RunChartDataGenerator.GenerateRunChartData([1, 88, 6]));
            projectMetricsService.Setup(service => service.GetStartedItemsForPortfolio(project, It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(expectedStartedItems);

            var response = subject.GetStartedItems(project.Id, DateTime.Now.AddDays(-1), DateTime.Now);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var result = response.Result as OkObjectResult;
                Assert.That(result.StatusCode, Is.EqualTo(200));
                Assert.That(result.Value, Is.EqualTo(expectedStartedItems));
            };
        }

        [Test]
        public void GetFeaturesInProgressOverTime_WithValidInput_ReturnsOk()
        {
            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 1, 5);
            var expectedResult = new RunChartData(RunChartDataGenerator.GenerateRunChartData([1, 2, 2, 1, 1]));
            
            projectMetricsService.Setup(x => x.GetFeaturesInProgressOverTimeForPortfolio(project, startDate, endDate))
                .Returns(expectedResult);

            var result = subject.GetFeaturesInProgressOverTime(1, startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult?.Value, Is.EqualTo(expectedResult));
            };
        }

        [Test]
        public void GetInProgressFeatures_WithValidInput_ReturnsOk()
        {
            var features = new List<Feature>
            {
                new Feature { Id = 1, Name = "Feature 1", ReferenceId = "F1" }
            };
            
            projectMetricsService.Setup(x => x.GetInProgressFeaturesForPortfolio(project))
                .Returns(features);

            var result = subject.GetInProgressFeatures(1);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
                
                var okResult = result.Result as OkObjectResult;
                var featureDtos = okResult?.Value as IEnumerable<FeatureDto>;

                Assert.That(featureDtos?.Count(), Is.EqualTo(1));
                Assert.That(featureDtos?.First().Id, Is.EqualTo(1));
            }
        }

        [Test]
        public void GetCycleTimePercentiles_WithValidInput_ReturnsOk()
        {
            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 1, 31);
            var percentiles = new List<PercentileValue>
            {
                new PercentileValue(50, 3),
                new PercentileValue(70, 4),
                new PercentileValue(85, 5),
                new PercentileValue(95, 6)
            };
            
            projectMetricsService.Setup(x => x.GetCycleTimePercentilesForPortfolio(project, startDate, endDate))
                .Returns(percentiles);

            var result = subject.GetCycleTimePercentiles(1, startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = result.Result as OkObjectResult;
                var returnedPercentiles = okResult?.Value as IEnumerable<PercentileValue>;
                
                Assert.That(returnedPercentiles?.Count(), Is.EqualTo(4));
            };
        }

        [Test]
        public void GetCycleTimeData_WithValidInput_ReturnsOk()
        {
            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 1, 31);
            var features = new List<Feature>
            {
                new Feature { Id = 1, Name = "Feature 1", ReferenceId = "F1", StartedDate = DateTime.Now.AddDays(-2), ClosedDate = DateTime.Now },
                new Feature { Id = 2, Name = "Feature 2", ReferenceId = "F2", StartedDate = DateTime.Now.AddDays(-5), ClosedDate = DateTime.Now }
            };

            projectMetricsService.Setup(x => x.GetCycleTimeDataForPortfolio(project, startDate, endDate))
                .Returns(features);

            var result = subject.GetCycleTimeData(1, startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = result.Result as OkObjectResult;
                var featureDtos = okResult?.Value as IEnumerable<FeatureDto>;
                Assert.That(featureDtos?.Count(), Is.EqualTo(2));
            };
        }

        [Test]
        public void GetSizePercentiles_WithValidInput_ReturnsOk()
        {
            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 1, 31);

            var percentiles = new List<PercentileValue>
            {
                new PercentileValue(50, 3),
                new PercentileValue(70, 4),
                new PercentileValue(85, 5),
                new PercentileValue(95, 6)
            };

            projectMetricsService.Setup(x => x.GetSizePercentilesForPortfolio(project, startDate, endDate))
                .Returns(percentiles);

            var result = subject.GetSizePercentiles(1, startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = result.Result as OkObjectResult;
                var returnedPercentiles = okResult?.Value as IEnumerable<PercentileValue>;

                Assert.That(returnedPercentiles?.Count(), Is.EqualTo(4));
            }
            ;
        }

        [Test]
        public void GetMultiItemForecastPredictabilityScore_ProjectIdDoesNotExist_ReturnsNotFound()
        {
            var response = subject.GetMultiItemForecastPredictabilityScore(1337, DateTime.Now, DateTime.Now);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());
                var notFoundResult = response.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            }
            ;
        }

        [Test]
        public void GetMultiItemForecastPredictabilityScore_StartDateAfterEndDate_ReturnsBadRequest()
        {
            var response = subject.GetMultiItemForecastPredictabilityScore(1337, DateTime.Now, DateTime.Now.AddDays(-1));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<BadRequestObjectResult>());
                var badRequestResult = response.Result as BadRequestObjectResult;
                Assert.That(badRequestResult.StatusCode, Is.EqualTo(400));
            }
            ;
        }

        [Test]
        public void GetMultiItemForecastPredictabilityScore_ProjectExists_GetsPredictabilityScoreFromTeamMetricsService()
        {
            var project = new Portfolio { Id = 1 };
            projectRepository.Setup(repo => repo.GetById(1)).Returns(project);

            var howManyForecast = new HowManyForecast();
            var expectedScore = new ForecastPredictabilityScore(howManyForecast);
            projectMetricsService.Setup(service => service.GetMultiItemForecastPredictabilityScoreForPortfolio(project, It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(expectedScore);

            var response = subject.GetMultiItemForecastPredictabilityScore(project.Id, DateTime.Now.AddDays(-1), DateTime.Now);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var result = response.Result as OkObjectResult;
                Assert.That(result.StatusCode, Is.EqualTo(200));
                Assert.That(result.Value, Is.EqualTo(expectedScore));
            }
        }

        [Test]
        public void GetTotalWorkItemAge_ProjectIdDoesNotExist_ReturnsNotFound()
        {
            var response = subject.GetTotalWorkItemAge(1337);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundResult = response.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            }
        }

        [Test]
        public void GetTotalWorkItemAge_ProjectExists_GetsTotalWorkItemAgeFromProjectMetricsService()
        {
            const int expectedTotalAge = 56;
            projectMetricsService.Setup(service => service.GetTotalWorkItemAge(project)).Returns(expectedTotalAge);

            var response = subject.GetTotalWorkItemAge(project.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var result = response.Result as OkObjectResult;
                Assert.That(result.StatusCode, Is.EqualTo(200));
                Assert.That(result.Value, Is.EqualTo(expectedTotalAge));
            }
        }

        [Test]
        public void GetAllFeaturesForSizeChart_WithValidInput_ReturnsOk()
        {
            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 1, 31);
            var features = new List<Feature>
            {
                new Feature { Id = 1, Name = "Feature 1", ReferenceId = "F1", StateCategory = StateCategories.Done, StartedDate = DateTime.Now.AddDays(-5), ClosedDate = new DateTime(2023, 1, 10) },
                new Feature { Id = 2, Name = "Feature 2", ReferenceId = "F2", StateCategory = StateCategories.Doing, StartedDate = DateTime.Now.AddDays(-3) },
                new Feature { Id = 3, Name = "Feature 3", ReferenceId = "F3", StateCategory = StateCategories.ToDo }
            };

            projectMetricsService.Setup(x => x.GetAllFeaturesForSizeChart(project, startDate, endDate))
                .Returns(features);

            var result = subject.GetAllFeaturesForSizeChart(1, startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = result.Result as OkObjectResult;
                var featureDtos = okResult?.Value as IEnumerable<FeatureDto>;
                Assert.That(featureDtos?.Count(), Is.EqualTo(3));
            };
        }

        [Test]
        public void GetAllFeaturesForSizeChart_WithInvalidDateRange_ReturnsBadRequest()
        {
            var startDate = new DateTime(2023, 1, 31);
            var endDate = new DateTime(2023, 1, 1);  // End date before start date

            var result = subject.GetAllFeaturesForSizeChart(1, startDate, endDate);

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public void GetAllFeaturesForSizeChart_WithInvalidProjectId_ReturnsNotFound()
        {
            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 1, 31);

            var result = subject.GetAllFeaturesForSizeChart(999, startDate, endDate);

            Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public void GetAllFeaturesForSizeChart_EmptyResult_ReturnsOkWithEmptyList()
        {
            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 1, 31);
            var emptyFeatures = new List<Feature>();

            projectMetricsService.Setup(x => x.GetAllFeaturesForSizeChart(project, startDate, endDate))
                .Returns(emptyFeatures);

            var result = subject.GetAllFeaturesForSizeChart(1, startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = result.Result as OkObjectResult;
                var featureDtos = okResult?.Value as IEnumerable<FeatureDto>;
                Assert.That(featureDtos, Is.Empty);
            };
        }

        [Test]
        public void GetThroughputPbc_PortfolioIdDoesNotExist_ReturnsNotFound()
        {
            var response = subject.GetThroughputProcessBehaviourChart(999, DateTime.Now, DateTime.Now);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundResult = response.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            }
        }

        [Test]
        public void GetThroughputPbc_StartDateAfterEndDate_ReturnsBadRequest()
        {
            var response = subject.GetThroughputProcessBehaviourChart(1, DateTime.Now, DateTime.Now.AddDays(-1));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<BadRequestObjectResult>());

                var badRequestResult = response.Result as BadRequestObjectResult;
                Assert.That(badRequestResult.StatusCode, Is.EqualTo(400));
            }
        }

        [Test]
        public void GetThroughputPbc_PortfolioExists_ReturnsPbcFromService()
        {
            var expectedPbc = new ProcessBehaviourChart
            {
                Status = BaselineStatus.Ready,
                XAxisKind = XAxisKind.Date,
                Average = 2.5,
                UpperNaturalProcessLimit = 5.0,
                LowerNaturalProcessLimit = 0.0,
                DataPoints = [new ProcessBehaviourChartDataPoint("2025-01-01", 3, SpecialCauseType.None, [1, 2])],
            };
            projectMetricsService.Setup(service => service.GetThroughputProcessBehaviourChart(project, It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(expectedPbc);

            var response = subject.GetThroughputProcessBehaviourChart(project.Id, DateTime.Now.AddDays(-1), DateTime.Now);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var result = response.Result as OkObjectResult;
                Assert.That(result.StatusCode, Is.EqualTo(200));
                Assert.That(result.Value, Is.EqualTo(expectedPbc));
            }
        }
    }
}