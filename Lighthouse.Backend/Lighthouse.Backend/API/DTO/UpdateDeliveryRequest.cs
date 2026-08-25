using System.Text.Json.Serialization;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WorkItemRules;

namespace Lighthouse.Backend.API.DTO
{
    public class UpdateDeliveryRequest
    {
        [JsonRequired]
        public string Name { get; set; } = string.Empty;

        [JsonRequired]
        public DateTime Date { get; set; }

        [JsonRequired]
        public List<int> FeatureIds { get; set; } = [];

        public DeliverySelectionMode SelectionMode { get; set; } = DeliverySelectionMode.Manual;

        public List<WorkItemRuleCondition>? Rules { get; set; }

        public string? Mode { get; set; }

        public string? SourceKey { get; set; }

        public string? SourceReference { get; set; }

        /// <summary>
        /// Whether the Delivery's forecast is broadcast onto the source it follows. Read only on the
        /// source-bound path, because it is a property of the binding and there is nowhere to broadcast
        /// to without one.
        ///
        /// Absent leaves it as it was, which is deliberately unlike every other field here: this request
        /// carries the whole Delivery, so what it leaves out is normally not what the Delivery keeps. The
        /// switch is the one field nobody sees being changed - a Delivery quietly stops broadcasting and
        /// the only symptom is a Release in somebody else's tracker that stopped being updated, with
        /// nothing anywhere saying why. On a Delivery being created there is nothing to leave as it was,
        /// so absent means off.
        /// </summary>
        public bool? PublishForecastToSource { get; set; }

        public Guid? ConcurrencyToken { get; set; }
    }
}