using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;

namespace Lighthouse.Backend.Modules.FlowMetrics.Application
{
    public sealed record CycleTimeWorkItem(WorkItem WorkItem, IReadOnlyList<NamedCycleTimeValue> NamedCycleTimes);
}
