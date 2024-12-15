using System.Text.Json.Serialization;

namespace Lighthouse.Backend.Models.AppSettings
{
    public class CleanUpDataHistorySettings
    {
        [JsonRequired]
        public int MaxStorageTimeInDays {  get; set; }
    }
}
