using ArchUnitNET.NUnit;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class ForecastFilterSeamArchUnitTest
    {
        private static readonly ArchitectureModel Architecture = LighthouseArchitecture.Production;

        [Test]
        public void OnlyWhitelistedTypes_InvokeForecastFilterRuleServiceFilter()
        {
            var filterMethod = MethodMembers().That()
                .AreDeclaredIn(typeof(IForecastFilterRuleService)).And().HaveNameStartingWith(nameof(IForecastFilterRuleService.Filter) + "(");

            Classes().That().AreNot(typeof(TeamMetricsService)).And().AreNot(typeof(ForecastFilterRuleService))
                .Should().NotCallAny(filterMethod)
                .Because(
                    "DDD-4 architectural enforcement: only TeamMetricsService and ForecastFilterRuleService " +
                    "may invoke IForecastFilterRuleService.Filter directly (single filter seam). Route the call " +
                    "through TeamMetricsService.GetThroughputForTeam (or another whitelisted seam) instead. If a " +
                    "new seam is genuinely required, update the architectural decision record first and amend this test.")
                .Check(Architecture);
        }
    }
}
