using Lighthouse.Backend.Services.Implementation;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// A forecast can be stopped part-way, and the stop travels as a token handed down from the update
    /// that runs it. The token is passed into the forecast; the domain model never goes looking for it. A
    /// Model that reached into the update queue would know which update it is running under, and a Feature
    /// or a forecast would then behave differently depending on who asked for it.
    /// </summary>
    [TestFixture]
    [Category("epic-6311-responsive-during-updates")]
    [Category("slice-02")]
    public class ModelsDoNotDependOnTheUpdateQueueArchUnitTest
    {
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
        public void TheDomainModel_KnowsNothingOfTheUpdateQueue_NotEvenTheNameOfAnUpdateType()
        {
            KernelRules.ModelsDoNotDependOnTheUpdateQueue(ProductionModule.Value).Check();
        }
    }
}
