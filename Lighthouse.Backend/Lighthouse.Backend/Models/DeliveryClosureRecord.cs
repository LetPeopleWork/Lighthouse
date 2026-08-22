namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// What a Delivery read as on the day it was archived, kept so the record cannot rewrite itself
    /// once the Portfolio behind it moves on. Keyed by DeliveryId alone: a Delivery has one closure,
    /// and a key that also carried a day would let a second one exist.
    /// </summary>
    public class DeliveryClosureRecord
    {
        public int DeliveryId { get; set; }

        public DateTime ArchivedOn { get; set; }

        public DateTime? TargetDateAtClosure { get; set; }

        public int TotalWork { get; set; }

        public int DoneWork { get; set; }

        public int RemainingWork { get; set; }

        public int? EstimatedItemCount { get; set; }

        public int? ForecastHowMany { get; set; }

        public double? LikelihoodPercentage { get; set; }

        public string? WhenDistributionJson { get; set; }

        public string? FeatureBreakdownJson { get; set; }

        /// <summary>
        /// A Delivery archived while it could not be forecast has to keep saying so. Recomputing this
        /// from the pinned numbers alone would default it to true, and the archived read would claim a
        /// forecast the closure day never had.
        /// </summary>
        public bool HasSufficientData { get; set; }

        public string? TeamsWithoutForecastJson { get; set; }

        public DeliverySelectionMode SelectionMode { get; set; }

        public string? RuleDefinitionJson { get; set; }

        public int? RuleSchemaVersion { get; set; }
    }
}
