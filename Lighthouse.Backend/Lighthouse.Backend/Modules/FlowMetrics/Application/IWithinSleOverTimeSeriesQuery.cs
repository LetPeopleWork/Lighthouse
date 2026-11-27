using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;

namespace Lighthouse.Backend.Modules.FlowMetrics.Application
{
    public interface IWithinSleOverTimeSeriesQuery
    {
        WithinSleOverTimeDto GetSeries(int ownerId, OwnerType ownerType, int horizon, DateOnly? from, DateOnly? to);
    }
}
