using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Modules.FlowMetrics.Domain
{
#pragma warning disable S3453 // Skeleton: Of will build instances through this private constructor once it is filled in.
    public sealed class ServiceLevelExpectation
#pragma warning restore S3453
    {
#pragma warning disable S1144 // Skeleton: used by Of once it is filled in.
        private ServiceLevelExpectation(int percentile, int days)
        {
            Percentile = percentile;
            Days = days;
        }
#pragma warning restore S1144

        public int Percentile { get; }

        public int Days { get; }

        public static ServiceLevelExpectation? Of(int percentile, int days)
        {
            throw new NotImplementedException("The Service Level Expectation value is not built yet.");
        }

        public static ServiceLevelExpectation? For(WorkTrackingSystemOptionsOwner owner)
        {
            throw new NotImplementedException("The Service Level Expectation of an owner is not read yet.");
        }
    }
}
