using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    public class PercentilesOverTimeSnapshot : IEntity
    {
        public int Id { get; set; }

        public int OwnerId { get; set; }

        public OwnerType OwnerType { get; set; }

        public DateOnly RecordedAt { get; set; }

        public MetricType MetricType { get; set; }

        public int? Horizon { get; set; }

        public int P50 { get; set; }

        public int P70 { get; set; }

        public int P85 { get; set; }

        public int P95 { get; set; }
    }
}
