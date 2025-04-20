using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    public class ForecastControllerTest
    {
        private Mock<IForecastUpdateService> forecastUpdateServiceMock;
        private Mock<IForecastService> forecastServiceMock;
        private Mock<IRepository<Team>> teamRepositoryMock;
        private Mock<ITeamMetricsService> teamMetricsServiceMock;

        [SetUp]
        public void Setup()
        {
            forecastUpdateServiceMock = new Mock<IForecastUpdateService>();
            forecastServiceMock = new Mock<IForecastService>();

            teamRepositoryMock = new Mock<IRepository<Team>>();
            teamMetricsServiceMock = new Mock<ITeamMetricsService>();
        }

        [Test]
        public void UpdateForecast_ProjectExists_TriggersForecastUpdate()
        {
            var subject = CreateSubject();

            var response = subject.UpdateForecastForProject(12);

            Assert.Multiple(() =>
            {
                Assert.That(response, Is.InstanceOf<OkResult>());

                var okResult = response as OkResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                forecastUpdateServiceMock.Verify(x => x.TriggerUpdate(12), Times.Once);
            });
        }

        [Test]
        public async Task RunManualForecast_HasTargetDate_RunsHowManyForecast()
        {
            var expectedTeam = new Team();
            teamRepositoryMock.Setup(x => x.GetById(12)).Returns(expectedTeam);
            var forecast = new HowManyForecast();

            forecastServiceMock.Setup(x => x.HowMany(It.IsAny<RunChartData>(), 3)).Returns(forecast);

            var subject = CreateSubject();

            var manualForecastInput = new ForecastController.ManualForecastInputDto { TargetDate = DateTime.Now.AddDays(3) };
            var result = await subject.RunManualForecastAsync(12, manualForecastInput);

            Assert.Multiple(() =>
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                var manualForecast = okResult.Value as ManualForecastDto;

                Assert.That(manualForecast.HowManyForecasts, Has.Count.EqualTo(4));
                Assert.That(manualForecast.WhenForecasts, Has.Count.EqualTo(0));
                Assert.That(manualForecast.Likelihood, Is.EqualTo(0));
            });
        }

        [Test]
        public async Task RunManualForecast_HasRemainingItems_RunsWhenForecast()
        {
            var expectedTeam = new Team();
            teamRepositoryMock.Setup(x => x.GetById(12)).Returns(expectedTeam);
            var forecast = new WhenForecast();

            forecastServiceMock.Setup(x => x.When(expectedTeam, 42)).Returns(Task.FromResult(forecast));

            var subject = CreateSubject();

            var manualForecastInput = new ForecastController.ManualForecastInputDto { RemainingItems = 42 };
            var result = await subject.RunManualForecastAsync(12, manualForecastInput);

            Assert.Multiple(() =>
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                var manualForecast = okResult.Value as ManualForecastDto;

                Assert.That(manualForecast.HowManyForecasts, Has.Count.EqualTo(0));
                Assert.That(manualForecast.WhenForecasts, Has.Count.EqualTo(4));
                Assert.That(manualForecast.Likelihood, Is.EqualTo(0));
            });
        }

        [Test]
        public async Task RunManualForecast_HasRemainingItemsAndTargetDate_IncludesLikelihood()
        {
            var expectedTeam = new Team();
            teamRepositoryMock.Setup(x => x.GetById(12)).Returns(expectedTeam);

            forecastServiceMock.Setup(x => x.When(expectedTeam, 42)).Returns(Task.FromResult(new WhenForecast()));
            forecastServiceMock.Setup(x => x.HowMany(It.IsAny<RunChartData>(), 3)).Returns(new HowManyForecast());

            var subject = CreateSubject();

            var manualForecastInput = new ForecastController.ManualForecastInputDto { RemainingItems = 42, TargetDate = DateTime.Now.AddDays(3) };
            var result = await subject.RunManualForecastAsync(12, manualForecastInput);

            Assert.Multiple(() =>
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                var manualForecast = okResult.Value as ManualForecastDto;

                Assert.That(manualForecast.HowManyForecasts, Has.Count.EqualTo(4));
                Assert.That(manualForecast.WhenForecasts, Has.Count.EqualTo(4));
                Assert.That(manualForecast.Likelihood, Is.Not.EqualTo(0));
            });
        }

        [Test]
        public async Task RunManualForecast_TeamDoesNotExist_ReturnsNotFound()
        {
            var subject = CreateSubject();

            var result = await subject.RunManualForecastAsync(12, new ForecastController.ManualForecastInputDto());

            Assert.Multiple(() =>
            {
                Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundResult = result.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            });
        }

        private ForecastController CreateSubject()
        {
            return new ForecastController(forecastUpdateServiceMock.Object, forecastServiceMock.Object, teamRepositoryMock.Object, teamMetricsServiceMock.Object);
        }
    }
}
