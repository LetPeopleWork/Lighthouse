using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    [TestFixture]
    public class ProjectMetricsControllerTests
    {
        private Mock<IRepository<Project>> projectRepository;
        private Mock<IProjectMetricsService> projectMetricsService;
        private ProjectMetricsController subject;
        private Project project;

        [SetUp]
        public void Setup()
        {
            projectRepository = new Mock<IRepository<Project>>();
            projectMetricsService = new Mock<IProjectMetricsService>();
            subject = new ProjectMetricsController(projectRepository.Object, projectMetricsService.Object);
            
            project = new Project
            {
                Id = 1,
                Name = "Test Project"
            };
            
            projectRepository.Setup(x => x.GetById(1)).Returns(project);
            projectRepository.Setup(x => x.GetById(999)).Returns((Project)null);
        }

        [Test]
        public void GetThroughput_WithValidInput_ReturnsOk()
        {
            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 1, 10);
            var expectedResult = new RunChartData([1, 0, 0, 1, 0, 0, 0, 0, 0, 0]);
            
            projectMetricsService.Setup(x => x.GetThroughputForProject(project, startDate, endDate))
                .Returns(expectedResult);

            var result = subject.GetThroughput(1, startDate, endDate);

            Assert.Multiple(() =>
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult?.Value, Is.EqualTo(expectedResult));
            });
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

            Assert.Multiple(() =>
            {
                Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundResult = response.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            });
        }

        [Test]
        public void GetStartedItems_StartDateAfterEndDate_ReturnsBadRequest()
        {
            var response = subject.GetStartedItems(1337, DateTime.Now, DateTime.Now.AddDays(-1));

            Assert.Multiple(() =>
            {
                Assert.That(response.Result, Is.InstanceOf<BadRequestObjectResult>());

                var badRequestResult = response.Result as BadRequestObjectResult;
                Assert.That(badRequestResult.StatusCode, Is.EqualTo(400));
            });
        }

        [Test]
        public void GetStartedItems_TeamExists_GetsStartedItemsFromTeamMetricsService()
        {
            var project = new Project { Id = 1 };
            projectRepository.Setup(repo => repo.GetById(1)).Returns(project);

            var expectedStartedItems = new RunChartData([1, 88, 6]);
            projectMetricsService.Setup(service => service.GetStartedItemsForProject(project, It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(expectedStartedItems);

            var response = subject.GetStartedItems(project.Id, DateTime.Now.AddDays(-1), DateTime.Now);

            Assert.Multiple(() =>
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var result = response.Result as OkObjectResult;
                Assert.That(result.StatusCode, Is.EqualTo(200));
                Assert.That(result.Value, Is.EqualTo(expectedStartedItems));
            });
        }

        [Test]
        public void GetFeaturesInProgressOverTime_WithValidInput_ReturnsOk()
        {
            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 1, 5);
            var expectedResult = new RunChartData([1, 2, 2, 1, 1]);
            
            projectMetricsService.Setup(x => x.GetFeaturesInProgressOverTimeForProject(project, startDate, endDate))
                .Returns(expectedResult);

            var result = subject.GetFeaturesInProgressOverTime(1, startDate, endDate);

            Assert.Multiple(() =>
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult?.Value, Is.EqualTo(expectedResult));
            });
        }

        [Test]
        public void GetInProgressFeatures_WithValidInput_ReturnsOk()
        {
            var features = new List<Feature>
            {
                new Feature { Id = 1, Name = "Feature 1", ReferenceId = "F1" }
            };
            
            projectMetricsService.Setup(x => x.GetInProgressFeaturesForProject(project))
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
            
            projectMetricsService.Setup(x => x.GetCycleTimePercentilesForProject(project, startDate, endDate))
                .Returns(percentiles);

            var result = subject.GetCycleTimePercentiles(1, startDate, endDate);

            Assert.Multiple(() =>
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = result.Result as OkObjectResult;
                var returnedPercentiles = okResult?.Value as IEnumerable<PercentileValue>;
                
                Assert.That(returnedPercentiles?.Count(), Is.EqualTo(4));
            });
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

            projectMetricsService.Setup(x => x.GetCycleTimeDataForProject(project, startDate, endDate))
                .Returns(features);

            var result = subject.GetCycleTimeData(1, startDate, endDate);

            Assert.Multiple(() =>
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = result.Result as OkObjectResult;
                var featureDtos = okResult?.Value as IEnumerable<FeatureDto>;
                Assert.That(featureDtos?.Count(), Is.EqualTo(2));
            });
        }
    }
}