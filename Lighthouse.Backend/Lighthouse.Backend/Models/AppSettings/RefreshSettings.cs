using System.Text.Json.Serialization;

namespace Lighthouse.Backend.Models.AppSettings
{
    public class RefreshSettings
    {
        [JsonRequired]
        public int Interval { get; set; }

        [JsonRequired]
        public int RefreshAfter { get; set; }

        [JsonRequired]
        public int StartDelay { get; set; } 
    }
}
