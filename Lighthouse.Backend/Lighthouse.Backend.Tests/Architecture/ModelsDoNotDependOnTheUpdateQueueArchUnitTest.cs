using ArchUnitNET.NUnit;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;

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
        private static readonly ArchitectureModel Architecture = LighthouseArchitecture.Production;

        [Test]
        public void TheDomainModel_KnowsNothingOfTheUpdateQueue_NotEvenTheNameOfAnUpdateType()
        {
            KernelRules.ModelsDoNotDependOnTheUpdateQueue().Check(Architecture);
        }
    }
}
