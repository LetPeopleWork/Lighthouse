using System.Text.Json.Serialization;

namespace Lighthouse.Backend.API.DTO
{
    public class UpdateDeliveryRequest
    {
        public UpdateDeliveryRequest()
        {
            FeatureIds = [];
        }

        [JsonRequired]
        public string Name { get; set; } = string.Empty;

        [JsonRequired]
        public DateTime Date { get; set; }

        [JsonRequired]
        public List<int> FeatureIds { get; set; }
    }
}