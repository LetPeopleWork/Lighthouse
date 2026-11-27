using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.FlowMetrics.Domain;
using Lighthouse.Backend.Platform.Host;

namespace Lighthouse.Backend.Modules.FlowMetrics.Application
{
    /// <summary>
    /// What the Within SLE record of a Team or a Portfolio reads. The day recorded live and a day filled in later
    /// both take their reader from here, so the two cannot count a window differently. A Cycle Time of 0 belongs to
    /// an item with no start, which the Within SLE widget does not count either.
    /// </summary>
    public static class WithinSleFamilies
    {
        public static WithinSleFamilyReader For(Team team, ITeamMetricsService metrics, ILighthouseClock clock)
        {
            return new WithinSleFamilyReader(
                (windowStart, windowEnd) => Closed(metrics.GetCycleTimeDataForTeam(team, windowStart, windowEnd).Select(item => item.WorkItem.CycleTime(clock.Zone))),
                () => ServiceLevelExpectation.For(team));
        }

        public static WithinSleFamilyReader For(Portfolio portfolio, IPortfolioMetricsService metrics, ILighthouseClock clock)
        {
            return new WithinSleFamilyReader(
                (windowStart, windowEnd) => Closed(metrics.GetCycleTimeDataForPortfolio(portfolio, windowStart, windowEnd).Select(feature => feature.CycleTime(clock.Zone))),
                () => ServiceLevelExpectation.For(portfolio));
        }

        private static List<int> Closed(IEnumerable<int> cycleTimesInDays) => cycleTimesInDays.Where(cycleTime => cycleTime > 0).ToList();
    }
}
