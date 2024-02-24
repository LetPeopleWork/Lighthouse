using CMFTAspNet.Models;
using CMFTAspNet.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Moq;
using CMFTAspNet.Pages.Projects;
using CMFTAspNet.Services.Implementation;

namespace CMFTAspNet.Tests.Pages.Projects
{
    public class DetailsModelTest
    {
        private Mock<IRepository<Project>> repositoryMock;
        private Mock<IWorkItemCollectorService> workItemCollectorServiceMock;

        [SetUp]
        public void Setup()
        {
            repositoryMock = new Mock<IRepository<Project>>();
            workItemCollectorServiceMock = new Mock<IWorkItemCollectorService>();
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
        public async Task OnPost_ProjectDoesNotExist_ReturnsNotFoundAsync()
        {
            repositoryMock.Setup(x => x.GetById(12)).Returns((Project)null);
            var subject = CreateSubject();

            var result = await subject.OnPost(12);

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task OnPost_ProjectExists_UpdatesFeatureForecastsAndSavesAsync()
        {
            var project = new Project { Id = 2, Name = "Team", SearchTerm = "Project" };

            repositoryMock.Setup(x => x.GetById(12)).Returns(project);
            var subject = CreateSubject();

            var result = await subject.OnPost(12);

            IEnumerable<Project> projects = [project];

            Assert.That(result, Is.InstanceOf<PageResult>());
            workItemCollectorServiceMock.Verify(x => x.CollectFeaturesForProject(projects), Times.Once());
            repositoryMock.Verify(x => x.Update(project), Times.Once());
            repositoryMock.Verify(x => x.Save(), Times.Once());
        }

        private DetailsModel CreateSubject()
        {
            return new DetailsModel(repositoryMock.Object, workItemCollectorServiceMock.Object);
        }
    }
}
