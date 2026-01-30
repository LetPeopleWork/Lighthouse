using System.Text.Json.Serialization;

namespace Lighthouse.Backend.API.DTO
{
    public class BacktestInputDto
    {
        [JsonRequired]
        public DateOnly StartDate { get; set; }

        [JsonRequired]
        public DateOnly EndDate { get; set; }

        public int HistoricalWindowDays { get; set; } = 30;
    }
}
