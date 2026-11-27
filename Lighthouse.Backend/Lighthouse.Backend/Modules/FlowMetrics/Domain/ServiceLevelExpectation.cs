using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Modules.FlowMetrics.Domain
{
    public sealed class ServiceLevelExpectation : IEquatable<ServiceLevelExpectation>
    {
        private ServiceLevelExpectation(int percentile, int days)
        {
            Percentile = percentile;
            Days = days;
        }

        public int Percentile { get; }

        public int Days { get; }

        public static ServiceLevelExpectation? Of(int percentile, int days)
        {
            if (percentile is < 1 or > 100 || days < 1)
            {
                return null;
            }

            return new ServiceLevelExpectation(percentile, days);
        }

        public static ServiceLevelExpectation? For(WorkTrackingSystemOptionsOwner owner)
        {
            return Of(owner.ServiceLevelExpectationProbability, owner.ServiceLevelExpectationRange);
        }

        public int CountWithin(IEnumerable<int> cycleTimesInDays)
        {
            return cycleTimesInDays.Count(cycleTimeInDays => cycleTimeInDays <= Days);
        }

        public bool Equals(ServiceLevelExpectation? other)
        {
            return other is not null && other.Percentile == Percentile && other.Days == Days;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as ServiceLevelExpectation);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Percentile, Days);
        }
    }
}
