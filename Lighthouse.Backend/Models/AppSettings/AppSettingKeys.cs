namespace Lighthouse.Backend.Models.AppSettings
{
    public static class AppSettingKeys
    {
        public const string ThroughputRefreshInterval = "PeriodicRefresh:Throughput:Interval";
        
        public const string ThroughputRefreshAfter = "PeriodicRefresh:Throughput:RefreshAfter";

        public const string ThroughputRefreshStartDelay = "PeriodicRefresh:Throughput:StartDelay";

        public const string FeaturesRefreshInterval = "PeriodicRefresh:Features:Interval";
        
        public const string FeaturesRefreshAfter = "PeriodicRefresh:Features:RefreshAfter";

        public const string FeaturesRefreshStartDelay = "PeriodicRefresh:Features:StartDelay";

        public const string ForecastRefreshInterval = "PeriodicRefresh:Forecasts:Interval";
        
        public const string ForecastRefreshAfter = "PeriodicRefresh:Forecasts:RefreshAfter";

        public const string ForecastRefreshStartDelay = "PeriodicRefresh:Forecasts:StartDelay";

        public const string TeamSettingName = "DefaultTeamSetting:Name";

        public const string TeamSettingHistory = "DefaultTeamSetting:History";

        public const string TeamSettingFeatureWIP = "DefaultTeamSetting:FeatureWIP";

        public const string TeamSettingWorkItemQuery = "DefaultTeamSetting:WorkItemQuery";

        public const string TeamSettingWorkItemTypes = "DefaultTeamSetting:WorkItemTypes";

        public const string TeamSettingRelationCustomField = "DefaultTeamSetting:RelationCustomField";
    }
}
