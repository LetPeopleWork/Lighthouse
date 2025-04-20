using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    public abstract class UpdateServiceTestBase
    {
        private readonly Mock<IUpdateQueueService> updateQueueServiceMock;

        private readonly Mock<IServiceProvider> serviceProviderMock;

        protected UpdateServiceTestBase()
        {
            updateQueueServiceMock = new Mock<IUpdateQueueService>();
            serviceProviderMock = new Mock<IServiceProvider>();
            
            var serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
            ServiceScopeFactory = serviceScopeFactoryMock.Object;
            
            var scopeMock = new Mock<IServiceScope>();
            scopeMock.SetupGet(x => x.ServiceProvider).Returns(serviceProviderMock.Object);
            serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);

            updateQueueServiceMock
                .Setup(x => x.EnqueueUpdate(It.IsAny<UpdateType>(), It.IsAny<int>(), It.IsAny<Func<IServiceProvider, Task>>()))
                .Callback((UpdateType updateType, int id, Func<IServiceProvider, Task> updateTask) =>
                {
                    updateTask(serviceProviderMock.Object).Wait();
                });
        }

        protected IServiceScopeFactory ServiceScopeFactory { get; }

        protected IUpdateQueueService UpdateQueueService => updateQueueServiceMock.Object;

        protected void SetupServiceProviderMock<T>(T @object) where T : class
        {
            serviceProviderMock.Setup(x => x.GetService(typeof(T))).Returns(@object);
        }
    }
}
