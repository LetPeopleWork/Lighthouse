using CMFTAspNet.Models;
using CMFTAspNet.Pages;
using CMFTAspNet.Services.Implementation;
using CMFTAspNet.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

namespace CMFTAspNet.Tests.Pages
{
    public class IndexModelTest
    {
        private Mock<IRepository<Project>> projectRepositoryMock;
        private Mock<IMonteCarloService> monteCarloServiceMock;

        [SetUp]
        public void Setup()
        {
            projectRepositoryMock = new Mock<IRepository<Project>>();
            monteCarloServiceMock = new Mock<IMonteCarloService>();
        }

        [Test]
        public void OnGet_LoadsAllProjects()
        {
            var projects = new List<Project>
            {
                new Project { Id = 1, Name = "Project1" },
                new Project { Id = 2, Name = "SuperImportantProject" },
            };

            projectRepositoryMock.Setup(x => x.GetAll()).Returns(projects);

            var subject = CreateSubject();

            var result = subject.OnGet();

            Assert.That(result, Is.InstanceOf<PageResult>());
            CollectionAssert.AreEquivalent(subject.Projects, projects);
        }

        [Test]
        public async Task OnPost_RecalculatesForecasts_ReturnsPage()
        {
            var subject = CreateSubject();

            var result = await subject.OnPost();

            Assert.That(result, Is.InstanceOf<PageResult>());
            monteCarloServiceMock.Verify(x => x.UpdateForecastsForAllProjects());
        }

        private IndexModel CreateSubject()
        {
            return new IndexModel(projectRepositoryMock.Object, monteCarloServiceMock.Object);
        }
    }
}
