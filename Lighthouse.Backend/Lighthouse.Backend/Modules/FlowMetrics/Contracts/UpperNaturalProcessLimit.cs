namespace Lighthouse.Backend.Modules.FlowMetrics.Contracts
{
    /// <summary>
    /// The upper natural process limit of a baseline, unrounded. A value above it is a large change on the process
    /// behaviour chart and an outlier on the Cycle Time scatterplot, so both ask this one object.
    /// </summary>
    public sealed record UpperNaturalProcessLimit
    {
        internal UpperNaturalProcessLimit(double days)
        {
            Days = days;
        }

        public double Days { get; }

        public bool IsExceededBy(int days) => days > Days;
    }
}
