using Lighthouse.Models;
using Lighthouse.Pages;
using Lighthouse.Services.Implementation;
using Lighthouse.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

namespace Lighthouse.Tests.Pages
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
        public void OnGet_LoadsAllFeatures()
        {
            var features = new List<Feature>
            {
                new Feature { Id = 1, Name = "Project1" },
                new Feature { Id = 2, Name = "SuperImportantProject" },
            };

            var project = new Project();
            project.UpdateFeatures(features);

            var projects = new List<Project> { project };

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
            monteCarloServiceMock.Verify(x => x.ForecastAllFeatures());
        }

        private IndexModel CreateSubject()
        {
            return new IndexModel(projectRepositoryMock.Object, monteCarloServiceMock.Object);
        }
    }
}
