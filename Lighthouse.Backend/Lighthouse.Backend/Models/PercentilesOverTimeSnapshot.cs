using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    public class PercentilesOverTimeSnapshot : IEntity
    {
        /// <summary>
        /// Horizon sentinel for metric families that have no horizon dimension (Work Item Age is
        /// measured as-of-today). Stored instead of NULL: SQL treats NULLs as distinct, so a NULL
        /// horizon defeats the unique index and EF translates <c>Horizon == value</c> to
        /// <c>Horizon = @p</c>, which never matches NULL — the upsert would INSERT a duplicate
        /// row every refresh instead of updating today's row.
        /// </summary>
        public const int NoHorizon = 0;

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
