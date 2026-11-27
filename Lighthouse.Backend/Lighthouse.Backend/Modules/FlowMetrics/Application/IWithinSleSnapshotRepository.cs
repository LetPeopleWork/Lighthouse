using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Modules.FlowMetrics.Application
{
    public interface IWithinSleSnapshotRepository : IRepository<WithinSleSnapshot>
    {
        IReadOnlyList<WithinSleSnapshot> GetSeries(int ownerId, OwnerType ownerType, int horizon, DateOnly? from, DateOnly? to);
    }
}
