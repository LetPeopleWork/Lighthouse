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
    }
}