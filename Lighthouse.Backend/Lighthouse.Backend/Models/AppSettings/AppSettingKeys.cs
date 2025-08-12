namespace Lighthouse.Backend.Models.AppSettings
{
    public static class AppSettingKeys
    {
        public const string TeamDataRefreshInterval = "PeriodicRefresh:Throughput:Interval";
        
        public const string TeamDataRefreshAfter = "PeriodicRefresh:Throughput:RefreshAfter";

        public const string TeamDataRefreshStartDelay = "PeriodicRefresh:Throughput:StartDelay";

        public const string FeaturesRefreshInterval = "PeriodicRefresh:Features:Interval";
        
        public const string FeaturesRefreshAfter = "PeriodicRefresh:Features:RefreshAfter";

        public const string FeaturesRefreshStartDelay = "PeriodicRefresh:Features:StartDelay";

        public const string TeamSettingName = "DefaultTeamSetting:Name";

        public const string TeamSettingHistory = "DefaultTeamSetting:History";

        public const string TeamSettingFeatureWIP = "DefaultTeamSetting:FeatureWIP";

        public const string TeamSettingWorkItemQuery = "DefaultTeamSetting:WorkItemQuery";

        public const string TeamSettingWorkItemTypes = "DefaultTeamSetting:WorkItemTypes";

        public const string TeamSettingToDoStates = "DefaultTeamSetting:ToDoStates";

        public const string TeamSettingDoingStates = "DefaultTeamSetting:DoingStates";

        public const string TeamSettingDoneStates = "DefaultTeamSetting:DoneStates";

        public const string TeamSettingTags = "DefaultTeamSetting:Tags";

        public const string TeamSettingSLEProbability = "DefaultTeamSetting:SLEProbability";

        public const string TeamSettingSLERange = "DefaultTeamSetting:SLERange";

        public const string TeamSettingParentOverrideField = "DefaultTeamSetting:RelationCustomField";

        public const string TeamSettingAutomaticallyAdjustFeatureWIP = "DefaultTeamSetting:AutomaticallyAdjustFeatureWIP";

        public const string TeamSettingBlockedStates = "DefaultTeamSetting:BlockedStates";

        public const string TeamSettingBlockedTags = "DefaultTeamSetting:BlockedTags";

        public const string ProjectSettingName = "DefaultProjectSetting:Name";

        public const string ProjectSettingWorkItemQuery = "DefaultProjectSetting:WorkItemQuery";

        public const string ProjectSettingWorkItemTypes = "DefaultProjectSetting:WorkItemTypes";

        public const string ProjectSettingToDoStates = "DefaultProjectSetting:ToDoStates";

        public const string ProjectSettingDoingStates = "DefaultProjectSetting:DoingStates";

        public const string ProjectSettingDoneStates = "DefaultProjectSetting:DoneStates";

        public const string ProjectSettingTags = "DefaultProjectSetting:Tags";

        public const string ProjectSettingOverrideRealChildCountStates = "DefaultProjectSetting:OverrideRealChildCountStates";

        public const string ProjectSettingUnparentedWorkItemQuery = "DefaultProjectSetting:UnparentedWorkItemQuery";

        public const string ProjectSettingUsePercentileToCalculateDefaultAmountOfWorkItems = "DefaultProjectSetting:UsePercentileToCalculateDefaultAmountOfWorkItems";

        public const string ProjectSettingDefaultAmountOfWorkItemsPerFeature = "DefaultProjectSetting:DefaultAmountOfWorkItemsPerFeature";

        public const string ProjectSettingDefaultWorkItemPercentile = "DefaultProjectSetting:DefaultWorkItemPercentile";

        public const string ProjectSettingHistoricalFeaturesWorkItemQuery = "DefaultProjectSetting:HistoricalFeaturesWorkItemQuery";

        public const string ProjectSettingPercentileHistoryInDays = "DefaultProjectSetting:PercentileHistoryInDays";

        public const string ProjectSettingSizeEstimateField = "DefaultProjectSetting:SizeEstimateField";

        public const string ProjectSettingsFeatureOwnerField = "DefaultProjectSetting:FeatureOwnerField";

        public const string ProjectSettingSLEProbability = "DefaultProjectSetting:SLEProbability";

        public const string ProjectSettingSLERange = "DefaultProjectSetting:SLERange";

        public const string ProjectSettingParentOverrideField = "DefaultProjectSetting:ParentOverrideField";

        public const string ProjectSettingBlockedStates = "DefaultProjectSetting:BlockedStates";

        public const string ProjectSettingBlockedTags = "DefaultProjectSetting:BlockedTags";

        public const string CleanUpDataHistorySettingsMaxStorageTimeInDays = "CleanUpDataHistorySettings:MaxStorageTimeInDays";

        public const string WorkTrackingSystemSettingsOverrideRequestTimeout = "WorkTrackingSystemSettings:OverrideRequestTimeout";

        public const string WorkTrackingSystemSettingsRequestTimeoutInSeconds = "WorkTrackingSystemSettings:RequestTimeoutInSeconds";
    }
}
