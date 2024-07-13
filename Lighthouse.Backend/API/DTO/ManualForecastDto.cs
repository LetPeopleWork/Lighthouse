namespace Lighthouse.Backend.API.DTO
{
    public class ManualForecastDto
    {
        public ManualForecastDto(int remainingItems, DateTime targetDate)
        {
            RemainingItems = remainingItems;
            TargetDate = targetDate;
        }

        public int RemainingItems { get; }

        public DateTime TargetDate { get; }

        public double Likelihood { get; set; }

        public List<WhenForecastDto> WhenForecasts { get; } = new List<WhenForecastDto>();

        public List<HowManyForecastDto> HowManyForecasts { get; } = new List<HowManyForecastDto>();
    }
}
