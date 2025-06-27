using Lighthouse.Backend.Models;
using System.Text.Json.Serialization;

namespace Lighthouse.Backend.API.DTO
{
    public class TeamSettingDto : SettingsOwnerDtoBase
    {
        public TeamSettingDto() : base()
        {            
        }

        public TeamSettingDto(Team team) : base(team)
        {
            ThroughputHistory = team.ThroughputHistory;
            UseFixedDatesForThroughput = team.UseFixedDatesForThroughput;

            var throughputSettings = team.GetThroughputSettings();
            ThroughputHistoryStartDate = throughputSettings.StartDate;
            ThroughputHistoryEndDate = throughputSettings.EndDate;

            FeatureWIP = team.FeatureWIP;
            AutomaticallyAdjustFeatureWIP = team.AutomaticallyAdjustFeatureWIP;
        }

        [JsonRequired]
        public int ThroughputHistory { get; set; }

        [JsonRequired]
        public bool UseFixedDatesForThroughput { get; set; }

        public DateTime? ThroughputHistoryStartDate { get; set; }

        public DateTime? ThroughputHistoryEndDate { get; set; }

        [JsonRequired]
        public int FeatureWIP { get; set; }

        [JsonRequired]
        public bool AutomaticallyAdjustFeatureWIP { get; set; }
    }
}
