using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation
{
    public class AppSettingService : IAppSettingService
    {
        private readonly IRepository<AppSetting> repository;

        public AppSettingService(IRepository<AppSetting> repository)
        {
            this.repository = repository;
        }

        public RefreshSettings GetFeaturRefreshSettings()
        {
            return CreateRefreshSettings(
                AppSettingKeys.FeaturesRefreshInterval,
                AppSettingKeys.FeaturesRefreshAfter,
                AppSettingKeys.FeaturesRefreshStartDelay);
        }

        public RefreshSettings GetForecastRefreshSettings()
        {
            return CreateRefreshSettings(
               AppSettingKeys.ForecastRefreshInterval,
               AppSettingKeys.ForecastRefreshAfter,
               AppSettingKeys.ForecastRefreshStartDelay);
        }

        public RefreshSettings GetThroughputRefreshSettings()
        {
            return CreateRefreshSettings(
               AppSettingKeys.ThroughputRefreshInterval,
               AppSettingKeys.ThroughputRefreshAfter,
               AppSettingKeys.ThroughputRefreshStartDelay);
        }

        public void UpdateFeatureRefreshSettings(RefreshSettings refreshSettings)
        {
            UpdateRefreshSettings(refreshSettings, AppSettingKeys.FeaturesRefreshInterval, AppSettingKeys.FeaturesRefreshAfter, AppSettingKeys.FeaturesRefreshStartDelay);
        }

        public void UpdateForecastRefreshSettings(RefreshSettings refreshSettings)
        {
            UpdateRefreshSettings(refreshSettings, AppSettingKeys.ForecastRefreshInterval, AppSettingKeys.ForecastRefreshAfter, AppSettingKeys.ForecastRefreshStartDelay);
        }

        public void UpdateThroughputRefreshSettings(RefreshSettings refreshSettings)
        {
            UpdateRefreshSettings(refreshSettings, AppSettingKeys.ThroughputRefreshInterval, AppSettingKeys.ThroughputRefreshAfter, AppSettingKeys.ThroughputRefreshStartDelay);
        }

        private void UpdateRefreshSettings(RefreshSettings refreshSettings, string intervalKey, string refreshAfterKey, string delayKey)
        {
            var interval = GetSettingByKey(intervalKey);
            interval.Value = refreshSettings.Interval.ToString();

            var refreshAfter = GetSettingByKey(refreshAfterKey);
            refreshAfter.Value = refreshSettings.RefreshAfter.ToString();

            var delay = GetSettingByKey(delayKey);
            delay.Value = refreshSettings.StartDelay.ToString();

            repository.Update(interval);
            repository.Update(refreshAfter);
            repository.Update(delay);
        }

        private RefreshSettings CreateRefreshSettings(string intervalKey, string refreshAfterKey, string delayKey)
        {
            var refreshSettings = new RefreshSettings
            {
                Interval = int.Parse(GetSettingByKey(intervalKey).Value),
                RefreshAfter = int.Parse(GetSettingByKey(refreshAfterKey).Value),
                StartDelay = int.Parse(GetSettingByKey(delayKey).Value),
            };

            return refreshSettings;
        }

        private AppSetting GetSettingByKey(string key)
        {
            var setting = repository.GetByPredicate((setting) => setting.Key == key);

            return setting ?? throw new ArgumentNullException("Setting with Key {key} not found", key);
        }
    }
}
