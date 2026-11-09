using ArchUnitNET.NUnit;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class MetricsArchitectureTests
    {
        private static readonly ArchitectureModel Architecture = LighthouseArchitecture.Production;

        [Test]
        public void NoPerStateAggregationServiceExists_InProductionAssembly()
        {
            Types().That().HaveNameContaining("PerStateAggregation")
                .Should().NotExist()
                .Because(
                    "the repositories are the one place both scopes share for per-state data, " +
                    "so no class or interface named *PerStateAggregation* may exist. Per-state percentiles are " +
                    "computed independently inside TeamMetricsService and PortfolioMetricsService via the protected " +
                    "BaseMetricsService.ComputeAgeInStatePercentiles helper. Remove the aggregation service, or " +
                    "change the architecture decision record first and amend this test.")
                .Check(Architecture);
        }

        [Test]
        public void ComputeAgeInStatePercentiles_IsProtected_AndNoPublicPerStateAggregationMemberIsExposed()
        {
            MethodMembers().That().AreDeclaredIn(typeof(BaseMetricsService)).And().HaveNameStartingWith("ComputeAgeInStatePercentiles(")
                .Should().BeProtected()
                .Because(
                    "BaseMetricsService.ComputeAgeInStatePercentiles must be the single per-state aggregation helper " +
                    "shared by the team and portfolio services, and it must stay protected: not public and not an " +
                    "interface member. Keep the helper protected, or change the architecture decision record first and amend this test.")
                .Check(Architecture);

            MethodMembers().That().AreDeclaredIn(typeof(BaseMetricsService)).And().ArePublic()
                .Should().NotHaveNameContaining("PerStateAggregation").AndShould().NotHaveNameContaining("ComputeAgeInStatePercentiles")
                .Because(
                    "no public member named *PerStateAggregation* or " +
                    "*ComputeAgeInStatePercentiles* may be exposed on BaseMetricsService (the helper is for the " +
                    "derived services only; the service surface exposes scope-specific GetAgeInStatePercentilesFor* methods, " +
                    "not the shared helper).")
                .WithoutRequiringPositiveResults()
                .Check(Architecture);
        }
    }
}
