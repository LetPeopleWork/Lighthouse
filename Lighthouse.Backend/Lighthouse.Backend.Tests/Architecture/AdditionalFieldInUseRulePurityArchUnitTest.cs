using ArchUnitNET.NUnit;
using Lighthouse.Backend.Models;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class AdditionalFieldInUseRulePurityArchUnitTest
    {
        private const string AnythingOfLighthouseOutsideModels = @"^Lighthouse\.Backend(?!\.Models(\.|$))(\.|$)";

        private static readonly ArchitectureModel Architecture = LighthouseArchitecture.Production;

        [Test]
        public void WhichRemovedFieldsAreStillInUse_IsDecidedFromPlainDataAlone()
        {
            Types().That()
                .Are(typeof(AdditionalFieldInUse), typeof(RemovedAdditionalField), typeof(DefaultNamingAField), typeof(OwnerNamingFields))
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(AnythingOfLighthouseOutsideModels))
                .Because(
                    "the rule that refuses removing a field still in use is handed the removed fields, the defaults and " +
                    "the owners' own values, and must not read the database or reach into the services itself: the " +
                    "connection save does every read, so the rule can be tested on its own with plain values.")
                .Check(Architecture);
        }
    }
}
