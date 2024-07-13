using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    public class ForecastControllerTest
    {
        private Mock<IMonteCarloService> monteCarloServiceMock;
        private Mock<IRepository<Team>> teamRepositoryMock;

        [SetUp]
        public void Setup()
        {
            monteCarloServiceMock = new Mock<IMonteCarloService>();
            teamRepositoryMock = new Mock<IRepository<Team>>();
        }

        [Test]
        public async Task UpdateForecastt_GivenTeamId_UpdatesForecastForTeamAsync()
        {
            var expectedTeam = new Team();
            teamRepositoryMock.Setup(x => x.GetById(12)).Returns(expectedTeam);

            var subject = CreateSubject();

            var result = await subject.UpdateForecastForTeamAsync(12);

            monteCarloServiceMock.Verify(x => x.ForecastFeaturesForTeam(expectedTeam));

            var okResult = result as OkResult;
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf<OkResult>());
                Assert.That(okResult.StatusCode, Is.EqualTo(200));
            });
        }

        [Test]
        public async Task UpdateForecast_TeamDoesNotExist_ReturnsNotFound()
        {
            var subject = CreateSubject();

            var result = await subject.UpdateForecastForTeamAsync(12);

            var notFoundResult = result as NotFoundResult;
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf<NotFoundResult>());
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            });
        }

        [Test]
        public async Task RunManualForecast_HasTargetDate_RunsHowManyForecast()
        {
            var expectedTeam = new Team();
            teamRepositoryMock.Setup(x => x.GetById(12)).Returns(expectedTeam);
            var forecast = new HowManyForecast();

            monteCarloServiceMock.Setup(x => x.HowMany(It.IsAny<Throughput>(), 3)).Returns(forecast);

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

            monteCarloServiceMock.Setup(x => x.When(expectedTeam, 42)).Returns(Task.FromResult(forecast));

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

            monteCarloServiceMock.Setup(x => x.When(expectedTeam, 42)).Returns(Task.FromResult(new WhenForecast()));
            monteCarloServiceMock.Setup(x => x.HowMany(It.IsAny<Throughput>(), 3)).Returns(new HowManyForecast());

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
            return new ForecastController(monteCarloServiceMock.Object, teamRepositoryMock.Object);
        }
    }
}
