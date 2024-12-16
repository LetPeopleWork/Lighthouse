using Lighthouse.Backend.Services.Implementation.BackgroundServices;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices
{
    public class DataRetentionServiceTest
    {
        private Mock<IServiceScopeFactory> serviceScopeFactoryMock;
        private Mock<ILogger<DataRetentionService>> loggerMock;

        private Mock<IFeatureHistoryService> featureHistoryServiceMock;

        [SetUp]
        public void SetUp()
        {
            serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
            loggerMock = new Mock<ILogger<DataRetentionService>>();

            featureHistoryServiceMock = new Mock<IFeatureHistoryService>();

            var scopeMock = new Mock<IServiceScope>();

            serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);
            scopeMock.Setup(x => x.ServiceProvider.GetService(typeof(IFeatureHistoryService))).Returns(featureHistoryServiceMock.Object);
        }

        [Test]
        public async Task ExecuteAsync_ReadyToClean_InvokesCleanupWithFeatureHistoryService()
        {
            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            featureHistoryServiceMock.Verify(x => x.CleanupData());
        }

        private DataRetentionService CreateSubject()
        {
            return new DataRetentionService(serviceScopeFactoryMock.Object, loggerMock.Object, 0);
        }
    }
}
