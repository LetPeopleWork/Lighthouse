using Lighthouse.Backend.Models.Reports;

namespace Lighthouse.Backend.Services.Interfaces.Reports
{
    /// <summary>The owner's metric series as the Metrics tab computes them, over inclusive instance days.</summary>
    public interface IReportMetricSeries
    {
        CycleTimePercentiles CycleTimePercentiles(ReportOwner owner, ReportWindow window);

        /// <summary>The Work Items finished on each day of the window, as the Metrics tab's throughput run chart counts them.</summary>
        IReadOnlyList<int> DailyThroughput(ReportOwner owner, ReportWindow window);

        /// <summary>The Work Items in progress on each day of the window, as the Metrics tab's WIP run chart counts them.</summary>
        IReadOnlyList<int> DailyWorkInProgress(ReportOwner owner, ReportWindow window);

        /// <summary>The summed age of the Work Items in progress on each day of the window, as the Metrics tab's Total Work Item Age reads it.</summary>
        IReadOnlyList<int> DailyTotalWorkItemAge(ReportOwner owner, ReportWindow window);
    }

    /// <summary>The Metrics tab's cycle-time percentiles, and how many finished items with a cycle time they rest on.</summary>
    public sealed record CycleTimePercentiles(int P50, int P70, int P85, int FinishedItems);
}
