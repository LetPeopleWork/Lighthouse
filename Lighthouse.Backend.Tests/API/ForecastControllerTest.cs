using Lighthouse.Backend.API;
using Lighthouse.Backend.Models;
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

        private ForecastController CreateSubject()
        {
            return new ForecastController(monteCarloServiceMock.Object, teamRepositoryMock.Object);
        }
    }
}
