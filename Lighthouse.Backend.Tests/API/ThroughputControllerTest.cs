using Lighthouse.Backend.API;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    public class ThroughputControllerTest
    {
        private Mock<IThroughputService> throughputServiceMock;
        private Mock<IRepository<Team>> teamRepositoryMock;

        [SetUp]
        public void SetUp()
        {
            throughputServiceMock = new Mock<IThroughputService>();
            teamRepositoryMock = new Mock<IRepository<Team>>();
        }

        [Test]
        public async Task UpdateThroughput_GivenTeamId_UpdatesThroughputForTeamAsync()
        {
            var expectedTeam = new Team();
            teamRepositoryMock.Setup(x => x.GetById(12)).Returns(expectedTeam);

            var subject = CreateSubject();

            var result = await subject.UpdateThroughput(12);

            throughputServiceMock.Verify(x => x.UpdateThroughputForTeam(expectedTeam));
            teamRepositoryMock.Verify(x => x.Save());

            var okResult = result as OkResult;
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf<OkResult>());
                Assert.That(okResult.StatusCode, Is.EqualTo(200));
            });
        }

        [Test]
        public async Task UpdateThroughput_TeamDoesNotExist_ReturnsNotFound()
        {
            var subject = CreateSubject();

            var result = await subject.UpdateThroughput(12);

            var notFoundResult = result as NotFoundResult;
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf<NotFoundResult>());
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            });
        }

        private ThroughputController CreateSubject()
        {
            return new ThroughputController(throughputServiceMock.Object, teamRepositoryMock.Object);
        }
    }
}
