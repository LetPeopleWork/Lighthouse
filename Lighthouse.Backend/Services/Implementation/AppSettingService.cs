using Lighthouse.Backend.API.DTO;
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

        public async Task UpdateFeatureRefreshSettingsAsync(RefreshSettings refreshSettings)
        {
            await UpdateRefreshSettingsAsync(refreshSettings, AppSettingKeys.FeaturesRefreshInterval, AppSettingKeys.FeaturesRefreshAfter, AppSettingKeys.FeaturesRefreshStartDelay);
        }

        public async Task UpdateForecastRefreshSettingsAsync(RefreshSettings refreshSettings)
        {
            await UpdateRefreshSettingsAsync(refreshSettings, AppSettingKeys.ForecastRefreshInterval, AppSettingKeys.ForecastRefreshAfter, AppSettingKeys.ForecastRefreshStartDelay);
        }

        public async Task UpdateThroughputRefreshSettingsAsync(RefreshSettings refreshSettings)
        {
            await UpdateRefreshSettingsAsync(refreshSettings, AppSettingKeys.ThroughputRefreshInterval, AppSettingKeys.ThroughputRefreshAfter, AppSettingKeys.ThroughputRefreshStartDelay);
        }

        public TeamSettingDto GetDefaultTeamSettings()
        {
            var workItemTypes = GetDefaultWorkItemTypes(AppSettingKeys.TeamSettingWorkItemTypes);

            var teamSettings = new TeamSettingDto
            {
                Name = GetSettingByKey(AppSettingKeys.TeamSettingName).Value,
                ThroughputHistory = int.Parse(GetSettingByKey(AppSettingKeys.TeamSettingHistory).Value),
                FeatureWIP = int.Parse(GetSettingByKey(AppSettingKeys.TeamSettingFeatureWIP).Value),
                WorkItemQuery = GetSettingByKey(AppSettingKeys.TeamSettingWorkItemQuery).Value,
                WorkItemTypes = workItemTypes,
                RelationCustomField = GetSettingByKey(AppSettingKeys.TeamSettingRelationCustomField).Value,
            };

            return teamSettings;
        }

        public async Task UpdateDefaultTeamSettingsAsync(TeamSettingDto defaultTeamSetting)
        {
            var name = GetSettingByKey(AppSettingKeys.TeamSettingName);
            name.Value = defaultTeamSetting.Name;
            repository.Update(name);
            
            var history = GetSettingByKey(AppSettingKeys.TeamSettingHistory);
            history.Value = defaultTeamSetting.ThroughputHistory.ToString();
            repository.Update(history);
            
            var featureWip = GetSettingByKey(AppSettingKeys.TeamSettingFeatureWIP);
            featureWip.Value = defaultTeamSetting.FeatureWIP.ToString();
            repository.Update(featureWip);
            
            var workItemQuery = GetSettingByKey(AppSettingKeys.TeamSettingWorkItemQuery);
            workItemQuery.Value = defaultTeamSetting.WorkItemQuery;
            repository.Update(workItemQuery);
            
            var workItemTypes = GetSettingByKey(AppSettingKeys.TeamSettingWorkItemTypes);
            workItemTypes.Value = string.Join(',', defaultTeamSetting.WorkItemTypes);
            repository.Update(workItemTypes);
            
            var relatedField = GetSettingByKey(AppSettingKeys.TeamSettingRelationCustomField);
            relatedField.Value = defaultTeamSetting.RelationCustomField;
            repository.Update(relatedField);

            await repository.Save();
        }

        private async Task UpdateRefreshSettingsAsync(RefreshSettings refreshSettings, string intervalKey, string refreshAfterKey, string delayKey)
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

            await repository.Save();
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

        private List<string> GetDefaultWorkItemTypes(string appSettingKey)
        {
            var workItemTypes = new List<string>();

            foreach (var workItemType in GetSettingByKey(appSettingKey).Value.Split(','))
            {
                workItemTypes.Add(workItemType.Trim());
            }

            return workItemTypes;
        }
    }
}
