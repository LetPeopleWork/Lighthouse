using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    public class DeliveryMetricSnapshot : IEntity
    {
        public int Id { get; set; }

        public int DeliveryId { get; set; }

        /// <summary>
        /// Legacy instant column, kept for the duration of the expand phase so a rollback to a
        /// release older than the one introducing <see cref="RecordedDay"/> still reads correct
        /// data. It is still written on every upsert. Expected to be dropped in the first
        /// contract release AFTER the release that ships <see cref="RecordedDay"/> - i.e. once no
        /// supported version reads it any more. Do not read it for day identity; use
        /// <see cref="RecordedDay"/>.
        /// </summary>
        public DateTime RecordedAt { get; set; }

        /// <summary>
        /// The calendar day this snapshot belongs to, and the persisted day key.
        ///
        /// Typed <see cref="DateOnly"/> on purpose: the global convention in
        /// <c>LighthouseAppContext.ConfigureConventions</c> attaches
        /// <c>UtcDateTimeConverter</c> to <c>Properties&lt;DateTime&gt;()</c> only, so a
        /// <see cref="DateOnly"/> is structurally out of the converter's reach and cannot be
        /// shifted by a zone conversion on write or on a query parameter (Bug #5567, R1).
        ///
        /// Matches the shape <see cref="BlockedCountSnapshot"/> and
        /// <see cref="PercentilesOverTimeSnapshot"/> already use.
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
