namespace Lighthouse.Backend.API.DTO
{
    public class BacktestResultDto
    {
        public BacktestResultDto(DateOnly startDate, DateOnly endDate, int historicalWindowDays)
        {
            StartDate = startDate;
            EndDate = endDate;
            HistoricalWindowDays = historicalWindowDays;
        }

        public DateOnly StartDate { get; }
        
        public DateOnly EndDate { get; }
        
        public int HistoricalWindowDays { get; }

        public List<ForecastDto> Percentiles { get; } = new List<ForecastDto>();

        public int ActualThroughput { get; set; }
    }
}
