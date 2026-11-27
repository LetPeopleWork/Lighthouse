using Lighthouse.Backend.Models;
using Lighthouse.Backend.Platform.Host;

namespace Lighthouse.Backend.Modules.FlowMetrics.Application
{
    public static class WithinSleFamilies
    {
        public static WithinSleFamilyReader For(Team team, ITeamMetricsService metrics, ILighthouseClock clock)
        {
            throw new NotImplementedException("Reading a Team's Within SLE record is not built yet.");
        }

        public static WithinSleFamilyReader For(Portfolio portfolio, IPortfolioMetricsService metrics, ILighthouseClock clock)
        {
            throw new NotImplementedException("Reading a Portfolio's Within SLE record is not built yet.");
        }
    }
}
