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

        public const string TeamSettingToDoStates = "DefaultTeamSetting:ToDoStates";

        public const string TeamSettingDoingStates = "DefaultTeamSetting:DoingStates";

        public const string TeamSettingDoneStates = "DefaultTeamSetting:DoneStates";

        public const string TeamSettingRelationCustomField = "DefaultTeamSetting:RelationCustomField";

        public const string ProjectSettingName = "DefaultProjectSetting:Name";

        public const string ProjectSettingWorkItemQuery = "DefaultProjectSetting:WorkItemQuery";

        public const string ProjectSettingWorkItemTypes = "DefaultProjectSetting:WorkItemTypes";

        public const string ProjectSettingToDoStates = "DefaultProjectSetting:ToDoStates";

        public const string ProjectSettingDoingStates = "DefaultProjectSetting:DoingStates";

        public const string ProjectSettingDoneStates = "DefaultProjectSetting:DoneStates";

        public const string ProjectSettingOverrideRealChildCountStates = "DefaultProjectSetting:OverrideRealChildCountStates";

        public const string ProjectSettingUnparentedWorkItemQuery = "DefaultProjectSetting:UnparentedWorkItemQuery";

        public const string ProjectSettingUsePercentileToCalculateDefaultAmountOfWorkItems = "DefaultProjectSetting:UsePercentileToCalculateDefaultAmountOfWorkItems";

        public const string ProjectSettingDefaultAmountOfWorkItemsPerFeature = "DefaultProjectSetting:DefaultAmountOfWorkItemsPerFeature";

        public const string ProjectSettingDefaultWorkItemPercentile = "DefaultProjectSetting:DefaultWorkItemPercentile";

        public const string ProjectSettingHistoricalFeaturesWorkItemQuery = "DefaultProjectSetting:HistoricalFeaturesWorkItemQuery";

        public const string ProjectSettingSizeEstimateField = "DefaultProjectSetting:SizeEstimateField";

        public const string CleanUpDataHistorySettingsMaxStorageTimeInDays = "CleanUpDataHistorySettings:MaxStorageTimeInDays";
    }
}
