namespace Lighthouse.Backend.API.DTO
{
    public class ManualForecastDto(int remainingItems, DateTime? targetDate)
    {
        public int RemainingItems { get; } = remainingItems;

        public DateTime? TargetDate { get; } = targetDate;

        public double Likelihood { get; set; }

        public List<WhenForecastDto> WhenForecasts { get; } = [];

        public List<ForecastDto> HowManyForecasts { get; } = [];

        public bool FilterApplied { get; set; }

        public string? ExcludedSummary { get; set; }

        public bool HasSufficientData { get; set; } = true;
    }
}
