using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;

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

        public RefreshSettings GetTeamDataRefreshSettings()
        {
            return CreateRefreshSettings(
               AppSettingKeys.TeamDataRefreshInterval,
               AppSettingKeys.TeamDataRefreshAfter,
               AppSettingKeys.TeamDataRefreshStartDelay);
        }

        public async Task UpdateFeatureRefreshSettings(RefreshSettings refreshSettings)
        {
            await UpdateRefreshSettingsAsync(refreshSettings, AppSettingKeys.FeaturesRefreshInterval, AppSettingKeys.FeaturesRefreshAfter, AppSettingKeys.FeaturesRefreshStartDelay);
        }

        public async Task UpdateTeamDataRefreshSettings(RefreshSettings refreshSettings)
        {
            await UpdateRefreshSettingsAsync(refreshSettings, AppSettingKeys.TeamDataRefreshInterval, AppSettingKeys.TeamDataRefreshAfter, AppSettingKeys.TeamDataRefreshStartDelay);
        }

        public TeamSettingDto GetDefaultTeamSettings()
        {
            var workItemTypes = GetListByKey(AppSettingKeys.TeamSettingWorkItemTypes);

            var toDoStates = GetListByKey(AppSettingKeys.TeamSettingToDoStates);
            var doingStates = GetListByKey(AppSettingKeys.TeamSettingDoingStates);
            var doneStates = GetListByKey(AppSettingKeys.TeamSettingDoneStates);
            var blockedStates = GetListByKey(AppSettingKeys.TeamSettingBlockedStates);
            var blockedTags = GetListByKey(AppSettingKeys.TeamSettingBlockedTags);

            var teamSettings = new TeamSettingDto
            {
                Name = GetSettingByKey(AppSettingKeys.TeamSettingName).Value,
                ThroughputHistory = int.Parse(GetSettingByKey(AppSettingKeys.TeamSettingHistory).Value),
                ThroughputHistoryStartDate = DateTime.UtcNow.Date.AddDays(-90),
                ThroughputHistoryEndDate = DateTime.UtcNow.Date,
                FeatureWIP = int.Parse(GetSettingByKey(AppSettingKeys.TeamSettingFeatureWIP).Value),
                WorkItemQuery = GetSettingByKey(AppSettingKeys.TeamSettingWorkItemQuery).Value,
                WorkItemTypes = workItemTypes,
                ParentOverrideField = GetSettingByKey(AppSettingKeys.TeamSettingParentOverrideField).Value,
                AutomaticallyAdjustFeatureWIP = bool.Parse(GetSettingByKey(AppSettingKeys.TeamSettingAutomaticallyAdjustFeatureWIP).Value),
                ToDoStates = toDoStates,
                DoingStates = doingStates,
                DoneStates = doneStates,
                Tags = GetListByKey(AppSettingKeys.TeamSettingTags),
                ServiceLevelExpectationProbability = int.Parse(GetSettingByKey(AppSettingKeys.TeamSettingSLEProbability).Value),
                ServiceLevelExpectationRange = int.Parse(GetSettingByKey(AppSettingKeys.TeamSettingSLERange).Value),
                BlockedStates = blockedStates,
                BlockedTags = blockedTags,
                DoneItemsCutoffDays = 180,
            };

            return teamSettings;
        }

        public PortfolioSettingDto GetDefaultProjectSettings()
        {
            var workItemTypes = GetListByKey(AppSettingKeys.ProjectSettingWorkItemTypes);

            var toDoStates = GetListByKey(AppSettingKeys.ProjectSettingToDoStates);
            var doingStates = GetListByKey(AppSettingKeys.ProjectSettingDoingStates);
            var doneStates = GetListByKey(AppSettingKeys.ProjectSettingDoneStates);
            var tags = GetListByKey(AppSettingKeys.ProjectSettingTags);
            var blockedStates = GetListByKey(AppSettingKeys.ProjectSettingBlockedStates);
            var blockedTags = GetListByKey(AppSettingKeys.ProjectSettingBlockedTags);

            var overrideRealChildCountStates = GetListByKey(AppSettingKeys.ProjectSettingOverrideRealChildCountStates);

            var projectSettings = new PortfolioSettingDto
            {
                Name = GetSettingByKey(AppSettingKeys.ProjectSettingName).Value,
                WorkItemQuery = GetSettingByKey(AppSettingKeys.ProjectSettingWorkItemQuery).Value,
                WorkItemTypes = workItemTypes,
                UnparentedItemsQuery = GetSettingByKey(AppSettingKeys.ProjectSettingUnparentedWorkItemQuery).Value,

                UsePercentileToCalculateDefaultAmountOfWorkItems = bool.Parse(GetSettingByKey(AppSettingKeys.ProjectSettingUsePercentileToCalculateDefaultAmountOfWorkItems).Value),
                DefaultAmountOfWorkItemsPerFeature = int.Parse(GetSettingByKey(AppSettingKeys.ProjectSettingDefaultAmountOfWorkItemsPerFeature).Value),
                DefaultWorkItemPercentile = int.Parse(GetSettingByKey(AppSettingKeys.ProjectSettingDefaultWorkItemPercentile).Value),
                PercentileHistoryInDays = int.Parse(GetSettingByKey(AppSettingKeys.ProjectSettingPercentileHistoryInDays).Value),
                ToDoStates = toDoStates,
                DoingStates = doingStates,
                DoneStates = doneStates,
                Tags = tags,

                OverrideRealChildCountStates = overrideRealChildCountStates,

                SizeEstimateField = GetSettingByKey(AppSettingKeys.ProjectSettingSizeEstimateField).Value,
                FeatureOwnerField = GetSettingByKey(AppSettingKeys.ProjectSettingsFeatureOwnerField).Value,

                ServiceLevelExpectationProbability = int.Parse(GetSettingByKey(AppSettingKeys.ProjectSettingSLEProbability).Value),
                ServiceLevelExpectationRange = int.Parse(GetSettingByKey(AppSettingKeys.ProjectSettingSLERange).Value),

                ParentOverrideField = GetSettingByKey(AppSettingKeys.ProjectSettingParentOverrideField).Value,

                BlockedStates = blockedStates,
                BlockedTags = blockedTags,

                DoneItemsCutoffDays = 365,
            };

            return projectSettings;
        }

        public WorkTrackingSystemSettings GetWorkTrackingSystemSettings()
        {
            return new WorkTrackingSystemSettings
            {
                OverrideRequestTimeout = bool.Parse(GetSettingByKey(AppSettingKeys.WorkTrackingSystemSettingsOverrideRequestTimeout).Value),
                RequestTimeoutInSeconds = int.Parse(GetSettingByKey(AppSettingKeys.WorkTrackingSystemSettingsRequestTimeoutInSeconds).Value)
            };
        }

        public async Task UpdateWorkTrackingSystemSettings(WorkTrackingSystemSettings workTrackingSystemSettings)
        {
            var overrideRequestTimeout = GetSettingByKey(AppSettingKeys.WorkTrackingSystemSettingsOverrideRequestTimeout);
            overrideRequestTimeout.Value = workTrackingSystemSettings.OverrideRequestTimeout.ToString();
            repository.Update(overrideRequestTimeout);

            var requestTimeoutInSeconds = GetSettingByKey(AppSettingKeys.WorkTrackingSystemSettingsRequestTimeoutInSeconds);
            requestTimeoutInSeconds.Value = workTrackingSystemSettings.RequestTimeoutInSeconds.ToString();
            repository.Update(requestTimeoutInSeconds);
            
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

            return setting ?? throw new ArgumentNullException(nameof(key), "Setting with Key {key} not found");
        }

        private List<string> GetListByKey(string key)
        {
            var list = new List<string>();

            foreach (var listItem in GetSettingByKey(key).Value.Split(','))
            {
                list.Add(listItem.Trim());
            }

            return list;
        }
    }
}
