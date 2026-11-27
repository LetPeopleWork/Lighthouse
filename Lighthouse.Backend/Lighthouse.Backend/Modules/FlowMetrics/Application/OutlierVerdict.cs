using Lighthouse.Backend.Modules.FlowMetrics.Domain;

namespace Lighthouse.Backend.Modules.FlowMetrics.Application
{
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
}
