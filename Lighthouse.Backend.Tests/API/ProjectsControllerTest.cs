using Lighthouse.Backend.API;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    public class ProjectsControllerTest
    {
        private Mock<IRepository<Project>> projectRepoMock;

        [SetUp]
        public void Setup()
        {
            projectRepoMock = new Mock<IRepository<Project>>();
        }

        [Test]
        public void GetProjects_ReturnsAllProjectsFromRepository()
        {
            var testProjects = GetTestProjects();
            projectRepoMock.Setup(x => x.GetAll()).Returns(testProjects);

            var subject = CreateSubject();

            var result = subject.GetOverview();

            Assert.That(result.Count, Is.EqualTo(testProjects.Count()));
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
            return new ProjectsController(projectRepoMock.Object);
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
