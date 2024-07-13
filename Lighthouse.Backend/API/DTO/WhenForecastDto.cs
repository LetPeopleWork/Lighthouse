using Lighthouse.Backend.Models.Forecast;

namespace Lighthouse.Backend.API.DTO
{
    public class WhenForecastDto
    {
        public WhenForecastDto(WhenForecast forecast, int probability)
        {
            Probability = probability;
            ExpectedDate = GetFutureDate(forecast.GetProbability(probability));
        }

        public int Probability { get; }

        public DateTime ExpectedDate { get; }

        private DateTime GetFutureDate(int daysInFuture)
        {
            return DateTime.Today.AddDays(daysInFuture);
        }
    }
}
