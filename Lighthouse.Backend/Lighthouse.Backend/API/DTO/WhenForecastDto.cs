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
        }

        public int Probability { get; set; }

        public DateTime ExpectedDate { get; set; }

        private static DateTime GetFutureDate(int daysInFuture)
        {
            return DateTime.UtcNow.Date.AddDays(daysInFuture);
        }
    }
}
