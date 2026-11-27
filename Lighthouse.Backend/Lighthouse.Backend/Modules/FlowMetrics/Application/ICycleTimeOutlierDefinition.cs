namespace Lighthouse.Backend.Modules.FlowMetrics.Application
{
    /// <summary>
    /// Decides which shown items are outliers against the baseline's Cycle Times. Another rule is supplied by
    /// registering another implementation.
    /// </summary>
    public interface ICycleTimeOutlierDefinition
    {
        /// <summary>The rule the metrics use when nothing else is registered: above the upper natural process limit.</summary>
        static ICycleTimeOutlierDefinition Default { get; } = new UpperNaturalProcessLimitOutlierDefinition();

        OutlierVerdict Judge(IReadOnlyList<int> baselineCycleTimes, IReadOnlyList<CycleTimeObservation> shown);
    }
}
