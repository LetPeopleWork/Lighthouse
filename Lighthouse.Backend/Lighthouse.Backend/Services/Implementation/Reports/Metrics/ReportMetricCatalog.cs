using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Interfaces.Reports;

namespace Lighthouse.Backend.Services.Implementation.Reports.Metrics
{
    /// <summary>Every metric this version can put in a report, in the order a report shows them.</summary>
    public class ReportMetricCatalog(IEnumerable<IReportMetric> metrics)
    {
        private readonly List<IReportMetric> metrics = [.. metrics];

        public IReadOnlyList<IReportMetric> For(ReportOwnerKind ownerKind) => [.. metrics.Where(metric => metric.AppliesTo(ownerKind))];

        public IReportMetric? Find(string key) => metrics.Find(metric => metric.Key == key);
    }
}
