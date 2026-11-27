using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Modules.FlowMetrics.Contracts;
using Lighthouse.Backend.Modules.Forecasting.Contracts;
using Lighthouse.Backend.Modules.Forecasting.Domain;

namespace Lighthouse.Backend.Modules.Forecasting.Application
{
    /// <summary>
    /// Answers the questions Flow metrics asks Forecasting, with the answers Forecasting already owns.
    /// </summary>
    /// <remarks>
    /// The forecast service is looked up on the first How-many question, not taken in the constructor: the forecast
    /// service needs the team metrics service, which needs this answer, so taking it eagerly would make building
    /// either of them recurse until the process runs out of stack.
    /// </remarks>
    public sealed class ForecastingAnswersToFlowMetrics(IServiceProvider serviceProvider) : IForecastDataSufficiency, IHowManyForecaster
    {
        private IForecastService ForecastService => field ??= serviceProvider.GetRequiredService<IForecastService>();

        public bool HasEnoughData(RunChartData throughput) => ForecastDataSufficiencyPolicy.HasEnoughData(throughput);

        public HowManyForecast HowMany(RunChartData throughput, int days) => ForecastService.HowMany(throughput, days);
    }
}
