using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Lighthouse.Backend.Tests.MCP
{
    public abstract class LighthosueToolsBaseTest
    {
        private readonly Mock<IServiceProvider> serviceProviderMock;

        protected LighthosueToolsBaseTest()
        {
            serviceProviderMock = new Mock<IServiceProvider>();

            var serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
            ServiceScopeFactory = serviceScopeFactoryMock.Object;

            var scopeMock = new Mock<IServiceScope>();
            scopeMock.SetupGet(x => x.ServiceProvider).Returns(serviceProviderMock.Object);
            serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);
        }

        protected IServiceScopeFactory ServiceScopeFactory { get; }

        protected void SetupServiceProviderMock<T>(T @object) where T : class
        {
            serviceProviderMock.Setup(x => x.GetService(typeof(T))).Returns(@object);
        }
    }
}
