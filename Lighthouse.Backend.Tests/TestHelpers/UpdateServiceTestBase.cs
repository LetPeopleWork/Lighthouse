using Lighthouse.Backend.Services.Implementation.Update;
using Lighthouse.Backend.Services.Interfaces.Update;
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

            updateQueueServiceMock
                .Setup(x => x.EnqueueUpdate(It.IsAny<UpdateType>(), It.IsAny<int>(), It.IsAny<Func<IServiceProvider, Task>>()))
                .Callback((UpdateType updateType, int id, Func<IServiceProvider, Task> updateTask) =>
                {
                    updateTask(serviceProviderMock.Object).Wait();
                });
        }

        public IUpdateQueueService UpdateQueueService => updateQueueServiceMock.Object;

        protected void SetupServiceProviderMock<T>(T @object) where T : class
        {
            serviceProviderMock.Setup(x => x.GetService(typeof(T))).Returns(@object);
        }
    }
}
