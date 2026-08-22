using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WorkItemRules;

namespace Lighthouse.Backend.API.DTO.Archived
{
    /// <summary>
    /// A retired Delivery as it goes over the wire. It is a different shape from a live one on
    /// purpose: every number here was written down once and is never worked out again, so a reader
    /// six months from now sees what the Delivery said on the day it closed rather than what its
    /// Features happen to say today.
    /// </summary>
    public class ArchivedDeliveryDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public DateTime Date { get; set; }

        public int PortfolioId { get; set; }

        public DateTime ArchivedOn { get; set; }

        public double Progress { get; set; }

        public int TotalWork { get; set; }

        public int DoneWork { get; set; }

        public int RemainingWork { get; set; }

        public double? LikelihoodPercentage { get; set; }

        public bool HasSufficientData { get; set; } = true;

        public List<string> TeamsWithoutForecast { get; set; } = [];

        /// <summary>
        /// The Feature rows travel here in full rather than as ids to look up. An id is an invitation
        /// to go and fetch the Feature as it stands today, which is the one thing a record of what a
        /// Delivery looked like at closing must not do - so there are no ids to fetch by.
        /// </summary>
        public List<DeliveryFeatureMetricDto> FeatureBreakdown { get; set; } = [];

        public List<WhenDistributionPointDto> WhenDistribution { get; set; } = [];

        public DeliverySelectionMode SelectionMode { get; set; }

        public List<WorkItemRuleCondition> Rules { get; set; } = [];

        public string Mode { get; set; } = WorkItemRuleSet.ModeAnd;

        public int MetricSnapshotCount { get; set; }

        public Guid ConcurrencyToken { get; set; }
    }
}
