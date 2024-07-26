using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.BackgroundServices;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices
{
    public class ForecastUpdateServiceTest
    {
        private Mock<IMonteCarloService> monteCarloServiceMock;
        private Mock<IAppSettingService> appSettingServiceMock;

        private Mock<IServiceScopeFactory> serviceScopeFactoryMock;
        private Mock<ILogger<ForecastUpdateService>> loggerMock;

        [SetUp]
        public void Setup()
        {
            monteCarloServiceMock = new Mock<IMonteCarloService>();
            appSettingServiceMock = new Mock<IAppSettingService>();

            serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
            loggerMock = new Mock<ILogger<ForecastUpdateService>>();

            var scopeMock = new Mock<IServiceScope>();

            serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);
            scopeMock.Setup(x => x.ServiceProvider.GetService(typeof(IMonteCarloService))).Returns(monteCarloServiceMock.Object);
            scopeMock.Setup(x => x.ServiceProvider.GetService(typeof(IAppSettingService))).Returns(appSettingServiceMock.Object);

            SetupRefreshSettings(10);
        }

        [Test]
        public async Task ExecuteAsync_RefreshesAllFeaturesAsync()
        {
            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            monteCarloServiceMock.Verify(x => x.ForecastAllFeatures());
        }

        private void SetupRefreshSettings(int interval)
        {
            var refreshSettings = new RefreshSettings { Interval = interval, RefreshAfter = 0, StartDelay = 0 };
            appSettingServiceMock.Setup(x => x.GetForecastRefreshSettings()).Returns(refreshSettings);
        }

        private ForecastUpdateService CreateSubject()
        {
            return new ForecastUpdateService(serviceScopeFactoryMock.Object, loggerMock.Object);
        }
    }
}
