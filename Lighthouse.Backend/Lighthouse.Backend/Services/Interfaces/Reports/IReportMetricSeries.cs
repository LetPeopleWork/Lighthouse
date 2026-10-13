using Lighthouse.Backend.Models.Reports;

namespace Lighthouse.Backend.Services.Interfaces.Reports
{
    /// <summary>The owner's metric series as the Metrics tab computes them, over inclusive instance days.</summary>
    public interface IReportMetricSeries
    {
        CycleTimePercentiles CycleTimePercentiles(ReportOwner owner, ReportWindow window);
    }

    /// <summary>The Metrics tab's cycle-time percentiles, and how many finished items with a cycle time they rest on.</summary>
    public sealed record CycleTimePercentiles(int P50, int P70, int P85, int FinishedItems);
}
