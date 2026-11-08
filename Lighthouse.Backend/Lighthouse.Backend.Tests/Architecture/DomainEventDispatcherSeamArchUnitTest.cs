using Lighthouse.Backend.Services.Implementation;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class DomainEventDispatcherSeamArchUnitTest
    {
        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(TeamMetricsService).Assembly));

        [OneTimeTearDown]
        public void DisposeModule()
        {
            if (ProductionModule.IsValueCreated)
            {
                ProductionModule.Value.Dispose();
            }
        }

        [Test]
        public void DomainEventSeamTypes_DoNotResolveViaServiceLocator()
        {
            ApplicationCoreRules.HandlersUseNoServiceLocator(ProductionModule.Value).Check();
        }

        [Test]
        public void DomainEventHandlerMethods_MustAwaitRepositorySaves_NeverFireAndForget()
        {
            ApplicationCoreRules.HandlersAwaitRepositorySaves(ProductionModule.Value).Check();
        }
    }
}
