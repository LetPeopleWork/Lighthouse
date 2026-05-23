using System.Reflection;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces.Licensing;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class LicenseGateSingleSourceArchUnitTest
    {
        [Test]
        public void TeamMetricsService_PublicConstructors_DoNotDependOnLicenseService()
        {
            var serviceType = typeof(TeamMetricsService);
            var publicConstructors = serviceType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

            Assert.That(publicConstructors, Is.Not.Empty,
                "TeamMetricsService must expose at least one public constructor.");

            foreach (var constructor in publicConstructors)
            {
                foreach (var parameter in constructor.GetParameters())
                {
                    Assert.That(
                        typeof(ILicenseService).IsAssignableFrom(parameter.ParameterType),
                        Is.False,
                        $"TeamMetricsService constructor parameter '{parameter.Name}' of type " +
                        $"'{parameter.ParameterType.Name}' is assignable to ILicenseService. " +
                        "DDD-9 architectural enforcement: the premium-gate must flow through " +
                        "IForecastFilterRuleService.GetEffectiveRuleSet, not via a direct ILicenseService " +
                        "dependency on TeamMetricsService. If a direct dependency is genuinely needed, " +
                        "update the architectural decision record first and amend this test.");
                }
            }
        }

        [Test]
        public void TeamMetricsService_InstanceFields_DoNotReferenceLicenseService()
        {
            var serviceType = typeof(TeamMetricsService);
            var instanceFields = serviceType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (var field in instanceFields)
            {
                Assert.That(
                    typeof(ILicenseService).IsAssignableFrom(field.FieldType),
                    Is.False,
                    $"TeamMetricsService instance field '{field.Name}' of type " +
                    $"'{field.FieldType.Name}' is assignable to ILicenseService. " +
                    "DDD-9 architectural enforcement: the premium-gate must flow through " +
                    "IForecastFilterRuleService.GetEffectiveRuleSet, not via a direct ILicenseService " +
                    "field on TeamMetricsService. If a direct field is genuinely needed, " +
                    "update the architectural decision record first and amend this test.");
            }
        }
    }
}
