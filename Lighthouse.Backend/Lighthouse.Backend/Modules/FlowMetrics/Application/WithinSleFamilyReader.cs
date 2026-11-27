using Lighthouse.Backend.Modules.FlowMetrics.Domain;

namespace Lighthouse.Backend.Modules.FlowMetrics.Application
{
    /// <summary>
    /// What the Within SLE record of one owner reads: the Cycle Times of the Work Items closed between the window
    /// start and the window end, and the SLE set on the owner now, or null when none is set.
    ///
    /// The Cycle Times come back null when the window reaches back further than the owner still keeps finished
    /// work. An empty list cannot say that, because a window in which nothing closed is a day worth keeping.
    /// </summary>
    public sealed record WithinSleFamilyReader(
        Func<DateTime, DateTime, IReadOnlyList<int>?> ReadClosedCycleTimes,
        Func<ServiceLevelExpectation?> CurrentSle);
}
