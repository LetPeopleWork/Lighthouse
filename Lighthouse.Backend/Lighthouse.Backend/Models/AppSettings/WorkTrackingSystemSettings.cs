using System.Text.Json.Serialization;

namespace Lighthouse.Backend.Models.AppSettings
{
    public class WorkTrackingSystemSettings
    {
        public bool OverrideRequestTimeout { get; set; } = false;

        [JsonRequired]
        public int RequestTimeoutInSeconds { get; set; }
    }
}
