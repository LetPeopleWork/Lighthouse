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

        [Test]
        public void GetThroughputForTeam_TeamExists_ReturnsRawThroughput()
        {
            var expectedThroughput = new int[] { 1, 1, 0, 2, 0, 1, 0, 0, 1, 2, 3, 0, 0, 0, 0 };
            var team = new Team();

            team.UpdateThroughput(expectedThroughput);
            teamRepositoryMock.Setup(x => x.GetById(12)).Returns(team);

            var subject = CreateSubject();

            var result = subject.GetThroughputForTeam(12);

            var okResult = result as OkObjectResult;
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf<OkObjectResult>());
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                var value = okResult.Value;
                Assert.That(value, Is.EqualTo(expectedThroughput));
            });
        }

        [Test]
        public void GetThroughputForTeam_TeamDoesNotExist_ReturnsNotFound()
        {
            var subject = CreateSubject();

            var result = subject.GetThroughputForTeam(12);

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
