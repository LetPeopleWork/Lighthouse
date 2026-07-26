using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// One day's natural process limits for a single metric family of a single owner.
    /// The natural key is (OwnerId, OwnerType, MetricType, RecordedAt) — four parts, because
    /// natural process limits have no horizon dimension, so there is no horizon sentinel here.
    /// The limit triple is <c>int</c> to match the compute path
    /// (<see cref="Metrics.ProcessBehaviourChart"/> exposes int limits) — no invented precision.
    /// </summary>
    public class ProcessBehaviorSnapshot : IEntity
    {
        public int Id { get; set; }

        public int OwnerId { get; set; }

        public OwnerType OwnerType { get; set; }

        public DateOnly RecordedAt { get; set; }

        public ProcessBehaviorMetricType MetricType { get; set; }

        public int Unpl { get; set; }

        public int Average { get; set; }

        public int Lnpl { get; set; }
    }
}
