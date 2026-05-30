using System.Text.Json.Serialization;

namespace Lighthouse.Backend.Models
{
    public class BlackoutPeriodDto
    {
        public int? Id { get; set; }

        [JsonRequired]
        public DateOnly Start { get; set; }

        [JsonRequired]
        public DateOnly End { get; set; }

        public string Description { get; set; } = string.Empty;
    }
}
