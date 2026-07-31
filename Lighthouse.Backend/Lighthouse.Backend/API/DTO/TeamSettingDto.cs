using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow;
using System.Text.Json.Serialization;

namespace Lighthouse.Backend.API.DTO
{
    public class TeamSettingDto : SettingsOwnerDtoBase
    {
        public TeamSettingDto() : base()
        {
        }

        public TeamSettingDto(Team team, DateOnly today) : base(team)
        {
            ThroughputHistory = team.ThroughputHistory;
            UseFixedDatesForThroughput = team.UseFixedDatesForThroughput;

            var throughputSettings = team.GetThroughputSettings(today);
            ThroughputHistoryStartDate = throughputSettings.StartDate;
            ThroughputHistoryEndDate = throughputSettings.EndDate;

            FeatureWIP = team.FeatureWIP;
            AutomaticallyAdjustFeatureWIP = team.AutomaticallyAdjustFeatureWIP;
            DoneItemsCutoffDays = team.DoneItemsCutoffDays;
            ForecastFilterRuleSetJson = team.ForecastFilterRuleSetJson;
            ConcurrencyToken = team.ConcurrencyToken;

            if (team.WorkTrackingSystemConnection != null)
            {
                DataRetrievalSchema = DataRetrievalSchemaDto.ForTeam(
                    team.WorkTrackingSystemConnection.WorkTrackingSystem,
                    WorkItemTableOf(team.WorkTrackingSystemConnection));
            }
        }

        // Read here rather than looked up inside the schema factory, so the factory stays a pure
        // function of two scalars and every caller is forced by the compiler to answer (ADR-123 D6).
        private static string WorkItemTableOf(WorkTrackingSystemConnection connection)
        {
            return connection.Options
                .Find(option => option.Key == ServiceNowWorkTrackingOptionNames.WorkItemTable)?.Value
                ?? string.Empty;
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

        public int DoneItemsCutoffDays { get; set; } = 365;

        public string? ForecastFilterRuleSetJson { get; set; }
    }
}
