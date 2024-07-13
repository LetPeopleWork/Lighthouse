namespace Lighthouse.Backend.API.DTO
{
    public class ManualForecastDto
    {
        public double Likelihood { get; set; }

        public List<WhenForecastDto> WhenForecasts { get; } = new List<WhenForecastDto>();

        public List<HowManyForecastDto> HowManyForecasts { get; } = new List<HowManyForecastDto>();
    }
}
