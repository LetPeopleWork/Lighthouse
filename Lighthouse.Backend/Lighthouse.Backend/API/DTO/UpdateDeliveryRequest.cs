using System.Text.Json.Serialization;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliveryRules;

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

        public List<DeliveryRuleCondition>? Rules { get; set; }
    }
}