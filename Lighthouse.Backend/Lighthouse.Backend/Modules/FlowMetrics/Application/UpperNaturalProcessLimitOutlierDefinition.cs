using Lighthouse.Backend.Modules.FlowMetrics.Contracts;
using Lighthouse.Backend.Modules.FlowMetrics.Domain;

namespace Lighthouse.Backend.Modules.FlowMetrics.Application
{
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
