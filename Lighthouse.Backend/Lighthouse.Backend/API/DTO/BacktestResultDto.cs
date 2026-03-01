namespace Lighthouse.Backend.API.DTO
{
    public class BacktestResultDto
    {
        public BacktestResultDto(DateOnly startDate, DateOnly endDate, DateOnly historicalStartDate, DateOnly historicalEndDate)
        {
            StartDate = startDate;
            EndDate = endDate;
            HistoricalStartDate = historicalStartDate;
            HistoricalEndDate = historicalEndDate;
        }

        public DateOnly StartDate { get; }
        
        public DateOnly EndDate { get; }
        
        public DateOnly HistoricalStartDate { get; }

        public DateOnly HistoricalEndDate { get; }

        public List<ForecastDto> Percentiles { get; } = new List<ForecastDto>();

        public int ActualThroughput { get; set; }
    }
}
