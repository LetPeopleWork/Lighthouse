using System.Text.Json.Serialization;

namespace Lighthouse.Backend.API.DTO
{
    public class BacktestInputDto
    {
        [JsonRequired]
        public DateOnly StartDate { get; set; }

        [JsonRequired]
        public DateOnly EndDate { get; set; }

        [JsonRequired]
        public DateOnly HistoricalStartDate { get; set; }

        [JsonRequired]
        public DateOnly HistoricalEndDate { get; set; }
    }
}
