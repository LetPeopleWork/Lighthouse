using Lighthouse.Backend.Modules.FlowMetrics.Application;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class ArchivedDeliveryReadPathArchUnitTest
    {
        private static readonly string[] ArchivedReadPathTypes =
        [
            "Lighthouse.Backend.API.DTO.Archived.ArchivedDeliveryProjection",
            "Lighthouse.Backend.API.DTO.Archived.ArchivedDeliveryIdentity",
        ];

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(BaseMetricsService).Assembly));

        [OneTimeTearDown]
        public void DisposeModule()
        {
            if (ProductionModule.IsValueCreated)
            {
                ProductionModule.Value.Dispose();
            }
        }

        [Test]
        public void ArchivedReadPath_CannotReachAnythingItCouldRecalculateFrom()
        {
            ApplicationCoreRules.ArchivedReadPathReachesNoRecalculation(ProductionModule.Value, ArchivedReadPathTypes).Check();
        }
    }
}
