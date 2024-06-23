using Lighthouse.API;
using Lighthouse.Models;
using Lighthouse.Services.Interfaces;
using Moq;

namespace Lighthouse.Tests.API
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
        public async Task GetProjects_ReturnsAllProjectsFromRepository()
        {
            var testProjects = GetTestProjects();
            projectRepoMock.Setup(x => x.GetAll()).Returns(testProjects);

            var subject = CreateSubject();

            var result = await subject.Get();

            Assert.That(result.Count, Is.EqualTo(testProjects.Count()));
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
