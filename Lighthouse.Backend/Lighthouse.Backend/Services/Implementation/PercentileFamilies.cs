using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation
{
    /// <summary>
    /// Which percentile families an owner records, and which chart each one is read from. The day
    /// recorded live and the day filled in behind the series both take their families from here: two
    /// copies of this list can be changed apart, and the chart then shows a step on the date they
    /// stopped agreeing.
    ///
    /// Cycle time is read over the trailing window that ends on the day. Work item age is read as of
    /// the day alone, so its reader ignores the window start.
    /// </summary>
    public static class PercentileFamilies
    {
        public static IReadOnlyList<PercentileFamilyReader> For(Team team, ITeamMetricsService metrics)
        {
            return
            [
                new(MetricType.CycleTime, (windowStart, windowEnd) => metrics.GetCycleTimePercentilesForTeam(team, windowStart, windowEnd)),
                new(MetricType.WorkItemAge, (_, windowEnd) => metrics.GetWorkItemAgePercentilesForTeam(team, windowEnd)),
            ];
        }

        public static IReadOnlyList<PercentileFamilyReader> For(Portfolio portfolio, IPortfolioMetricsService metrics)
        {
            return
            [
                new(MetricType.CycleTime, (windowStart, windowEnd) => metrics.GetCycleTimePercentilesForPortfolio(portfolio, windowStart, windowEnd)),
                new(MetricType.WorkItemAge, (_, windowEnd) => metrics.GetWorkItemAgePercentilesForPortfolio(portfolio, windowEnd)),
            ];
        }
    }
}
