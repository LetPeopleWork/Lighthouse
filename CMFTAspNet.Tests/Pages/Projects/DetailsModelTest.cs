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
        private Mock<IRepository<Project>> projectRepositoryMock;
        private Mock<IWorkItemCollectorService> workItemCollectorServiceMock;
        private Mock<IMonteCarloService> monteCarloServiceMock;

        [SetUp]
        public void Setup()
        {
            projectRepositoryMock = new Mock<IRepository<Project>>();
            workItemCollectorServiceMock = new Mock<IWorkItemCollectorService>();
            monteCarloServiceMock = new Mock<IMonteCarloService>();
        }

        [Test]
        public async Task OnPost_IdIsNull_ReturnsNotFoundAsync()
        {
            var subject = CreateSubject();

            var result = await subject.OnPost(null);

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
            projectRepositoryMock.Verify(x => x.GetById(It.IsAny<int>()), Times.Never());
        }

        [Test]
        public async Task OnPost_ProjectDoesNotExist_ReturnsNotFoundAsync()
        {
            projectRepositoryMock.Setup(x => x.GetById(12)).Returns((Project)null);
            var subject = CreateSubject();

            var result = await subject.OnPost(12);

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task OnPost_ProjectExists_UpdatesFeaturesForProject()
        {
            var project = SetupProject();
            var subject = CreateSubject();

            var result = await subject.OnPost(12);

            Assert.That(result, Is.InstanceOf<PageResult>());
            workItemCollectorServiceMock.Verify(x => x.UpdateFeaturesForProject(project), Times.Once());
            monteCarloServiceMock.Verify(x => x.ForecastFeatures(project.Features), Times.Once());
        }

        private DetailsModel CreateSubject()
        {
            return new DetailsModel(projectRepositoryMock.Object, workItemCollectorServiceMock.Object, monteCarloServiceMock.Object);
        }

        private Project SetupProject()
        {
            var project = new Project { Id = 2, Name = "Team", SearchTerm = "Project" };
            projectRepositoryMock.Setup(x => x.GetById(12)).Returns(project);

            return project;
        }
    }
}
