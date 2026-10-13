using Lighthouse.Backend.Models;
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
            var team = teams.GetById(owner.Id) ?? throw new InvalidOperationException($"Team {owner.Id} does not exist");
            var start = window.Start.ToDateTime(TimeOnly.MinValue);
            var end = window.End.ToDateTime(TimeOnly.MinValue);

            var percentiles = metrics.GetCycleTimePercentilesForTeam(team, start, end).ToDictionary(percentile => percentile.Percentile, percentile => percentile.Value);
            var finishedItems = metrics.GetClosedItemsForTeam(team, start, end).Count(item => item.CycleTime(clock.Zone) > 0);

            return new CycleTimePercentiles(percentiles[50], percentiles[70], percentiles[85], finishedItems);
        }
    }
}
