using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Modules.FlowMetrics.Application
{
#pragma warning disable CS9113 // Skeleton: the read uses these collaborators once it is filled in.
    public sealed class WithinSleOverTimeSeriesQuery(IWithinSleSnapshotRepository snapshots, IRepository<Team> teams, IRepository<Portfolio> portfolios) : IWithinSleOverTimeSeriesQuery
#pragma warning restore CS9113
    {
        public WithinSleOverTimeDto GetSeries(int ownerId, OwnerType ownerType, int horizon, DateOnly? from, DateOnly? to)
        {
            throw new NotImplementedException("Reading the Within SLE record is not built yet.");
        }
    }
}
