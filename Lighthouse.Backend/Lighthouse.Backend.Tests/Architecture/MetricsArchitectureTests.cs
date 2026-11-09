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
                    "ADR-021 / DDD-10 architectural enforcement: the repositories are the shared per-state seam, " +
                    "so no class or interface named *PerStateAggregation* may exist. Per-state percentiles are " +
                    "computed independently inside TeamMetricsService and PortfolioMetricsService via the protected " +
                    "BaseMetricsService.ComputeAgeInStatePercentiles helper. Remove the aggregation service, or " +
                    "update ADR-021 first and amend this test.")
                .Check(Architecture);
        }

        [Test]
        public void ComputeAgeInStatePercentiles_IsProtected_AndNoPublicPerStateAggregationMemberIsExposed()
        {
            MethodMembers().That().AreDeclaredIn(typeof(BaseMetricsService)).And().HaveNameStartingWith("ComputeAgeInStatePercentiles(")
                .Should().BeProtected()
                .Because(
                    "DDD-9 architectural enforcement: BaseMetricsService.ComputeAgeInStatePercentiles must be the single " +
                    "intra-inheritance per-state aggregation helper and must be protected (intra-inheritance only), not " +
                    "public and not an interface member. Keep the helper protected, or update ADR-021 first and amend this test.")
                .Check(Architecture);

            MethodMembers().That().AreDeclaredIn(typeof(BaseMetricsService)).And().ArePublic()
                .Should().NotHaveNameContaining("PerStateAggregation").AndShould().NotHaveNameContaining("ComputeAgeInStatePercentiles")
                .Because(
                    "DDD-9 architectural enforcement: no public member named *PerStateAggregation* or " +
                    "*ComputeAgeInStatePercentiles* may be exposed on BaseMetricsService (the helper is " +
                    "intra-inheritance only; the service surface exposes scope-specific GetAgeInStatePercentilesFor* methods, " +
                    "not the shared helper).")
                .WithoutRequiringPositiveResults()
                .Check(Architecture);
        }
    }
}
