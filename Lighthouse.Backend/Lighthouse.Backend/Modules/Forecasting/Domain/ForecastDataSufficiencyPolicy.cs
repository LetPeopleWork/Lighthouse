using Lighthouse.Backend.Models.Metrics;

namespace Lighthouse.Backend.Modules.Forecasting.Domain
{
    public static class ForecastDataSufficiencyPolicy
    {
        public const int MinimumActiveDays = 5;

        public static bool HasEnoughData(RunChartData throughput) => throughput.DaysWithThroughput >= MinimumActiveDays;
    }
}
