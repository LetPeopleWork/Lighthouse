using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.BackgroundServices;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices
{
    public class FeatureUpdateServiceTest
    {
        private Mock<IRepository<Project>> projectRepoMock;
        private Mock<IWorkItemCollectorService> workItemCollectorServiceMock;
        private Mock<IAppSettingService> appSettingServiceMock;

        private Mock<IServiceScopeFactory> serviceScopeFactoryMock;
        private Mock<ILogger<FeatureUpdateService>> loggerMock;

        [SetUp]
        public void Setup()
        {
            projectRepoMock = new Mock<IRepository<Project>>();
            workItemCollectorServiceMock = new Mock<IWorkItemCollectorService>();
            appSettingServiceMock = new Mock<IAppSettingService>();

            serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
            loggerMock = new Mock<ILogger<FeatureUpdateService>>();

            var scopeMock = new Mock<IServiceScope>();

            serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);
            scopeMock.Setup(x => x.ServiceProvider.GetService(typeof(IRepository<Project>))).Returns(projectRepoMock.Object);
            scopeMock.Setup(x => x.ServiceProvider.GetService(typeof(IWorkItemCollectorService))).Returns(workItemCollectorServiceMock.Object);
            scopeMock.Setup(x => x.ServiceProvider.GetService(typeof(IAppSettingService))).Returns(appSettingServiceMock.Object);

            SetupRefreshSettings(10, 10);
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

            SetupRefreshSettings(10, 360);

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

        private void SetupRefreshSettings(int interval, int refreshAfter)
        {
            var refreshSettings = new RefreshSettings { Interval = interval, RefreshAfter = refreshAfter, StartDelay = 0 };
            appSettingServiceMock.Setup(x => x.GetFeaturRefreshSettings()).Returns(refreshSettings);
        }

        private FeatureUpdateService CreateSubject()
        {
            return new FeatureUpdateService(serviceScopeFactoryMock.Object, loggerMock.Object);
        }
    }
}
