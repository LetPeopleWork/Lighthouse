using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    public class ProjectsControllerTest
    {
        private Mock<IRepository<Project>> projectRepoMock;

        private Mock<IWorkItemCollectorService> workItemCollectorServiceMock;

        private Mock<IMonteCarloService> monteCarloServiceMock;

        [SetUp]
        public void Setup()
        {
            projectRepoMock = new Mock<IRepository<Project>>();
            workItemCollectorServiceMock = new Mock<IWorkItemCollectorService>();
            monteCarloServiceMock = new Mock<IMonteCarloService>();
        }

        [Test]
        public void GetProjects_ReturnsAllProjectsFromRepository()
        {
            var testProjects = GetTestProjects();
            projectRepoMock.Setup(x => x.GetAll()).Returns(testProjects);

            var subject = CreateSubject();

            var result = subject.GetProjects();

            Assert.That(result.Count, Is.EqualTo(testProjects.Count()));
        }

        [Test]
        public void GetProject_ReturnsSpecificProject()
        {
            var testProject = GetTestProjects().Last();
            projectRepoMock.Setup(x => x.GetById(42)).Returns(testProject);

            var subject = CreateSubject();

            var result = subject.Get(42);

            Assert.Multiple(() =>
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                var projectDto = okResult.Value as ProjectDto;

                Assert.That(projectDto.Id, Is.EqualTo(testProject.Id));
                Assert.That(projectDto.Name, Is.EqualTo(testProject.Name));
            });
        }

        [Test]
        public void GetProject_ProjectNotFound_ReturnsNotFound()
        {
            var subject = CreateSubject();

            var result = subject.Get(1337);

            Assert.Multiple(() =>
            {
                Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
                var notFoundResult = result.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            });
        }

        [Test]
        public async Task UpdateFeaturesForProject_ProjectExists_UpdatesAndRefreshesForecasts()
        {
            var testProject = GetTestProjects().Last();
            projectRepoMock.Setup(x => x.GetById(42)).Returns(testProject);

            var subject = CreateSubject();

            var result = await subject.UpdateFeaturesForProject(42);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf<OkObjectResult>());

                var okResult = result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                workItemCollectorServiceMock.Verify(x => x.UpdateFeaturesForProject(testProject));
                monteCarloServiceMock.Verify(x => x.UpdateForecastsForProject(testProject));
            });
        }

        [Test]
        public async Task UpdateFeaturesForProject_ProjectNotFound_ReturnsNotFoundAsync()
        {
            var subject = CreateSubject();

            var result = await subject.UpdateFeaturesForProject(1337);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf<NotFoundResult>());
                var notFoundResult = result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            });
        }

        [Test]
        public void Delete_RemovesTeamAndSaves()
        {
            var projectId = 12;

            var subject = CreateSubject();

            subject.DeleteProject(projectId);

            projectRepoMock.Verify(x => x.Remove(projectId));
            projectRepoMock.Verify(x => x.Save());
        }

        private ProjectsController CreateSubject()
        {
            return new ProjectsController(projectRepoMock.Object, workItemCollectorServiceMock.Object, monteCarloServiceMock.Object);
        }

        private IEnumerable<Project> GetTestProjects()
        {
            return new List<Project>
            {
                new Project { Id = 12, Name = "Foo" },
                new Project { Id = 42, Name = "Bar" }
            };
        }
    }
}
