using ArchUnitNET.NUnit;
using Lighthouse.Backend.API.Helpers;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    // The rules that decide what a Team may save as its refinement setup are plain functions of the values
    // being saved. Kept that way, every rule can be tested by handing it values, and none of them can quietly
    // start reading the database or the trackers while it judges a save.
    [TestFixture]
    public class RefinementModuleArchUnitTest
    {
        private const string ServicesImplementationPattern = @"^Lighthouse\.Backend\.Services\.Implementation($|\..*)";

        private static readonly ArchitectureModel Architecture = LighthouseArchitecture.Production;

        [Test]
        public void TheRefinementSettingsValidator_IsStatic()
        {
            var validator = typeof(RefinementSettingsValidator);

            Assert.That(validator.IsAbstract && validator.IsSealed, Is.True,
                "The refinement settings validator must stay a static class: it judges the values it is given and holds nothing.");
        }

        [Test]
        public void TheRefinementSettingsValidator_ReferencesNothingInServicesImplementation()
        {
            Types().That().HaveFullName(typeof(RefinementSettingsValidator).FullName!)
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(ServicesImplementationPattern))
                .Because("a validator that reaches into a service can no longer be tested by handing it values.")
                .Check(Architecture);
        }
    }
}
