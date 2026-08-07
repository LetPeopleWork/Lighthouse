using Lighthouse.Backend.Models;
using System.Text.Json.Serialization;

namespace Lighthouse.Backend.API.DTO
{
    /// <summary>Who owns the order on this instance (ADR-134).</summary>
    public class FeatureOrderingDto
    {
        [JsonRequired]
        public FeatureOrderingPolicy Policy { get; set; }
    }
}
