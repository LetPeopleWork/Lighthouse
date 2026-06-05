using ArchUnitNET.NUnit;
using Lighthouse.Backend.Services.Implementation;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using ArchLoader = ArchUnitNET.Loader.ArchLoader;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class FlowEfficiencySeamArchUnitTest
    {
        private const string ComputeFlowEfficiencyName = "ComputeFlowEfficiency";

        private static readonly ArchitectureModel Architecture = new ArchLoader()
            .LoadAssemblies(typeof(BaseMetricsService).Assembly)
            .Build();

        [Test]
        public void ComputeFlowEfficiency_IsProtected_AndNotExposedViaAnyInterface()
        {
            MethodMembers().That().AreDeclaredIn(typeof(BaseMetricsService)).And().HaveNameContaining(ComputeFlowEfficiencyName)
                .Should().BeProtected()
                .Because(
                    "ADR-024 architectural enforcement: the flow-efficiency fold BaseMetricsService.ComputeFlowEfficiency " +
                    "must be protected (intra-inheritance only), never public, so it cannot be promoted to a shared " +
                    "interface (no IFlowEfficiencyService). Defining wait states is a labelling overlay folded into the " +
                    "existing metrics service, not a new per-state aggregation service. Keep the helper protected, or " +
                    "update ADR-024 first and amend this test.")
                .Check(Architecture);

            MethodMembers().That().AreDeclaredIn(Interfaces())
                .Should().NotHaveNameContaining(ComputeFlowEfficiencyName)
                .Because(
                    "ADR-024 architectural enforcement: no interface in the production assembly may declare a member " +
                    "named ComputeFlowEfficiency. The flow-efficiency fold is intra-inheritance only; the service surface " +
                    "exposes scope-specific GetFlowEfficiencyFor* methods, not the shared helper.")
                .Check(Architecture);
        }

        [Test]
        public void NoFlowEfficiencyAggregationService_WasIntroduced()
        {
            var flowEfficiencyServiceTypes = Architecture.Types
                .Where(t => t.Name.Contains("FlowEfficiency", StringComparison.Ordinal)
                    && (t.Name.EndsWith("Service", StringComparison.Ordinal) || t.Name.EndsWith("ServiceImpl", StringComparison.Ordinal)))
                .Select(t => t.FullName)
                .ToList();

            Assert.That(flowEfficiencyServiceTypes, Is.Empty,
                "ADR-024 architectural enforcement: no IFlowEfficiencyService / FlowEfficiencyService / per-state " +
                "aggregation service may exist. Flow efficiency is a fold on BaseMetricsService computed from the " +
                "team's/portfolio's WaitStates labelling overlay, not a standalone aggregation service. The following " +
                "FlowEfficiency*Service types were found and violate the seam: " +
                string.Join(", ", flowEfficiencyServiceTypes) + ". " +
                "Fold the computation into BaseMetricsService, or update ADR-024 first and amend this test.");
        }
    }
}
