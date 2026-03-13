using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices.Update
{
    public class ForecastUpdaterTest : UpdateServiceTestBase
    {
        private Mock<IRepository<Portfolio>> portfolioRepositoryMock;
        private Mock<IAppSettingService> appSettingServiceMock;
        private Mock<IForecastService> forecastServiceMock;
        private Mock<IWriteBackTriggerService> writeBackTriggerServiceMock;

        private int idCounter = 0;

        [SetUp]
        public void Setup()
        {
            portfolioRepositoryMock = new Mock<IRepository<Portfolio>>();
            appSettingServiceMock = new Mock<IAppSettingService>();
            forecastServiceMock = new Mock<IForecastService>();
            writeBackTriggerServiceMock = new Mock<IWriteBackTriggerService>();

            SetupServiceProviderMock(appSettingServiceMock.Object);
            SetupServiceProviderMock(portfolioRepositoryMock.Object);
            SetupServiceProviderMock(forecastServiceMock.Object);
            SetupServiceProviderMock(writeBackTriggerServiceMock.Object);
        }

        [Test]
        public void Update_ShouldDoNothing_WhenProjectNotFound()
        {
            // Arrange
            portfolioRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).Returns((Portfolio)null);

            var subject = CreateSubject();

            // Act
            subject.TriggerUpdate(1);

            // Assert
            portfolioRepositoryMock.Verify(x => x.GetById(It.IsAny<int>()), Times.Once);
            portfolioRepositoryMock.VerifyNoOtherCalls();
            forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(It.IsAny<Portfolio>()), Times.Never);
        }

        [Test]
        public void Update_ShouldCallUpdateForecastsForProject_WhenProjectIsFound()
        {
            // Arrange
            var project = CreatePortfolio();

            portfolioRepositoryMock.Setup(x => x.GetById(project.Id)).Returns(project);

            var subject = CreateSubject();

            // Act
            subject.TriggerUpdate(project.Id);

            // Assert
            forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(project), Times.Once);
        }
        
        [Test]
        public void Update_ShouldTriggerForecastWriteBackForPortfolio_WhenProjectIsFound()
        {
            var portfolio = CreatePortfolio();

            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);

            var subject = CreateSubject();

            subject.TriggerUpdate(portfolio.Id);

            writeBackTriggerServiceMock.Verify(x => x.TriggerForecastWriteBackForPortfolio(portfolio), Times.Once);
        }

        [Test]
        public void Update_ShouldNotTriggerWriteBack_WhenProjectNotFound()
        {
            portfolioRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).Returns((Portfolio)null);

            var subject = CreateSubject();

            subject.TriggerUpdate(1);

            writeBackTriggerServiceMock.Verify(x => x.TriggerForecastWriteBackForPortfolio(It.IsAny<Portfolio>()), Times.Never);
        }

        [Test]
        public void Update_ShouldHandleException_WhenUpdateForecastsForProjectThrows()
        {
            // Arrange
            var portfolio = CreatePortfolio();

            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);
            forecastServiceMock.Setup(x => x.UpdateForecastsForPortfolio(It.IsAny<Portfolio>())).ThrowsAsync(new Exception("Test exception"));

            var subject = CreateSubject();

            // Act & Assert
            Assert.DoesNotThrow(() => subject.TriggerUpdate(portfolio.Id));
            forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(portfolio), Times.Once);
        }

        private ForecastUpdater CreateSubject()
        {
            return new ForecastUpdater(Mock.Of<ILogger<ForecastUpdater>>(), ServiceScopeFactory, UpdateQueueService);
        }

        private Portfolio CreatePortfolio(params Feature[] features)
        {
            var portfolio = CreatePortfolio(DateTime.UtcNow, features);

            return portfolio;
        }

        private Portfolio CreatePortfolio(DateTime lastUpdatedTime, params Feature[] features)
        {
            var portfolio = new Portfolio
            {
                Name = "Project",
                Id = idCounter++,
                UpdateTime = lastUpdatedTime,
            };
            portfolio.UpdateFeatures(features);
            return portfolio;
        }
    }
}
