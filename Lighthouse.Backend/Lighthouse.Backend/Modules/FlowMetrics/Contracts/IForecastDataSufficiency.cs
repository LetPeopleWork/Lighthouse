using Lighthouse.Backend.Models.Metrics;

namespace Lighthouse.Backend.Modules.FlowMetrics.Contracts
{
    /// <summary>
    /// Whether a throughput holds enough data to forecast with.
    /// </summary>
    public interface IForecastDataSufficiency
    {
        bool HasEnoughData(RunChartData throughput);
    }
}
