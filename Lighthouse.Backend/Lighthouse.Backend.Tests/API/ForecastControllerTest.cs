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
        private Mock<IForecastUpdater> forecastUpdaterMock;
        private Mock<IForecastService> forecastServiceMock;
        private Mock<IRepository<Team>> teamRepositoryMock;
        private Mock<ITeamMetricsService> teamMetricsServiceMock;

        [SetUp]
        public void Setup()
        {
            forecastUpdaterMock = new Mock<IForecastUpdater>();
            forecastServiceMock = new Mock<IForecastService>();

            teamRepositoryMock = new Mock<IRepository<Team>>();
            teamMetricsServiceMock = new Mock<ITeamMetricsService>();
        }

        [Test]
        public void UpdateForecast_ProjectExists_TriggersForecastUpdate()
        {
            var subject = CreateSubject();

            var response = subject.UpdateForecastForProject(12);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response, Is.InstanceOf<OkResult>());

                var okResult = response as OkResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                forecastUpdaterMock.Verify(x => x.TriggerUpdate(12), Times.Once);
            }
        }

        [Test]
        public void UpdateForecastsForTeamPortfolios_TeamExists_TriggersUpdateForAllPortfolios()
        {
            var portfolio1 = new Portfolio { Id = 1, Name = "Portfolio 1" };
            var portfolio2 = new Portfolio { Id = 2, Name = "Portfolio 2" };
            var portfolio3 = new Portfolio { Id = 3, Name = "Portfolio 3" };

            var team = new Team { Id = 12, Name = "Test Team" };
            team.Portfolios.Add(portfolio1);
            team.Portfolios.Add(portfolio2);
            team.Portfolios.Add(portfolio3);

            teamRepositoryMock.Setup(x => x.GetById(12)).Returns(team);

            var subject = CreateSubject();

            var response = subject.UpdateForecastsForTeamPortfolios(12);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = response.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                forecastUpdaterMock.Verify(x => x.TriggerUpdate(1), Times.Once);
                forecastUpdaterMock.Verify(x => x.TriggerUpdate(2), Times.Once);
                forecastUpdaterMock.Verify(x => x.TriggerUpdate(3), Times.Once);
            }
        }

        [Test]
        public void UpdateForecastsForTeamPortfolios_TeamHasNoPortfolios_DoesNotTriggerUpdates()
        {
            var team = new Team { Id = 12, Name = "Test Team" };

            teamRepositoryMock.Setup(x => x.GetById(12)).Returns(team);

            var subject = CreateSubject();

            var response = subject.UpdateForecastsForTeamPortfolios(12);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = response.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                forecastUpdaterMock.Verify(x => x.TriggerUpdate(It.IsAny<int>()), Times.Never);
            }
        }

        [Test]
        public void UpdateForecastsForTeamPortfolios_TeamDoesNotExist_ReturnsNotFound()
        {
            teamRepositoryMock.Setup(x => x.GetById(12)).Returns((Team)null);

            var subject = CreateSubject();

            var response = subject.UpdateForecastsForTeamPortfolios(12);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundResult = response.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));

                forecastUpdaterMock.Verify(x => x.TriggerUpdate(It.IsAny<int>()), Times.Never);
            }
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

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                var manualForecast = okResult.Value as ManualForecastDto;

                Assert.That(manualForecast.HowManyForecasts, Has.Count.EqualTo(4));
                Assert.That(manualForecast.WhenForecasts, Has.Count.EqualTo(0));
                Assert.That(manualForecast.Likelihood, Is.Zero);
            }
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

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                var manualForecast = okResult.Value as ManualForecastDto;

                Assert.That(manualForecast.HowManyForecasts, Has.Count.EqualTo(0));
                Assert.That(manualForecast.WhenForecasts, Has.Count.EqualTo(4));
                Assert.That(manualForecast.Likelihood, Is.Zero);
            }
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

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                var manualForecast = okResult.Value as ManualForecastDto;

                Assert.That(manualForecast.HowManyForecasts, Has.Count.EqualTo(4));
                Assert.That(manualForecast.WhenForecasts, Has.Count.EqualTo(4));
                Assert.That(manualForecast.Likelihood, Is.Not.Zero);
            }
        }

        [Test]
        public async Task RunManualForecast_TeamDoesNotExist_ReturnsNotFound()
        {
            var subject = CreateSubject();

            var result = await subject.RunManualForecastAsync(12, new ForecastController.ManualForecastInputDto());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundResult = result.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            }
        }

        [Test]
        public void RunItemCreationPrediction_TeamExists_ReturnsPredictionForecast()
        {
            var subject = CreateSubject();

            var expectedTeam = new Team();
            teamRepositoryMock.Setup(x => x.GetById(12)).Returns(expectedTeam);

            var itemCreationPredictionInput = new ForecastController.ItemCreationPredictionInputDto
            {
                WorkItemTypes = ["Bug", "Task"],
                StartDate = DateTime.Now.AddDays(-1),
                EndDate = DateTime.Now.AddDays(30)
            };

            var expectedForecast = new HowManyForecast();

            forecastServiceMock.Setup(x => x.PredictWorkItemCreation(expectedTeam, itemCreationPredictionInput.WorkItemTypes, itemCreationPredictionInput.StartDate, itemCreationPredictionInput.EndDate, 30))
                .Returns(expectedForecast);

            var result = subject.RunItemCreationPrediction(12, itemCreationPredictionInput);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                var prediction = okResult.Value as ManualForecastDto;

                Assert.That(prediction.HowManyForecasts, Has.Count.EqualTo(4));
                Assert.That(prediction.WhenForecasts, Has.Count.EqualTo(0));
                Assert.That(prediction.Likelihood, Is.Zero);
            }
        }

        [Test]
        public void RunItemCreationPrediction_TeamDoesNotExist_ReturnsNotFound()
        {
            var subject = CreateSubject();

            var result = subject.RunItemCreationPrediction(12, new ForecastController.ItemCreationPredictionInputDto());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundResult = result.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            }
        }

        private ForecastController CreateSubject()
        {
            return new ForecastController(forecastUpdaterMock.Object, forecastServiceMock.Object, teamRepositoryMock.Object, teamMetricsServiceMock.Object);
        }
    }
}
