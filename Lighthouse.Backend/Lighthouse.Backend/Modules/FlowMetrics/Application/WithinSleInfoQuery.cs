using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Platform.Host;

namespace Lighthouse.Backend.Modules.FlowMetrics.Application
{
#pragma warning disable CS9113 // Skeleton: the reads use these collaborators once they are filled in.
    public sealed class WithinSleInfoQuery(ITeamMetricsService teamMetrics, IPortfolioMetricsService portfolioMetrics, ILighthouseClock clock) : IWithinSleInfoQuery
#pragma warning restore CS9113
    {
        public WithinSleInfoDto GetForTeam(Team team, DateTime startDate, DateTime endDate)
        {
            throw new NotImplementedException("The Within SLE read for a Team is not built yet.");
        }

        public WithinSleInfoDto GetForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            throw new NotImplementedException("The Within SLE read for a Portfolio is not built yet.");
        }
    }
}
