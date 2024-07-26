namespace Lighthouse.Backend.Models.AppSettings
{
    public static class AppSettingKeys
    {
        public static string ThroughputRefreshInterval => "PeriodicRefresh:Throughput:Interval";
        
        public static string ThroughputRefreshAfter => "PeriodicRefresh:Throughput:RefreshAfter";

        public static string ThroughputRefreshStartDelay => "PeriodicRefresh:Throughput:StartDelay";

        public static string FeaturesRefreshInterval => "PeriodicRefresh:Features:Interval";
        
        public static string FeaturesRefreshAfter => "PeriodicRefresh:Features:RefreshAfter";

        public static string FeaturesRefreshStartDelay => "PeriodicRefresh:Features:StartDelay";

        public static string ForecastRefreshInterval => "PeriodicRefresh:Forecasts:Interval";
        
        public static string ForecastRefreshAfter => "PeriodicRefresh:Forecasts:RefreshAfter";

        public static string ForecastRefreshStartDelay => "PeriodicRefresh:Forecasts:StartDelay";
    }
}
