using System.Globalization;
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
            var (previousStart, previousEnd) = PrecedingPeriod(startDate, endDate);

            return Answer(
                team,
                CycleTimesOfTeam(team, startDate, endDate),
                CycleTimesOfTeam(team, previousStart, previousEnd),
                startDate, endDate, previousStart, previousEnd);
        }

        public WithinSleInfoDto GetForPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate)
        {
            var (previousStart, previousEnd) = PrecedingPeriod(startDate, endDate);

            return Answer(
                portfolio,
                CycleTimesOfPortfolio(portfolio, startDate, endDate),
                CycleTimesOfPortfolio(portfolio, previousStart, previousEnd),
                startDate, endDate, previousStart, previousEnd);
        }

        private IEnumerable<int> CycleTimesOfTeam(Team team, DateTime startDate, DateTime endDate)
            => teamMetrics.GetCycleTimeDataForTeam(team, startDate, endDate).Select(item => item.WorkItem.CycleTime(clock.Zone));

        private IEnumerable<int> CycleTimesOfPortfolio(Portfolio portfolio, DateTime startDate, DateTime endDate)
            => portfolioMetrics.GetCycleTimeDataForPortfolio(portfolio, startDate, endDate).Select(feature => feature.CycleTime(clock.Zone));

        private static (DateTime Start, DateTime End) PrecedingPeriod(DateTime startDate, DateTime endDate)
        {
            var periodDays = (endDate.Date - startDate.Date).Days + 1;
            var previousEnd = startDate.Date.AddDays(-1);

            return (previousEnd.AddDays(-(periodDays - 1)), previousEnd);
        }

        private static WithinSleInfoDto Answer(
            WorkTrackingSystemOptionsOwner owner,
            IEnumerable<int> currentCycleTimes,
            IEnumerable<int> previousCycleTimes,
            DateTime currentStart, DateTime currentEnd, DateTime previousStart, DateTime previousEnd)
        {
            var sle = ServiceLevelExpectation.For(owner);
            var current = WithinSleShare.Of(Closed(currentCycleTimes), sle);
            var previous = WithinSleShare.Of(Closed(previousCycleTimes), sle);

            return AnswerOf(current, ComparisonOf(current, previous, currentStart, currentEnd, previousStart, previousEnd));
        }

        private static List<int> Closed(IEnumerable<int> cycleTimes) => cycleTimes.Where(cycleTime => cycleTime > 0).ToList();

        private static InfoWidgetComparisonDto ComparisonOf(
            WithinSleShare current, WithinSleShare previous,
            DateTime currentStart, DateTime currentEnd, DateTime previousStart, DateTime previousEnd)
        {
            if (current is not WithinSleShare.Measured now || previous is not WithinSleShare.Measured before)
            {
                return new InfoWidgetComparisonDto("none", "Within SLE", null, null, null, null, null, []);
            }

            var points = Math.Round(now.SharePercent - before.SharePercent, 1, MidpointRounding.AwayFromZero);
            var direction = Math.Sign(points) switch { > 0 => "up", < 0 => "down", _ => "flat" };

            return new InfoWidgetComparisonDto(
                direction, "Within SLE",
                $"{currentStart:yyyy-MM-dd} – {currentEnd:yyyy-MM-dd}", $"{now.SharePercent.ToString(CultureInfo.InvariantCulture)}%",
                $"{previousStart:yyyy-MM-dd} – {previousEnd:yyyy-MM-dd}", $"{before.SharePercent.ToString(CultureInfo.InvariantCulture)}%",
                $"{points.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture)} pts", []);
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
