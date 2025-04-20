using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices.Update
{
    public class ForecastUpdateServiceTest : UpdateServiceTestBase
    {
        private Mock<IRepository<Project>> projectRepositoryMock;
        private Mock<IAppSettingService> appSettingServiceMock;
        private Mock<IForecastService> forecastServiceMock;

        private int idCounter = 0;

        [SetUp]
        public void Setup()
        {
            projectRepositoryMock = new Mock<IRepository<Project>>();
            appSettingServiceMock = new Mock<IAppSettingService>();
            forecastServiceMock = new Mock<IForecastService>();

            SetupServiceProviderMock(appSettingServiceMock.Object);
            SetupServiceProviderMock(projectRepositoryMock.Object);
            SetupServiceProviderMock(forecastServiceMock.Object);
        }

        [Test]
        public void Update_ShouldDoNothing_WhenProjectNotFound()
        {
            // Arrange
            projectRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).Returns((Project)null);

            var subject = CreateSubject();

            // Act
            subject.TriggerUpdate(1);

            // Assert
            projectRepositoryMock.Verify(x => x.GetById(It.IsAny<int>()), Times.Once);
            projectRepositoryMock.VerifyNoOtherCalls();
            forecastServiceMock.Verify(x => x.UpdateForecastsForProject(It.IsAny<Project>()), Times.Never);
        }

        [Test]
        public void Update_ShouldCallUpdateForecastsForProject_WhenProjectIsFound()
        {
            // Arrange
            var project = CreateProject();
            var serviceProviderMock = new Mock<IServiceProvider>();

            projectRepositoryMock.Setup(x => x.GetById(project.Id)).Returns(project);

            var subject = CreateSubject();

            // Act
            subject.TriggerUpdate(project.Id);

            // Assert
            forecastServiceMock.Verify(x => x.UpdateForecastsForProject(project), Times.Once);
        }

        [Test]
        public void Update_ShouldHandleException_WhenUpdateForecastsForProjectThrows()
        {
            // Arrange
            var project = CreateProject();

            projectRepositoryMock.Setup(x => x.GetById(project.Id)).Returns(project);
            forecastServiceMock.Setup(x => x.UpdateForecastsForProject(It.IsAny<Project>())).ThrowsAsync(new Exception("Test exception"));

            var subject = CreateSubject();

            // Act & Assert
            Assert.DoesNotThrow(() => subject.TriggerUpdate(project.Id));
            forecastServiceMock.Verify(x => x.UpdateForecastsForProject(project), Times.Once);
        }

        private ForecastUpdateService CreateSubject()
        {
            return new ForecastUpdateService(Mock.Of<ILogger<ForecastUpdateService>>(), ServiceScopeFactory, UpdateQueueService);
        }

        private Project CreateProject(params Feature[] features)
        {
            var project = CreateProject(DateTime.UtcNow, features);
            project.Teams.AddRange(features.SelectMany(f => f.Teams).Distinct());

            return project;
        }

        private Project CreateProject(DateTime lastUpdatedTime, params Feature[] features)
        {
            var project = new Project
            {
                Name = "Project",
                Id = idCounter++,
                ProjectUpdateTime = lastUpdatedTime,
            };
            project.UpdateFeatures(features);
            return project;
        }
    }
}
