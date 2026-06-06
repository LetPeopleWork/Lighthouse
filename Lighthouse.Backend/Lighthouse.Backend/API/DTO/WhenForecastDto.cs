using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation;

namespace Lighthouse.Backend.API.DTO
{
    public class WhenForecastDto
    {
        public WhenForecastDto()
        {
        }

        public WhenForecastDto(WhenForecast forecast, int probability, IReadOnlyList<BlackoutPeriod> blackoutPeriods)
        {
            Probability = probability;
            ExpectedDate = blackoutPeriods.ProjectWorkingDays(DateTime.UtcNow.Date, forecast.GetProbability(probability));
            FilterApplied = forecast.FilterApplied;
            ExcludedSummary = forecast.ExcludedSummary;
        }

        public int Probability { get; set; }

        public DateTime ExpectedDate { get; set; }

        public bool FilterApplied { get; set; }

        public string? ExcludedSummary { get; set; }
    }
}
