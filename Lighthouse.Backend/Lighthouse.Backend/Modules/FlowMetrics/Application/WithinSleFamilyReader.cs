using Lighthouse.Backend.Modules.FlowMetrics.Domain;

namespace Lighthouse.Backend.Modules.FlowMetrics.Application
{
    /// <summary>
    /// What the Within SLE record of one owner reads: the Cycle Times of the Work Items closed between the window
    /// start and the window end, and the SLE set on the owner now, or null when none is set.
    /// </summary>
    public sealed record WithinSleFamilyReader(
        Func<DateTime, DateTime, IReadOnlyList<int>> ReadClosedCycleTimes,
        Func<ServiceLevelExpectation?> CurrentSle);
}
