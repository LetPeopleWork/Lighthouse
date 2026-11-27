using ArchUnitNET.NUnit;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
using Lighthouse.Backend.Modules.FlowMetrics.Contracts;
using Lighthouse.Backend.Modules.Forecasting.Application;
using Lighthouse.Backend.Modules.Forecasting.Contracts;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class ForecastFilterSeamArchUnitTest
    {
        private static readonly ArchitectureModel Architecture = LighthouseArchitecture.Production;

        [Test]
        public void OnlyTheMetricsServiceAndTheRuleService_FilterThroughput()
        {
            var filterMethod = MethodMembers().That()
                .AreDeclaredIn(typeof(IThroughputFilter), typeof(IForecastFilterRuleService))
                .And().HaveNameStartingWith(nameof(IThroughputFilter.Filter) + "(");

            Classes().That().AreNot(typeof(TeamMetricsService)).And().AreNot(typeof(ForecastFilterRuleService))
                .Should().NotCallAny(filterMethod)
                .Because(
                    "a Team's forecast filter is applied in one place, so every forecast and every metric that respects " +
                    "the filter counts the same work items: only TeamMetricsService and ForecastFilterRuleService may call " +
                    "Filter on IThroughputFilter or IForecastFilterRuleService. Ask TeamMetricsService for the filtered " +
                    "throughput (GetThroughputForTeam or GetForecastThroughputStatus) instead of filtering work items yourself.")
                .Check(Architecture);
        }
    }
}
