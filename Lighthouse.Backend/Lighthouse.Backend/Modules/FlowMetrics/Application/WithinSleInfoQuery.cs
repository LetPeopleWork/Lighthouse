using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Modules.FlowMetrics.Domain;
using Lighthouse.Backend.Platform.Host;

namespace Lighthouse.Backend.Modules.FlowMetrics.Application
{
    public sealed class WithinSleInfoQuery(ITeamMetricsService teamMetrics, IPortfolioMetricsService portfolioMetrics, ILighthouseClock clock) : IWithinSleInfoQuery
    {
        public WithinSleInfoDto GetForTeam(Team team, DateTime startDate, DateTime endDate)
        {
            var cycleTimes = teamMetrics.GetCycleTimeDataForTeam(team, startDate, endDate)
                .Select(item => item.WorkItem.CycleTime(clock.Zone));

            return Answer(team, cycleTimes);
        }

        public WithinSleInfoDto GetForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            var cycleTimes = portfolioMetrics.GetCycleTimeDataForPortfolio(portfolio, startDate, endDate)
                .Select(feature => feature.CycleTime(clock.Zone));

            return Answer(portfolio, cycleTimes);
        }

        private static WithinSleInfoDto Answer(WorkTrackingSystemOptionsOwner owner, IEnumerable<int> cycleTimes)
        {
            var days = cycleTimes.Where(cycleTime => cycleTime > 0).ToList();
            var comparison = new InfoWidgetComparisonDto("none", "Within SLE", null, null, null, null, null, []);

            return AnswerOf(WithinSleShare.Of(days, ServiceLevelExpectation.For(owner)), comparison);
        }

        private static WithinSleInfoDto AnswerOf(WithinSleShare share, InfoWidgetComparisonDto comparison)
        {
            return share switch
            {
                WithinSleShare.Measured m => new WithinSleInfoDto(WithinSleVerdict.Measured, m.Closed, m.Within, m.Sle.Percentile, m.Sle.Days, m.SharePercent, comparison),
                WithinSleShare.TooFew t => new WithinSleInfoDto(WithinSleVerdict.TooFew, t.Closed, 0, t.Sle.Percentile, t.Sle.Days, null, comparison),
                WithinSleShare.NoSle n => new WithinSleInfoDto(WithinSleVerdict.NoSle, n.Closed, 0, null, null, null, comparison),
                _ => throw new ArgumentOutOfRangeException(nameof(share), share, "Unknown share within the Service Level Expectation."),
            };
        }
    }
}
