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
            var closed = cycleTimesInDays.Count;

            if (sle is null)
            {
                return new NoSle(closed);
            }

            return Counted(sle.CountWithin(cycleTimesInDays), closed, sle);
        }

        public static WithinSleShare Counted(int within, int closed, ServiceLevelExpectation sle)
        {
            if (closed < MinimumClosed)
            {
                return new TooFew(closed, sle);
            }

            return new Measured(within, closed, sle);
        }

        public sealed record NoSle(int Closed) : WithinSleShare;

        public sealed record TooFew(int Closed, ServiceLevelExpectation Sle) : WithinSleShare;

        public sealed record Measured(int Within, int Closed, ServiceLevelExpectation Sle) : WithinSleShare
        {
            public double SharePercent => Math.Round(Within * 100.0 / Closed, 1, MidpointRounding.AwayFromZero);
        }
    }
}
