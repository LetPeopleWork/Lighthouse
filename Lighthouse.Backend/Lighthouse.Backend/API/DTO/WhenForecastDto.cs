using Lighthouse.Backend.Models.Forecast;

namespace Lighthouse.Backend.API.DTO
{
    public class WhenForecastDto
    {
        public WhenForecastDto()
        {
        }

        public WhenForecastDto(WhenForecast forecast, int probability)
        {
            Probability = probability;
            ExpectedDate = GetFutureDate(forecast.GetProbability(probability));
            FilterApplied = forecast.FilterApplied;
            ExcludedSummary = forecast.ExcludedSummary;
        }

        public int Probability { get; set; }

        public DateTime ExpectedDate { get; set; }

        public bool FilterApplied { get; set; }

        public string? ExcludedSummary { get; set; }

        private static DateTime GetFutureDate(int daysInFuture)
        {
            return DateTime.UtcNow.Date.AddDays(daysInFuture);
        }
    }
}
