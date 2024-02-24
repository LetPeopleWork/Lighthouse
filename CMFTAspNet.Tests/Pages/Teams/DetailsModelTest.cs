using CMFTAspNet.Models;
using CMFTAspNet.Pages.Teams;
using CMFTAspNet.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

namespace CMFTAspNet.Tests.Pages.Teams
{
    public class DetailsModelTest
    {
        private Mock<IRepository<Team>> repositoryMock;
        private Mock<IThroughputService> throughputServiceMock;

        [SetUp]
        public void Setup()
        {
            repositoryMock = new Mock<IRepository<Team>>();
            throughputServiceMock = new Mock<IThroughputService>();
        }

        [Test]
        public async Task OnPost_IdIsNull_ReturnsNotFoundAsync()
        {
            var subject = CreateSubject();

            var result = await subject.OnPost(null);

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
            repositoryMock.Verify(x => x.GetById(It.IsAny<int>()), Times.Never());
        }

        [Test]
        public async Task OnPost_TeamDoesNotExist_ReturnsNotFoundAsync()
        {
            repositoryMock.Setup(x => x.GetById(12)).Returns((Team)null);
            var subject = CreateSubject();

            var result = await subject.OnPost(12);

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task OnPost_TeamExists_UpdatesThroughputAndSavesAsync()
        {
            var team = new Team { Id = 2, Name = "Team", ProjectName = "Project" };

            repositoryMock.Setup(x => x.GetById(12)).Returns(team);
            var subject = CreateSubject();

            var result = await subject.OnPost(12);

            Assert.That(result, Is.InstanceOf<PageResult>());
            throughputServiceMock.Verify(x => x.UpdateThroughput(team), Times.Once());
            repositoryMock.Verify(x => x.Update(team), Times.Once());
            repositoryMock.Verify(x => x.Save(), Times.Once());
        }

        private DetailsModel CreateSubject()
        {
            return new DetailsModel(repositoryMock.Object, throughputServiceMock.Object);
        }
    }
}
