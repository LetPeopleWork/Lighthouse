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

            var teamSettings = new TeamSettingDto
            {
                Name = GetSettingByKey(AppSettingKeys.TeamSettingName).Value,
                ThroughputHistory = int.Parse(GetSettingByKey(AppSettingKeys.TeamSettingHistory).Value),
                ThroughputHistoryStartDate = DateTime.UtcNow.Date.AddDays(-90),
                ThroughputHistoryEndDate = DateTime.UtcNow.Date,
                FeatureWIP = int.Parse(GetSettingByKey(AppSettingKeys.TeamSettingFeatureWIP).Value),
                WorkItemQuery = GetSettingByKey(AppSettingKeys.TeamSettingWorkItemQuery).Value,
                WorkItemTypes = workItemTypes,
                RelationCustomField = GetSettingByKey(AppSettingKeys.TeamSettingRelationCustomField).Value,
                AutomaticallyAdjustFeatureWIP = bool.Parse(GetSettingByKey(AppSettingKeys.TeamSettingAutomaticallyAdjustFeatureWIP).Value),
                ToDoStates = toDoStates,
                DoingStates = doingStates,
                DoneStates = doneStates,
                Tags = GetListByKey(AppSettingKeys.TeamSettingTags),
                ServiceLevelExpectationProbability = int.Parse(GetSettingByKey(AppSettingKeys.TeamSettingSLEProbability).Value),
                ServiceLevelExpectationRange = int.Parse(GetSettingByKey(AppSettingKeys.TeamSettingSLERange).Value),
            };

            return teamSettings;
        }

        public async Task UpdateDefaultTeamSettings(TeamSettingDto defaultTeamSetting)
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
            
            var todoStates = GetSettingByKey(AppSettingKeys.TeamSettingToDoStates);
            todoStates.Value = string.Join(',', defaultTeamSetting.ToDoStates);
            repository.Update(todoStates);
            
            var doingStates = GetSettingByKey(AppSettingKeys.TeamSettingDoingStates);
            doingStates.Value = string.Join(',', defaultTeamSetting.DoingStates);
            repository.Update(doingStates);
            
            var doneStates = GetSettingByKey(AppSettingKeys.TeamSettingDoneStates);
            doneStates.Value = string.Join(',', defaultTeamSetting.DoneStates);
            repository.Update(doneStates);
            
            var tags = GetSettingByKey(AppSettingKeys.TeamSettingTags);
            tags.Value = string.Join(',', defaultTeamSetting.Tags);
            repository.Update(tags);
            
            var relatedField = GetSettingByKey(AppSettingKeys.TeamSettingRelationCustomField);
            relatedField.Value = defaultTeamSetting.RelationCustomField;
            repository.Update(relatedField);
            
            var autoAdjustWIPField = GetSettingByKey(AppSettingKeys.TeamSettingAutomaticallyAdjustFeatureWIP);
            autoAdjustWIPField.Value = defaultTeamSetting.AutomaticallyAdjustFeatureWIP.ToString();
            repository.Update(autoAdjustWIPField);

            var sleProbability = GetSettingByKey(AppSettingKeys.TeamSettingSLEProbability);
            sleProbability.Value = defaultTeamSetting.ServiceLevelExpectationProbability.ToString();
            repository.Update(sleProbability);

            var sleRange = GetSettingByKey(AppSettingKeys.TeamSettingSLERange);
            sleRange.Value = defaultTeamSetting.ServiceLevelExpectationRange.ToString();
            repository.Update(sleRange);

            await repository.Save();
        }

        public ProjectSettingDto GetDefaultProjectSettings()
        {
            var workItemTypes = GetListByKey(AppSettingKeys.ProjectSettingWorkItemTypes);

            var toDoStates = GetListByKey(AppSettingKeys.ProjectSettingToDoStates);
            var doingStates = GetListByKey(AppSettingKeys.ProjectSettingDoingStates);
            var doneStates = GetListByKey(AppSettingKeys.ProjectSettingDoneStates);
            var tags = GetListByKey(AppSettingKeys.ProjectSettingTags);

            var overrideRealChildCountStates = GetListByKey(AppSettingKeys.ProjectSettingOverrideRealChildCountStates);

            var projectSettings = new ProjectSettingDto
            {
                Name = GetSettingByKey(AppSettingKeys.ProjectSettingName).Value,
                WorkItemQuery = GetSettingByKey(AppSettingKeys.ProjectSettingWorkItemQuery).Value,
                WorkItemTypes = workItemTypes,
                UnparentedItemsQuery = GetSettingByKey(AppSettingKeys.ProjectSettingUnparentedWorkItemQuery).Value,

                UsePercentileToCalculateDefaultAmountOfWorkItems = bool.Parse(GetSettingByKey(AppSettingKeys.ProjectSettingUsePercentileToCalculateDefaultAmountOfWorkItems).Value),
                DefaultAmountOfWorkItemsPerFeature = int.Parse(GetSettingByKey(AppSettingKeys.ProjectSettingDefaultAmountOfWorkItemsPerFeature).Value),
                DefaultWorkItemPercentile = int.Parse(GetSettingByKey(AppSettingKeys.ProjectSettingDefaultWorkItemPercentile).Value),
                HistoricalFeaturesWorkItemQuery = GetSettingByKey(AppSettingKeys.ProjectSettingHistoricalFeaturesWorkItemQuery).Value,
                ToDoStates = toDoStates,
                DoingStates = doingStates,
                DoneStates = doneStates,
                Tags = tags,

                OverrideRealChildCountStates = overrideRealChildCountStates,

                SizeEstimateField = GetSettingByKey(AppSettingKeys.ProjectSettingSizeEstimateField).Value,
                FeatureOwnerField = GetSettingByKey(AppSettingKeys.ProjectSettingsFeatureOwnerField).Value,

                ServiceLevelExpectationProbability = int.Parse(GetSettingByKey(AppSettingKeys.ProjectSettingSLEProbability).Value),
                ServiceLevelExpectationRange = int.Parse(GetSettingByKey(AppSettingKeys.ProjectSettingSLERange).Value),
            };

            return projectSettings;
        }

        public async Task UpdateDefaultProjectSettings(ProjectSettingDto defaultProjectSetting)
        {
            var name = GetSettingByKey(AppSettingKeys.ProjectSettingName);
            name.Value = defaultProjectSetting.Name;
            repository.Update(name);

            var workItemQuery = GetSettingByKey(AppSettingKeys.ProjectSettingWorkItemQuery);
            workItemQuery.Value = defaultProjectSetting.WorkItemQuery;
            repository.Update(workItemQuery);

            var workItemTypes = GetSettingByKey(AppSettingKeys.ProjectSettingWorkItemTypes);
            workItemTypes.Value = string.Join(',', defaultProjectSetting.WorkItemTypes);
            repository.Update(workItemTypes);

            var toDoStates = GetSettingByKey(AppSettingKeys.ProjectSettingToDoStates);
            toDoStates.Value = string.Join(',', defaultProjectSetting.ToDoStates);
            repository.Update(toDoStates);

            var doingStates = GetSettingByKey(AppSettingKeys.ProjectSettingDoingStates);
            doingStates.Value = string.Join(',', defaultProjectSetting.DoingStates);
            repository.Update(doingStates);

            var doneStates = GetSettingByKey(AppSettingKeys.ProjectSettingDoneStates);
            doneStates.Value = string.Join(',', defaultProjectSetting.DoneStates);
            repository.Update(doneStates);

            var tags = GetSettingByKey(AppSettingKeys.ProjectSettingTags);
            tags.Value = string.Join(',', defaultProjectSetting.Tags);
            repository.Update(tags);

            var overrideRealChildCountStates = GetSettingByKey(AppSettingKeys.ProjectSettingOverrideRealChildCountStates);
            overrideRealChildCountStates.Value = string.Join(',', defaultProjectSetting.OverrideRealChildCountStates);
            repository.Update(overrideRealChildCountStates);

            var unparentedItemQuery = GetSettingByKey(AppSettingKeys.ProjectSettingUnparentedWorkItemQuery);
            unparentedItemQuery.Value = defaultProjectSetting.UnparentedItemsQuery;
            repository.Update(unparentedItemQuery);

            var usePercentileToCalculateDefaultAmountOfWorkItems = GetSettingByKey(AppSettingKeys.ProjectSettingUsePercentileToCalculateDefaultAmountOfWorkItems);
            usePercentileToCalculateDefaultAmountOfWorkItems.Value = defaultProjectSetting.UsePercentileToCalculateDefaultAmountOfWorkItems.ToString();
            repository.Update(usePercentileToCalculateDefaultAmountOfWorkItems);

            var defaultAmountofItems = GetSettingByKey(AppSettingKeys.ProjectSettingDefaultAmountOfWorkItemsPerFeature);
            defaultAmountofItems.Value = defaultProjectSetting.DefaultAmountOfWorkItemsPerFeature.ToString();
            repository.Update(defaultAmountofItems);

            var defaultWorkItemPercentile = GetSettingByKey(AppSettingKeys.ProjectSettingDefaultWorkItemPercentile);
            defaultWorkItemPercentile.Value = defaultProjectSetting.DefaultWorkItemPercentile.ToString();
            repository.Update(defaultWorkItemPercentile);

            var historicalFeaturesWorkItemQuery = GetSettingByKey(AppSettingKeys.ProjectSettingHistoricalFeaturesWorkItemQuery);
            historicalFeaturesWorkItemQuery.Value = defaultProjectSetting.HistoricalFeaturesWorkItemQuery;
            repository.Update(historicalFeaturesWorkItemQuery);

            var sizeEstimateField = GetSettingByKey(AppSettingKeys.ProjectSettingSizeEstimateField);
            sizeEstimateField.Value = defaultProjectSetting.SizeEstimateField;
            repository.Update(sizeEstimateField);

            var featureOwnerField = GetSettingByKey(AppSettingKeys.ProjectSettingsFeatureOwnerField);
            featureOwnerField.Value = defaultProjectSetting.FeatureOwnerField;
            repository.Update(featureOwnerField);

            var sleProbability = GetSettingByKey(AppSettingKeys.ProjectSettingSLEProbability);
            sleProbability.Value = defaultProjectSetting.ServiceLevelExpectationProbability.ToString();
            repository.Update(sleProbability);

            var sleRange = GetSettingByKey(AppSettingKeys.ProjectSettingSLERange);
            sleRange.Value = defaultProjectSetting.ServiceLevelExpectationRange.ToString();
            repository.Update(sleRange);

            await repository.Save();
        }

        public DataRetentionSettings GetDataRetentionSettings()
        {
            return new DataRetentionSettings
            {
                MaxStorageTimeInDays = int.Parse(GetSettingByKey(AppSettingKeys.CleanUpDataHistorySettingsMaxStorageTimeInDays).Value)
            };
        }

        public async Task UpdateDataRetentionSettings(DataRetentionSettings cleanUpDataHistorySettings)
        {
            var maxStorageTimeInDays = GetSettingByKey(AppSettingKeys.CleanUpDataHistorySettingsMaxStorageTimeInDays);
            maxStorageTimeInDays.Value = cleanUpDataHistorySettings.MaxStorageTimeInDays.ToString();
            repository.Update(maxStorageTimeInDays);

            await repository.Save();
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
