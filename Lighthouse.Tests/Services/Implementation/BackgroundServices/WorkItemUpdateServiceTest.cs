using Lighthouse.Models;
using Lighthouse.Services.Implementation;
using Lighthouse.Services.Implementation.BackgroundServices;
using Lighthouse.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Tests.Services.Implementation.BackgroundServices
{
    public class WorkItemUpdateServiceTest
    {
        private IConfiguration configuration;
        private Mock<IRepository<Project>> projectRepoMock;
        private Mock<IWorkItemCollectorService> workItemCollectorServiceMock;
        private Mock<IServiceScopeFactory> serviceScopeFactoryMock;
        private Mock<ILogger<WorkItemUpdateService>> loggerMock;

        [SetUp]
        public void Setup()
        {
            projectRepoMock = new Mock<IRepository<Project>>();
            workItemCollectorServiceMock = new Mock<IWorkItemCollectorService>();

            serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
            loggerMock = new Mock<ILogger<WorkItemUpdateService>>();

            var scopeMock = new Mock<IServiceScope>();

            serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);
            scopeMock.Setup(x => x.ServiceProvider.GetService(typeof(IRepository<Project>))).Returns(projectRepoMock.Object);
            scopeMock.Setup(x => x.ServiceProvider.GetService(typeof(IWorkItemCollectorService))).Returns(workItemCollectorServiceMock.Object);

            SetupConfiguration(10, 10);
        }

        [Test]
        public async Task ExecuteAsync_ReadyToRefresh_RefreshesAllProjectsAsync()
        {
            var project = CreateProject(DateTime.Now.AddDays(-1));
            SetupProjects([project]);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            workItemCollectorServiceMock.Verify(x => x.UpdateFeaturesForProject(project));
            projectRepoMock.Verify(x => x.Save());
        }

        [Test]
        public async Task ExecuteAsync_MultipleProjects_RefreshesAllProjectsAsync()
        {
            var project1 = CreateProject(DateTime.Now.AddDays(-1));
            var project2 = CreateProject(DateTime.Now.AddDays(-1));
            SetupProjects([project1, project2]);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            workItemCollectorServiceMock.Verify(x => x.UpdateFeaturesForProject(project1));
            workItemCollectorServiceMock.Verify(x => x.UpdateFeaturesForProject(project2));
            projectRepoMock.Verify(x => x.Save(), Times.Exactly(2));
        }

        [Test]
        public async Task ExecuteAsync_MultipleProjects_RefreshesOnlyProjectsWhereLastRefreshIsOlderThanConfiguredSetting()
        {
            var project1 = CreateProject(DateTime.Now.AddDays(-1));
            var project2 = CreateProject(DateTime.Now);

            SetupConfiguration(10, 360);

            SetupProjects([project1, project2]);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            workItemCollectorServiceMock.Verify(x => x.UpdateFeaturesForProject(project1));
            workItemCollectorServiceMock.Verify(x => x.UpdateFeaturesForProject(project2), Times.Never);
            projectRepoMock.Verify(x => x.Save(), Times.Exactly(1));
        }

        private void SetupProjects(IEnumerable<Project> projects)
        {
            projectRepoMock.Setup(x => x.GetAll()).Returns(projects);
        }

        private Project CreateProject(DateTime lastUpdateTime)
        {
            return new Project { ProjectUpdateTime = lastUpdateTime };
        }

        private void SetupConfiguration(int interval, int refreshAfter)
        {
            var inMemorySettings = new Dictionary<string, string?> 
            {
                { "PeriodicRefresh:WorkItems:Interval", interval.ToString() },
                { "PeriodicRefresh:WorkItems:RefreshAfter", refreshAfter.ToString() },
                { "PeriodicRefresh:Forecast:StartDelay", 0.ToString() },
            };

            configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        private WorkItemUpdateService CreateSubject()
        {
            return new WorkItemUpdateService(configuration, serviceScopeFactoryMock.Object, loggerMock.Object);
        }
    }
}
