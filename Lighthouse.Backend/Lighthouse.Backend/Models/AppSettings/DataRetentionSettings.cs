using System.Text.Json.Serialization;

namespace Lighthouse.Backend.Models.AppSettings
{
    public class DataRetentionSettings
    {
        [JsonRequired]
        public int MaxStorageTimeInDays {  get; set; }
    }
}
