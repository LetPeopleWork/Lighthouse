using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;

namespace Lighthouse.Backend.Modules.FlowMetrics.Contracts
{
    public sealed record CycleTimeFeature(Feature Feature, IReadOnlyList<NamedCycleTimeValue> NamedCycleTimes);
}
