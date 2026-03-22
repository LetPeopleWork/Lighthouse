using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    public class BlackoutPeriod : IEntity
    {
        public int Id { get; set; }

        public DateOnly Start { get; set; }

        public DateOnly End { get; set; }

        public string Description { get; set; } = string.Empty;
    }
}
