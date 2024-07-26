using Lighthouse.Backend.Models.AppSettings;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface IAppSettingService
    {
        RefreshSettings GetThroughputRefreshSettings();

        void UpdateThroughputRefreshSettings(RefreshSettings refreshSettings);
        
        RefreshSettings GetFeaturRefreshSettings();

        void UpdateFeatureRefreshSettings(RefreshSettings refreshSettings);

        RefreshSettings GetForecastRefreshSettings();

        void UpdateForecastRefreshSettings(RefreshSettings refreshSettings);

    }
}
