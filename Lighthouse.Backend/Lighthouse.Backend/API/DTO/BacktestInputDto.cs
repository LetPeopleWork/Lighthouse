namespace Lighthouse.Backend.API.DTO
{
    public class BacktestInputDto
    {
        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public int HistoricalWindowDays { get; set; } = 30;
    }
}
