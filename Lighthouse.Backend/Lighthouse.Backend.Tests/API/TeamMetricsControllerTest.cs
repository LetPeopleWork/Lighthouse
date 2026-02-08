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
    public class TeamMetricsControllerTest
    {
        private Mock<IRepository<Team>> teamRepositoryMock;
        private Mock<ITeamMetricsService> teamMetricsServiceMock;

        [SetUp]
        public void Setup()
        {
            teamRepositoryMock = new Mock<IRepository<Team>>();
            teamMetricsServiceMock = new Mock<ITeamMetricsService>();
        }

        [Test]
        public void GetThroughput_TeamIdDoesNotExist_ReturnsNotFound()
        {
            var subject = CreateSubject();

            var response = subject.GetThroughput(1337, DateTime.Now, DateTime.Now);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundResult = response.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            };
        }

        [Test]
        public void GetThroughput_StartDateAfterEndDate_ReturnsBadRequest()
        {
            var subject = CreateSubject();

            var response = subject.GetThroughput(1337, DateTime.Now, DateTime.Now.AddDays(-1));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<BadRequestObjectResult>());

                var badRequestResult = response.Result as BadRequestObjectResult;
                Assert.That(badRequestResult.StatusCode, Is.EqualTo(400));
            };
        }

        [Test]
        public void GetThroughput_TeamExists_GetsThroughputFromTeamMetricsService()
        {
            var team = new Team { Id = 1 };
            teamRepositoryMock.Setup(repo => repo.GetById(1)).Returns(team);

            var expectedThroughput = new RunChartData(RunChartDataGenerator.GenerateRunChartData([1, 88, 6]));
            teamMetricsServiceMock.Setup(service => service.GetThroughputForTeam(team, It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(expectedThroughput);

            var subject = CreateSubject();

            var response = subject.GetThroughput(team.Id, DateTime.Now.AddDays(-1), DateTime.Now);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var result = response.Result as OkObjectResult;
                Assert.That(result.StatusCode, Is.EqualTo(200));
                Assert.That(result.Value, Is.EqualTo(expectedThroughput));
            };
        }

        [Test]
        public void GetStartedItems_TeamIdDoesNotExist_ReturnsNotFound()
        {
            var subject = CreateSubject();

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
            var subject = CreateSubject();

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
            var team = new Team { Id = 1 };
            teamRepositoryMock.Setup(repo => repo.GetById(1)).Returns(team);

            var expectedStartedItems = new RunChartData(RunChartDataGenerator.GenerateRunChartData([1, 88, 6]));
            teamMetricsServiceMock.Setup(service => service.GetStartedItemsForTeam(team, It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(expectedStartedItems);

            var subject = CreateSubject();

            var response = subject.GetStartedItems(team.Id, DateTime.Now.AddDays(-1), DateTime.Now);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var result = response.Result as OkObjectResult;
                Assert.That(result.StatusCode, Is.EqualTo(200));
                Assert.That(result.Value, Is.EqualTo(expectedStartedItems));
            };
        }

        [Test]
        public void GetFeaturesInProgress_TeamIdDoesNotExist_ReturnsNotFound()
        {
            var subject = CreateSubject();

            var response = subject.GetFeaturesInProgress(1337);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundResult = response.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            };
        }

        [Test]
        public void GetFeaturesInProgress_TeamExists_GetsFeaturesFromTeamMetricsService()
        {
            var team = new Team { Id = 1 };
            teamRepositoryMock.Setup(repo => repo.GetById(1)).Returns(team);

            var feature1 = new Feature
            {
                Name = "Vfl"
            };

            var feature2 = new Feature
            {
                Name = "GCZ"
            };


            var expectedFeatures = new List<Feature> { feature1, feature2 };
            teamMetricsServiceMock.Setup(service => service.GetCurrentFeaturesInProgressForTeam(team)).Returns(expectedFeatures);

            var subject = CreateSubject();

            var response = subject.GetFeaturesInProgress(team.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var result = response.Result as OkObjectResult;
                Assert.That(result.StatusCode, Is.EqualTo(200));

                var actualItems = (IEnumerable<FeatureDto>)result.Value;
                Assert.That(actualItems?.Count(), Is.EqualTo(2));
                Assert.That(actualItems?.First().Name, Is.EqualTo("Vfl"));
                Assert.That(actualItems?.Last().Name, Is.EqualTo("GCZ"));
            };
        }

        [Test]
        public void GetWipForTeam_TeamIdDoesNotExist_ReturnsNotFound()
        {
            var subject = CreateSubject();

            var response = subject.GetCurrentWipForTeam(1337);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundResult = response.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            };
        }

        [Test]
        public void GetWipForTeam_TeamExists_GetsItemsFromTeamMetricsService()
        {
            var team = new Team { Id = 1 };
            teamRepositoryMock.Setup(repo => repo.GetById(1)).Returns(team);

            var item1 = new WorkItem
            {
                Name = "Vfl",
                Team = team,
            };

            var item2 = new WorkItem
            {
                Name = "GCZ",
                Team = team,
            };

            var expectedItems = new List<WorkItem> { item1, item2 };
            teamMetricsServiceMock.Setup(service => service.GetCurrentWipForTeam(team)).Returns(expectedItems);

            var subject = CreateSubject();

            var response = subject.GetCurrentWipForTeam(team.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var result = response.Result as OkObjectResult;
                Assert.That(result.StatusCode, Is.EqualTo(200));

                var actualItems = (IEnumerable<WorkItemDto>)result.Value;
                Assert.That(actualItems?.Count(), Is.EqualTo(2));
                Assert.That(actualItems?.First().Name, Is.EqualTo("Vfl"));
                Assert.That(actualItems?.Last().Name, Is.EqualTo("GCZ"));
            };
        }

        [Test]
        public void GetCycleTimePercentilesForTeam_TeamIdDoesNotExist_ReturnsNotFound()
        {
            var subject = CreateSubject();

            var response = subject.GetCycleTimePercentilesForTeam(1337, DateTime.Now, DateTime.Now);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundResult = response.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            };
        }

        [Test]
        public void GetCycleTimePercentilesForTeam_StartDateAfterEndDate_ReturnsBadRequest()
        {
            var subject = CreateSubject();

            var response = subject.GetCycleTimePercentilesForTeam(1337, DateTime.Now, DateTime.Now.AddDays(-1));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<BadRequestObjectResult>());

                var badRequestResult = response.Result as BadRequestObjectResult;
                Assert.That(badRequestResult.StatusCode, Is.EqualTo(400));
            };
        }

        [Test]
        public void GetCycleTimePercentilesForTeam_TeamExists_GetsPercentilesFromTeamMetricsService()
        {
            var team = new Team { Id = 1 };
            teamRepositoryMock.Setup(repo => repo.GetById(1)).Returns(team);

            var expectedPercentiles = new List<PercentileValue>
            {
                new PercentileValue(50, 5),
                new PercentileValue (90, 10)
            };

            teamMetricsServiceMock.Setup(service => service.GetCycleTimePercentilesForTeam(team, It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(expectedPercentiles);

            var subject = CreateSubject();

            var response = subject.GetCycleTimePercentilesForTeam(team.Id, DateTime.Now.AddDays(-1), DateTime.Now);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var result = response.Result as OkObjectResult;
                Assert.That(result.StatusCode, Is.EqualTo(200));
                Assert.That(result.Value, Is.EqualTo(expectedPercentiles));
            };
        }

        [Test]
        public void GetCycleTimeDataForTeam_TeamIdDoesNotExist_ReturnsNotFound()
        {
            var subject = CreateSubject();

            var response = subject.GetCycleTimeDataForTeam(1337, DateTime.Now, DateTime.Now);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundResult = response.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            };
        }

        [Test]
        public void GetCycleTimeDataForTeam_StartDateAfterEndDate_ReturnsBadRequest()
        {
            var subject = CreateSubject();

            var response = subject.GetCycleTimeDataForTeam(1337, DateTime.Now, DateTime.Now.AddDays(-1));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<BadRequestObjectResult>());

                var badRequestResult = response.Result as BadRequestObjectResult;
                Assert.That(badRequestResult.StatusCode, Is.EqualTo(400));
            };
        }

        [Test]
        public void GetCycleTimeDataForTeam_TeamExists_GetsDataFromTeamMetricsService()
        {
            var team = new Team { Id = 1 };
            teamRepositoryMock.Setup(repo => repo.GetById(1)).Returns(team);

            var item1 = new WorkItem
            {
                Name = "Vfl",
                Team = team,
            };

            var item2 = new WorkItem
            {
                Name = "GCZ",
                Team = team,
            };

            var expectedItems = new List<WorkItem> { item1, item2 };
            teamMetricsServiceMock.Setup(service => service.GetClosedItemsForTeam(team, It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(expectedItems);

            var subject = CreateSubject();

            var response = subject.GetCycleTimeDataForTeam(team.Id, DateTime.Now.AddDays(-1), DateTime.Now);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var result = response.Result as OkObjectResult;
                Assert.That(result.StatusCode, Is.EqualTo(200));

                var actualItems = (IEnumerable<WorkItemDto>)result.Value;
                Assert.That(actualItems?.Count(), Is.EqualTo(2));
                Assert.That(actualItems?.First().Name, Is.EqualTo("Vfl"));
                Assert.That(actualItems?.Last().Name, Is.EqualTo("GCZ"));
            };
        }

        [Test]
        public void GetWorkInProgressOverTime_TeamIdDoesNotExist_ReturnsNotFound()
        {
            var subject = CreateSubject();

            var response = subject.GetWorkInProgressOverTime(1337, DateTime.Now, DateTime.Now);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundResult = response.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            };
        }

        [Test]
        public void GetWorkInProgressOverTime_StartDateAfterEndDate_ReturnsBadRequest()
        {
            var subject = CreateSubject();

            var response = subject.GetWorkInProgressOverTime(1337, DateTime.Now, DateTime.Now.AddDays(-1));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<BadRequestObjectResult>());

                var badRequestResult = response.Result as BadRequestObjectResult;
                Assert.That(badRequestResult.StatusCode, Is.EqualTo(400));
            };
        }

        [Test]
        public void GetWorkInProgressOverTime_TeamExists_GetsWorkInProgressOverTimeFromTeamMetricsService()
        {
            var team = new Team { Id = 1 };
            teamRepositoryMock.Setup(repo => repo.GetById(1)).Returns(team);

            var expectedData = new RunChartData(RunChartDataGenerator.GenerateRunChartData([1, 2, 3]));
            teamMetricsServiceMock.Setup(service => service.GetWorkInProgressOverTimeForTeam(team, It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(expectedData);

            var subject = CreateSubject();

            var response = subject.GetWorkInProgressOverTime(team.Id, DateTime.Now.AddDays(-1), DateTime.Now);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var result = response.Result as OkObjectResult;
                Assert.That(result.StatusCode, Is.EqualTo(200));
                Assert.That(result.Value, Is.EqualTo(expectedData));
            };
        }

        [Test]
        public void GetMultiItemForecastPredictabilityScore_TeamIdDoesNotExist_ReturnsNotFound()
        {
            var subject = CreateSubject();
            
            var response = subject.GetMultiItemForecastPredictabilityScore(1337, DateTime.Now, DateTime.Now);
            
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());
                var notFoundResult = response.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            };
        }

        [Test]
        public void GetMultiItemForecastPredictabilityScore_StartDateAfterEndDate_ReturnsBadRequest()
        {
            var subject = CreateSubject();
            
            var response = subject.GetMultiItemForecastPredictabilityScore(1337, DateTime.Now, DateTime.Now.AddDays(-1));
            
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<BadRequestObjectResult>());
                var badRequestResult = response.Result as BadRequestObjectResult;
                Assert.That(badRequestResult.StatusCode, Is.EqualTo(400));
            };
        }

        [Test]
        public void GetMultiItemForecastPredictabilityScore_TeamExists_GetsPredictabilityScoreFromTeamMetricsService()
        {
            var team = new Team { Id = 1 };
            teamRepositoryMock.Setup(repo => repo.GetById(1)).Returns(team);

            var howManyForecast = new HowManyForecast();
            var expectedScore = new ForecastPredictabilityScore(howManyForecast);
            teamMetricsServiceMock.Setup(service => service.GetMultiItemForecastPredictabilityScoreForTeam(team, It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(expectedScore);
            
            var subject = CreateSubject();
            var response = subject.GetMultiItemForecastPredictabilityScore(team.Id, DateTime.Now.AddDays(-1), DateTime.Now);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var result = response.Result as OkObjectResult;
                Assert.That(result.StatusCode, Is.EqualTo(200));
                Assert.That(result.Value, Is.EqualTo(expectedScore));
            };
        }

        [Test]
        public void GetTotalWorkItemAge_TeamIdDoesNotExist_ReturnsNotFound()
        {
            var subject = CreateSubject();

            var response = subject.GetTotalWorkItemAge(1337);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundResult = response.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            }
        }

        [Test]
        public void GetTotalWorkItemAge_TeamExists_GetsTotalWorkItemAgeFromTeamMetricsService()
        {
            var team = new Team { Id = 1 };
            teamRepositoryMock.Setup(repo => repo.GetById(1)).Returns(team);

            const int expectedTotalAge = 42;
            teamMetricsServiceMock.Setup(service => service.GetTotalWorkItemAge(team)).Returns(expectedTotalAge);

            var subject = CreateSubject();

            var response = subject.GetTotalWorkItemAge(team.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var result = response.Result as OkObjectResult;
                Assert.That(result.StatusCode, Is.EqualTo(200));
                Assert.That(result.Value, Is.EqualTo(expectedTotalAge));
            }
        }

        [Test]
        public void GetThroughputPbc_TeamIdDoesNotExist_ReturnsNotFound()
        {
            var subject = CreateSubject();

            var response = subject.GetThroughputProcessBehaviourChart(1337, DateTime.Now, DateTime.Now);

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
            var subject = CreateSubject();

            var response = subject.GetThroughputProcessBehaviourChart(1337, DateTime.Now, DateTime.Now.AddDays(-1));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<BadRequestObjectResult>());

                var badRequestResult = response.Result as BadRequestObjectResult;
                Assert.That(badRequestResult.StatusCode, Is.EqualTo(400));
            }
        }

        [Test]
        public void GetThroughputPbc_TeamExists_ReturnsPbcFromService()
        {
            var team = new Team { Id = 1 };
            teamRepositoryMock.Setup(repo => repo.GetById(1)).Returns(team);

            var expectedPbc = new ProcessBehaviourChart
            {
                Status = BaselineStatus.Ready,
                XAxisKind = XAxisKind.Date,
                Average = 5.0,
                UpperNaturalProcessLimit = 10.0,
                LowerNaturalProcessLimit = 0.0,
                DataPoints = [new ProcessBehaviourChartDataPoint("2025-01-01", 3, SpecialCauseType.None, [1, 2])],
            };
            teamMetricsServiceMock.Setup(service => service.GetThroughputProcessBehaviourChart(team, It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(expectedPbc);

            var subject = CreateSubject();

            var response = subject.GetThroughputProcessBehaviourChart(team.Id, DateTime.Now.AddDays(-1), DateTime.Now);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var result = response.Result as OkObjectResult;
                Assert.That(result.StatusCode, Is.EqualTo(200));
                Assert.That(result.Value, Is.EqualTo(expectedPbc));
            }
        }

        [Test]
        public void GetWipPbc_TeamIdDoesNotExist_ReturnsNotFound()
        {
            var subject = CreateSubject();

            var response = subject.GetWipProcessBehaviourChart(1337, DateTime.Now, DateTime.Now);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundResult = response.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            }
        }

        [Test]
        public void GetWipPbc_StartDateAfterEndDate_ReturnsBadRequest()
        {
            var subject = CreateSubject();

            var response = subject.GetWipProcessBehaviourChart(1337, DateTime.Now, DateTime.Now.AddDays(-1));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<BadRequestObjectResult>());

                var badRequestResult = response.Result as BadRequestObjectResult;
                Assert.That(badRequestResult.StatusCode, Is.EqualTo(400));
            }
        }

        [Test]
        public void GetWipPbc_TeamExists_ReturnsPbcFromService()
        {
            var team = new Team { Id = 1 };
            teamRepositoryMock.Setup(repo => repo.GetById(1)).Returns(team);

            var expectedPbc = new ProcessBehaviourChart { Status = BaselineStatus.Ready };
            teamMetricsServiceMock.Setup(service => service.GetWipProcessBehaviourChart(team, It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(expectedPbc);

            var subject = CreateSubject();

            var response = subject.GetWipProcessBehaviourChart(team.Id, DateTime.Now.AddDays(-1), DateTime.Now);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var result = response.Result as OkObjectResult;
                Assert.That(result.StatusCode, Is.EqualTo(200));
                Assert.That(result.Value, Is.EqualTo(expectedPbc));
            }
        }

        [Test]
        public void GetTotalWorkItemAgePbc_TeamIdDoesNotExist_ReturnsNotFound()
        {
            var subject = CreateSubject();

            var response = subject.GetTotalWorkItemAgeProcessBehaviourChart(1337, DateTime.Now, DateTime.Now);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundResult = response.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            }
        }

        [Test]
        public void GetTotalWorkItemAgePbc_StartDateAfterEndDate_ReturnsBadRequest()
        {
            var subject = CreateSubject();

            var response = subject.GetTotalWorkItemAgeProcessBehaviourChart(1337, DateTime.Now, DateTime.Now.AddDays(-1));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<BadRequestObjectResult>());

                var badRequestResult = response.Result as BadRequestObjectResult;
                Assert.That(badRequestResult.StatusCode, Is.EqualTo(400));
            }
        }

        [Test]
        public void GetTotalWorkItemAgePbc_TeamExists_ReturnsPbcFromService()
        {
            var team = new Team { Id = 1 };
            teamRepositoryMock.Setup(repo => repo.GetById(1)).Returns(team);

            var expectedPbc = new ProcessBehaviourChart { Status = BaselineStatus.Ready };
            teamMetricsServiceMock.Setup(service => service.GetTotalWorkItemAgeProcessBehaviourChart(team, It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(expectedPbc);

            var subject = CreateSubject();

            var response = subject.GetTotalWorkItemAgeProcessBehaviourChart(team.Id, DateTime.Now.AddDays(-1), DateTime.Now);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var result = response.Result as OkObjectResult;
                Assert.That(result.StatusCode, Is.EqualTo(200));
                Assert.That(result.Value, Is.EqualTo(expectedPbc));
            }
        }

        [Test]
        public void GetCycleTimePbc_TeamIdDoesNotExist_ReturnsNotFound()
        {
            var subject = CreateSubject();

            var response = subject.GetCycleTimeProcessBehaviourChart(1337, DateTime.Now, DateTime.Now);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundResult = response.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            }
        }

        [Test]
        public void GetCycleTimePbc_StartDateAfterEndDate_ReturnsBadRequest()
        {
            var subject = CreateSubject();

            var response = subject.GetCycleTimeProcessBehaviourChart(1337, DateTime.Now, DateTime.Now.AddDays(-1));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<BadRequestObjectResult>());

                var badRequestResult = response.Result as BadRequestObjectResult;
                Assert.That(badRequestResult.StatusCode, Is.EqualTo(400));
            }
        }

        [Test]
        public void GetCycleTimePbc_TeamExists_ReturnsPbcFromService()
        {
            var team = new Team { Id = 1 };
            teamRepositoryMock.Setup(repo => repo.GetById(1)).Returns(team);

            var expectedPbc = new ProcessBehaviourChart { Status = BaselineStatus.Ready, XAxisKind = XAxisKind.DateTime };
            teamMetricsServiceMock.Setup(service => service.GetCycleTimeProcessBehaviourChart(team, It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(expectedPbc);

            var subject = CreateSubject();

            var response = subject.GetCycleTimeProcessBehaviourChart(team.Id, DateTime.Now.AddDays(-1), DateTime.Now);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var result = response.Result as OkObjectResult;
                Assert.That(result.StatusCode, Is.EqualTo(200));
                Assert.That(result.Value, Is.EqualTo(expectedPbc));
            }
        }

        private TeamMetricsController CreateSubject()
        {
            return new TeamMetricsController(teamRepositoryMock.Object, teamMetricsServiceMock.Object);
        }
    }
}
