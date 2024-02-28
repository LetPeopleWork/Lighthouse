using CMFTAspNet.Models;
using CMFTAspNet.Services.Implementation;
using CMFTAspNet.Services.Implementation.BackgroundServices;
using CMFTAspNet.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace CMFTAspNet.Tests.Services.Implementation.BackgroundServices
{
    public class ForecastUpdateServiceTest
    {
        private IConfiguration configuration;
        private Mock<IRepository<Feature>> featureRepoMock;
        private Mock<IMonteCarloService> monteCarloServiceMock;
        private Mock<IServiceScopeFactory> serviceScopeFactoryMock;
        private Mock<ILogger<ForecastUpdateService>> loggerMock;

        [SetUp]
        public void Setup()
        {
            featureRepoMock = new Mock<IRepository<Feature>>();
            monteCarloServiceMock = new Mock<IMonteCarloService>();

            serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
            loggerMock = new Mock<ILogger<ForecastUpdateService>>();

            var scopeMock = new Mock<IServiceScope>();

            serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);
            scopeMock.Setup(x => x.ServiceProvider.GetService(typeof(IRepository<Feature>))).Returns(featureRepoMock.Object);
            scopeMock.Setup(x => x.ServiceProvider.GetService(typeof(IMonteCarloService))).Returns(monteCarloServiceMock.Object);

            SetupConfiguration(10);
        }

        [Test]
        public async Task ExecuteAsync_RefreshesAllFeaturesAsync()
        {
            var features = new List<Feature>
            {
                new Feature(),
                new Feature(),
                new Feature(),
            };

            featureRepoMock.Setup(x => x.GetAll()).Returns(features);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            monteCarloServiceMock.Verify(x => x.ForecastFeatures(features));
            featureRepoMock.Verify(x => x.Save());
        }

        private void SetupConfiguration(int interval)
        {
            var inMemorySettings = new Dictionary<string, string?> 
            {
                { "PeriodicRefresh:Forecasts:Interval", interval.ToString() },
                { "PeriodicRefresh:Forecasts:StartDelay", 0.ToString() },
            };

            configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        private ForecastUpdateService CreateSubject()
        {
            return new ForecastUpdateService(configuration, serviceScopeFactoryMock.Object, loggerMock.Object);
        }
    }
}
