using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;

namespace Lighthouse.Backend.API.DTO
{
    public class WhenForecastDto
    {
        public WhenForecastDto()
        {
        }

        /// <param name="forecast">
        /// A completion distribution or a start one. Both are read the same way - a day off the
        /// histogram, projected over working days - which is why the day-to-date translation stays in one
        /// place. Only a completion carries the throughput-filter notice, so only a completion sets it.
        /// </param>
        public WhenForecastDto(ForecastBase forecast, int probability, DateOnly today, IReadOnlyList<BlackoutPeriod> blackoutPeriods)
        {
            var percentile = forecast.GetPercentile(probability);

            Probability = probability;
            BeyondHorizon = percentile.IsBeyondHorizon;
            HorizonYears = ForecastHorizon.Years;

            ExpectedDate = percentile.ExpectedDateFrom(today, blackoutPeriods);

            if (forecast is WhenForecast completion)
            {
                FilterApplied = completion.FilterApplied;
                ExcludedSummary = completion.ExcludedSummary;
            }
        }

        public int Probability { get; set; }

        public DateTime ExpectedDate { get; set; }

        public bool BeyondHorizon { get; set; }

        public int HorizonYears { get; set; }

        public bool FilterApplied { get; set; }

        public string? ExcludedSummary { get; set; }
    }
}
