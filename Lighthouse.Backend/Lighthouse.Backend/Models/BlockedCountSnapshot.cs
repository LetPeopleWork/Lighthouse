using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    public class BlockedCountSnapshot : IEntity
    {
        public int Id { get; set; }

        public int OwnerId { get; set; }

        public OwnerType OwnerType { get; set; }

        public DateOnly RecordedAt { get; set; }

        public int BlockedCount { get; set; }
    }
}
