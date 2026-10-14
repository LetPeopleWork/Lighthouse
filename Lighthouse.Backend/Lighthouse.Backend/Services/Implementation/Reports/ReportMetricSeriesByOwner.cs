using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Interfaces.Reports;

namespace Lighthouse.Backend.Services.Implementation.Reports
{
    /// <summary>Reads each owner's series from the series of its kind: a Team's over its Work Items, a Portfolio's over its Features.</summary>
    public class ReportMetricSeriesByOwner(TeamReportMetricSeries teamSeries, PortfolioReportMetricSeries portfolioSeries) : IReportMetricSeries
    {
        public CycleTimePercentiles CycleTimePercentiles(ReportOwner owner, ReportWindow window) => SeriesOf(owner).CycleTimePercentiles(owner, window);

        public IReadOnlyList<int> DailyThroughput(ReportOwner owner, ReportWindow window) => SeriesOf(owner).DailyThroughput(owner, window);

        public IReadOnlyList<int> DailyWorkInProgress(ReportOwner owner, ReportWindow window) => SeriesOf(owner).DailyWorkInProgress(owner, window);

        public IReadOnlyList<int> DailyTotalWorkItemAge(ReportOwner owner, ReportWindow window) => SeriesOf(owner).DailyTotalWorkItemAge(owner, window);

        private IReportMetricSeries SeriesOf(ReportOwner owner)
            => owner.Kind == ReportOwnerKind.Portfolio ? portfolioSeries : teamSeries;
    }
}
