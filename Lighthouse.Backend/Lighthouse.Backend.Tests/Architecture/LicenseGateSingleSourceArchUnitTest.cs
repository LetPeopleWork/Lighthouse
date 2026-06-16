using ArchUnitNET.NUnit;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class LicenseGateSingleSourceArchUnitTest
    {
        private static readonly ArchitectureModel Architecture = LighthouseArchitecture.Production;

        [Test]
        public void TeamMetricsService_DoesNotDependOnLicenseService()
        {
            Classes().That().Are(typeof(TeamMetricsService))
                .Should().NotDependOnAny(typeof(ILicenseService))
                .Because(
                    "DDD-9 architectural enforcement: the premium-gate must flow through " +
                    "IForecastFilterRuleService.GetEffectiveRuleSet, not via a direct ILicenseService " +
                    "dependency on TeamMetricsService (constructor parameter, field, or method body). " +
                    "If a direct dependency is genuinely needed, update the architectural decision record " +
                    "first and amend this test.")
                .Check(Architecture);
        }
    }
}
