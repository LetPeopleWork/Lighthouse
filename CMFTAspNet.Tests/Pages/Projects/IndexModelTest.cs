using CMFTAspNet.Models;
using CMFTAspNet.Pages.Projects;
using CMFTAspNet.Services.Interfaces;
using Moq;

namespace CMFTAspNet.Tests.Pages.Projects
{
    public class IndexModelTest
    {
        private Mock<IRepository<Project>> projectRepositoryMock;

        [SetUp]
        public void Setup()
        {
            projectRepositoryMock = new Mock<IRepository<Project>>();
        }

        [Test]
        public void OnGet_LoadsAllProjectsAsync()
        {
            var expectedProjects = new List<Project>
            {
                new Project { Name = "Project 1" },
                new Project { Name = "Project 2" },
            };

            projectRepositoryMock.Setup(x => x.GetAll()).Returns(expectedProjects);

            var subject = CreateSubject();

            subject.OnGet();

            CollectionAssert.AreEqual(expectedProjects, subject.Projects);
        }

        private IndexModel CreateSubject()
        {
            return new IndexModel(projectRepositoryMock.Object);
        }
    }
}
