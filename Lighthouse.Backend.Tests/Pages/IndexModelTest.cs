using Lighthouse.Backend.Models;
using Lighthouse.Backend.Pages;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

namespace Lighthouse.Backend.Tests.Pages
{
    public class IndexModelTest
    {
        private Mock<IRepository<Project>> projectRepositoryMock;
        
        private Mock<IMonteCarloService> monteCarloServiceMock;
        
        private Mock<IWorkItemCollectorService> workItemCollectorServiceMock;


        [SetUp]
        public void Setup()
        {
            projectRepositoryMock = new Mock<IRepository<Project>>();
            monteCarloServiceMock = new Mock<IMonteCarloService>();
            workItemCollectorServiceMock = new Mock<IWorkItemCollectorService>();
        }

        [Test]
        public void OnGet_LoadsAllFeatures()
        {
            var projects = SetupProjectAndFeatures();

            projectRepositoryMock.Setup(x => x.GetAll()).Returns(projects);

            var subject = CreateSubject();

            var result = subject.OnGet();

            Assert.That(result, Is.InstanceOf<PageResult>());
            CollectionAssert.AreEquivalent(subject.Projects, projects);
        }

        [Test]
        public async Task OnPost_ReloadsFeatures_RecalculatesForecasts_ReturnsPage()
        {
            var project = SetupProjectAndFeatures().Single();

            var subject = CreateSubject();

            var result = await subject.OnPost();

            Assert.That(result, Is.InstanceOf<PageResult>());
            monteCarloServiceMock.Verify(x => x.ForecastAllFeatures());
            workItemCollectorServiceMock.Verify(x => x.UpdateFeaturesForProject(project), Times.Once);
        }

        private List<Project> SetupProjectAndFeatures()
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

            return projects;
        }

        private IndexModel CreateSubject()
        {
            return new IndexModel(projectRepositoryMock.Object, monteCarloServiceMock.Object, workItemCollectorServiceMock.Object);
        }
    }
}
