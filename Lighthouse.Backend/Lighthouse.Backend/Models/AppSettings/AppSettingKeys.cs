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
    }
}
