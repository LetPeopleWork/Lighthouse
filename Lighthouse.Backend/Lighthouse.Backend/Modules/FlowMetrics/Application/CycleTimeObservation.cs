namespace Lighthouse.Backend.Modules.FlowMetrics.Application
{
    /// <summary>A closed item the Cycle Time scatterplot shows, with its Cycle Time in whole days.</summary>
    public sealed record CycleTimeObservation(int WorkItemId, int Days);
}
