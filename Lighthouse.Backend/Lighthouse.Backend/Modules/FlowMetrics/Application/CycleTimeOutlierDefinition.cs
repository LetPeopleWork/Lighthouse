using Lighthouse.Backend.Modules.FlowMetrics.Domain;

namespace Lighthouse.Backend.Modules.FlowMetrics.Application
{
    /// <summary>A closed item the Cycle Time scatterplot shows, with its Cycle Time in whole days.</summary>
    public sealed record CycleTimeObservation(int WorkItemId, int Days);

    /// <summary>
    /// Which of the shown items are outliers. Either the baseline gave limits and the verdict names the items above
    /// them, or it gave none and no item is set apart.
    /// </summary>
    public abstract record OutlierVerdict
    {
        private OutlierVerdict()
        {
        }

        public sealed record Limited(UpperNaturalProcessLimit Limit, IReadOnlyList<int> OutlierWorkItemIds) : OutlierVerdict
        {
            public double UpperNaturalProcessLimitDays => Limit.Days;
        }

        public sealed record NoLimits : OutlierVerdict;
    }

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

    /// <summary>
    /// An outlier is an item whose Cycle Time is above the upper natural process limit of the baseline, the same
    /// limit the Cycle Time process behaviour chart draws. Without 2 baseline Cycle Times there is no limit.
    /// </summary>
    public sealed class UpperNaturalProcessLimitOutlierDefinition : ICycleTimeOutlierDefinition
    {
        public OutlierVerdict Judge(IReadOnlyList<int> baselineCycleTimes, IReadOnlyList<CycleTimeObservation> shown)
        {
            if (baselineCycleTimes.Count < 2)
            {
                return new OutlierVerdict.NoLimits();
            }

            var limit = XmRCalculator.UpperNaturalProcessLimitOf([.. baselineCycleTimes]);
            var outlierIds = shown
                .Where(observation => limit.IsExceededBy(observation.Days))
                .Select(observation => observation.WorkItemId)
                .ToList();

            return new OutlierVerdict.Limited(limit, outlierIds);
        }
    }
}
