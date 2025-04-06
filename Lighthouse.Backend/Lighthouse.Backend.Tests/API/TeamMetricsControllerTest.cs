using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.DTO.Metrics;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
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

            Assert.Multiple(() =>
            {
                Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundResult = response.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            });
        }

        [Test]
        public void GetThroughput_StartDateAfterEndDate_ReturnsBadRequest()
        {
            var subject = CreateSubject();

            var response = subject.GetThroughput(1337, DateTime.Now, DateTime.Now.AddDays(-1));

            Assert.Multiple(() =>
            {
                Assert.That(response.Result, Is.InstanceOf<BadRequestObjectResult>());

                var badRequestResult = response.Result as BadRequestObjectResult;
                Assert.That(badRequestResult.StatusCode, Is.EqualTo(400));
            });
        }

        [Test]
        public void GetThroughput_TeamExists_GetsThroughputFromTeamMetricsService()
        {
            var team = new Team { Id = 1 };
            teamRepositoryMock.Setup(repo => repo.GetById(1)).Returns(team);

            var expectedThroughput = new Throughput([1, 88, 6]);
            teamMetricsServiceMock.Setup(service => service.GetThroughputForTeam(team, It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(expectedThroughput);

            var subject = CreateSubject();

            var response = subject.GetThroughput(team.Id, DateTime.Now.AddDays(-1), DateTime.Now);

            Assert.Multiple(() =>
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var result = response.Result as OkObjectResult;
                Assert.That(result.StatusCode, Is.EqualTo(200));
                Assert.That(result.Value, Is.EqualTo(expectedThroughput));
            });
        }

        [Test]
        public void GetFeaturesInProgress_TeamIdDoesNotExist_ReturnsNotFound()
        {
            var subject = CreateSubject();

            var response = subject.GetFeaturesInProgress(1337);

            Assert.Multiple(() =>
            {
                Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundResult = response.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            });
        }

        [Test]
        public void GetFeaturesInProgress_TeamExists_GetsFeaturesFromTeamMetricsService()
        {
            var team = new Team { Id = 1 };
            teamRepositoryMock.Setup(repo => repo.GetById(1)).Returns(team);

            var expectedFeatures = new List<WorkItemDto> { WorkItemDto.CreateUnknownWorkItemDto("Vfl"), WorkItemDto.CreateUnknownWorkItemDto("GCZ") };
            teamMetricsServiceMock.Setup(service => service.GetCurrentFeaturesInProgressForTeam(team)).Returns(expectedFeatures);

            var subject = CreateSubject();

            var response = subject.GetFeaturesInProgress(team.Id);

            Assert.Multiple(() =>
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var result = response.Result as OkObjectResult;
                Assert.That(result.StatusCode, Is.EqualTo(200));
                Assert.That(result.Value, Is.EqualTo(expectedFeatures));
            });
        }

        [Test]
        public void GetWipForTeam_TeamIdDoesNotExist_ReturnsNotFound()
        {
            var subject = CreateSubject();

            var response = subject.GetCurrentWipForTeam(1337);

            Assert.Multiple(() =>
            {
                Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundResult = response.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            });
        }

        [Test]
        public void GetWipForTeam_TeamExists_GetsItemsFromTeamMetricsService()
        {
            var team = new Team { Id = 1 };
            teamRepositoryMock.Setup(repo => repo.GetById(1)).Returns(team);

            var expectedItems = new List<WorkItemDto> { WorkItemDto.CreateUnknownWorkItemDto("Vfl"), WorkItemDto.CreateUnknownWorkItemDto("GCZ") };
            teamMetricsServiceMock.Setup(service => service.GetCurrentWipForTeam(team)).Returns(expectedItems);

            var subject = CreateSubject();

            var response = subject.GetCurrentWipForTeam(team.Id);

            Assert.Multiple(() =>
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var result = response.Result as OkObjectResult;
                Assert.That(result.StatusCode, Is.EqualTo(200));
                Assert.That(result.Value, Is.EqualTo(expectedItems));
            });
        }

        private TeamMetricsController CreateSubject()
        {
            return new TeamMetricsController(teamRepositoryMock.Object, teamMetricsServiceMock.Object);
        }
    }
}
