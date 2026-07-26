using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.Repositories
{
    public interface IPercentilesOverTimeSnapshotRepository : IRepository<PercentilesOverTimeSnapshot>
    {
        /// <summary>
        /// Serves the persisted percentiles series for one owner, metric family and horizon, ordered by
        /// RecordedAt. <paramref name="from"/> and <paramref name="to"/> are an optional window on
        /// RecordedAt, inclusive at both ends; either may be omitted independently, and omitting both
        /// returns the full history. The bounds are composed onto the query so the database applies them.
        /// </summary>
        IReadOnlyList<PercentilesOverTimeSnapshot> GetSeries(int ownerId, OwnerType ownerType, MetricType metricType, int? horizon, DateOnly? from, DateOnly? to);
    }
}
