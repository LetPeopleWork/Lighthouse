using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    public class DeliveryMetricSnapshot : IEntity
    {
        public int Id { get; set; }

        public int DeliveryId { get; set; }

        /// <summary>
        /// Legacy instant column, still written on every upsert for the expand phase and dropped in
        /// the first contract release after <see cref="RecordedDay"/> ships. Never read it for day
        /// identity - use <see cref="RecordedDay"/>.
        /// </summary>
        public DateTime RecordedAt { get; set; }

        /// <summary>
        /// The persisted day key. <see cref="DateOnly"/> on purpose: the global
        /// <c>Properties&lt;DateTime&gt;()</c> converter cannot reach it, so no zone conversion can
        /// shift it on a write or on a query parameter (Bug #5567).
        /// </summary>
        public DateOnly RecordedDay { get; set; }

        public DateTime? TargetDateAtSnapshot { get; set; }

        public int TotalWork { get; set; }

        public int DoneWork { get; set; }

        public int RemainingWork { get; set; }

        public int? EstimatedItemCount { get; set; }

        public int? ForecastHowMany { get; set; }

        public double? LikelihoodPercentage { get; set; }

        public string? WhenDistributionJson { get; set; }

        public string? FeatureBreakdownJson { get; set; }
    }
}
