using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;

namespace Lighthouse.Backend.Modules.FlowMetrics.Application
{
    /// <summary>
    /// How many items a throughput is likely to finish in a number of days.
    /// </summary>
    public interface IHowManyForecaster
    {
        HowManyForecast HowMany(RunChartData throughput, int days);
    }
}
