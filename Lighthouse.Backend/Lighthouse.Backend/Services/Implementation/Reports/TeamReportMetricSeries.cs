using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.Reports
{
    /// <summary>A Team's series, read through the same service the Team's Metrics tab reads, so a report and the tab never disagree.</summary>
    public class TeamReportMetricSeries(IRepository<Team> teams, ITeamMetricsService metrics, ILighthouseClock clock) : IReportMetricSeries
    {
        public CycleTimePercentiles CycleTimePercentiles(ReportOwner owner, ReportWindow window)
        {
            var team = TeamOf(owner);
            var (start, end) = DaysOf(window);

            var percentiles = metrics.GetCycleTimePercentilesForTeam(team, start, end).ToDictionary(percentile => percentile.Percentile, percentile => percentile.Value);
            var finishedItems = metrics.GetClosedItemsForTeam(team, start, end).Count(item => item.CycleTime(clock.Zone) > 0);

            return new CycleTimePercentiles(percentiles[50], percentiles[70], percentiles[85], finishedItems);
        }

        public IReadOnlyList<int> DailyThroughput(ReportOwner owner, ReportWindow window)
        {
            var team = TeamOf(owner);
            var (start, end) = DaysOf(window);

            return DailyCounts(metrics.GetThroughputForTeam(team, start, end));
        }

        public IReadOnlyList<int> DailyWorkInProgress(ReportOwner owner, ReportWindow window)
        {
            var team = TeamOf(owner);
            var (start, end) = DaysOf(window);

            return DailyCounts(metrics.GetWorkInProgressOverTimeForTeam(team, start, end));
        }

        private static int[] DailyCounts(RunChartData runChart)
            => [.. runChart.WorkItemsPerUnitOfTime.OrderBy(day => day.Key).Select(day => day.Value.Count)];

        private Team TeamOf(ReportOwner owner)
        {
            if (owner.Kind != ReportOwnerKind.Team)
            {
                throw new ArgumentException($"Only a Team's series can be read here, not a {owner.Kind}'s", nameof(owner));
            }

            return teams.GetById(owner.Id) ?? throw new InvalidOperationException($"Team {owner.Id} does not exist");
        }

        private static (DateTime Start, DateTime End) DaysOf(ReportWindow window)
            => (window.Start.ToDateTime(TimeOnly.MinValue), window.End.ToDateTime(TimeOnly.MinValue));
    }
}
