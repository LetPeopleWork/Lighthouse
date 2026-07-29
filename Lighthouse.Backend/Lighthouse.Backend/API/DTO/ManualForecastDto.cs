namespace Lighthouse.Backend.API.DTO
{
    public class ManualForecastDto(int remainingItems, DateTime? targetDate)
    {
        public int RemainingItems { get; } = remainingItems;

        public DateTime? TargetDate { get; } = targetDate;

        // Null when the forecast has no trials to read a likelihood off - 0 would state a certainty
        // of failure the data cannot support (Bug #5586, same carrier as ADR-112).
        public double? Likelihood { get; set; }

        public List<WhenForecastDto> WhenForecasts { get; } = [];

        public List<ForecastDto> HowManyForecasts { get; } = [];

        public bool FilterApplied { get; set; }

        public string? ExcludedSummary { get; set; }

        public bool HasSufficientData { get; set; } = true;
    }
}
