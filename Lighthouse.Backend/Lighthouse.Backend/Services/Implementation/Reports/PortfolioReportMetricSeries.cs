using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using static Lighthouse.Backend.Services.Implementation.Reports.ReportSeriesReading;

namespace Lighthouse.Backend.Services.Implementation.Reports
{
    /// <summary>A Portfolio's series over its Features, read through the same service the Portfolio's Metrics tab reads, so a report and the tab never disagree.</summary>
    public class PortfolioReportMetricSeries(IRepository<Portfolio> portfolios, IPortfolioMetricsService metrics, ILighthouseClock clock) : IReportMetricSeries
    {
        public CycleTimePercentiles CycleTimePercentiles(ReportOwner owner, ReportWindow window)
            => OverWindow(owner, window, (portfolio, start, end) =>
            {
                var percentiles = metrics.GetCycleTimePercentilesForPortfolio(portfolio, start, end).ToDictionary(percentile => percentile.Percentile, percentile => percentile.Value);
                var finishedFeatures = metrics.GetCycleTimeDataForPortfolio(portfolio, start, end).Count(feature => feature.CycleTime(clock.Zone) > 0);

                return new CycleTimePercentiles(
                    percentiles.GetValueOrDefault(50), percentiles.GetValueOrDefault(70), percentiles.GetValueOrDefault(85), percentiles.GetValueOrDefault(95), finishedFeatures);
            });

        public IReadOnlyList<int> DailyThroughput(ReportOwner owner, ReportWindow window)
            => OverWindow(owner, window, (portfolio, start, end) => DailyCounts(metrics.GetThroughputForPortfolio(portfolio, start, end)));

        public IReadOnlyList<int> DailyWorkInProgress(ReportOwner owner, ReportWindow window)
            => OverWindow(owner, window, (portfolio, start, end) => DailyCounts(metrics.GetFeaturesInProgressOverTimeForPortfolio(portfolio, start, end)));

        public IReadOnlyList<int> DailyTotalWorkItemAge(ReportOwner owner, ReportWindow window)
            => OverWindow(owner, window, metrics.GetDailyTotalWorkItemAgeForPortfolio);

        private T OverWindow<T>(ReportOwner owner, ReportWindow window, Func<Portfolio, DateTime, DateTime, T> read)
        {
            var portfolio = PortfolioOf(owner);
            var (start, end) = DaysOf(window);

            return read(portfolio, start, end);
        }

        private Portfolio PortfolioOf(ReportOwner owner)
        {
            if (owner.Kind != ReportOwnerKind.Portfolio)
            {
                throw new ArgumentException($"Only a Portfolio's series can be read here, not a {owner.Kind}'s", nameof(owner));
            }

            return portfolios.GetById(owner.Id) ?? throw new ReportOwnerGoneException(owner);
        }
    }
}
