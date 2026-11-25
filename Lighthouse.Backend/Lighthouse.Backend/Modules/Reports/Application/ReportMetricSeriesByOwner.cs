using Lighthouse.Backend.Models.Reports;

namespace Lighthouse.Backend.Modules.Reports.Application
{
    /// <summary>Reads each owner's series from the series of its kind: a Team's over its Work Items, a Portfolio's over its Features.</summary>
    public class ReportMetricSeriesByOwner(TeamReportMetricSeries teamSeries, PortfolioReportMetricSeries portfolioSeries) : IReportMetricSeries
    {
        public CycleTimePercentiles CycleTimePercentiles(ReportOwner owner, ReportWindow window) => SeriesOf(owner).CycleTimePercentiles(owner, window);

        public IReadOnlyList<int> DailyThroughput(ReportOwner owner, ReportWindow window) => SeriesOf(owner).DailyThroughput(owner, window);

        public IReadOnlyList<int> DailyArrivals(ReportOwner owner, ReportWindow window) => SeriesOf(owner).DailyArrivals(owner, window);

        public IReadOnlyList<int> DailyWorkInProgress(ReportOwner owner, ReportWindow window) => SeriesOf(owner).DailyWorkInProgress(owner, window);

        public IReadOnlyList<int> DailyTotalWorkItemAge(ReportOwner owner, ReportWindow window) => SeriesOf(owner).DailyTotalWorkItemAge(owner, window);

        private IReportMetricSeries SeriesOf(ReportOwner owner)
            => owner.Kind == ReportOwnerKind.Portfolio ? portfolioSeries : teamSeries;
    }
}
