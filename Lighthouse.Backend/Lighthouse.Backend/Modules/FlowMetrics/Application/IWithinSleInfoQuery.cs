using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;

namespace Lighthouse.Backend.Modules.FlowMetrics.Application
{
    public interface IWithinSleInfoQuery
    {
        WithinSleInfoDto GetForTeam(Team team, DateTime startDate, DateTime endDate);

        WithinSleInfoDto GetForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate);
    }
}
