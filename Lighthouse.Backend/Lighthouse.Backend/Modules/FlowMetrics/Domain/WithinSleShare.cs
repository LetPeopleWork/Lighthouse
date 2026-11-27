namespace Lighthouse.Backend.Modules.FlowMetrics.Domain
{
    public abstract record WithinSleShare
    {
        public const int MinimumClosed = 5;

        private protected WithinSleShare()
        {
        }

        public static WithinSleShare Of(IReadOnlyCollection<int> cycleTimesInDays, ServiceLevelExpectation? sle)
        {
            throw new NotImplementedException("The share within the Service Level Expectation is not counted yet.");
        }

        public sealed record NoSle(int Closed) : WithinSleShare;

        public sealed record TooFew(int Closed, ServiceLevelExpectation Sle) : WithinSleShare;

        public sealed record Measured(int Within, int Closed, ServiceLevelExpectation Sle) : WithinSleShare
        {
#pragma warning disable S2325 // Skeleton: reads Within and Closed once it is filled in.
            public double SharePercent => throw new NotImplementedException("The share percentage is not computed yet.");
#pragma warning restore S2325
        }
    }
}
