using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class AppSettingRepository : RepositoryBase<AppSetting>
    {
        public AppSettingRepository(LighthouseAppContext context, ILogger<AppSettingRepository> logger) : base(context, (LighthouseAppContext context) => context.AppSettings, logger)
        {
            SeedAppSettings();
        }

        private void SeedAppSettings()
        {
            AddIfNotExists(new AppSetting { Id = 0, Key = AppSettingKeys.TeamDataRefreshInterval, Value = "60" });
            AddIfNotExists(new AppSetting { Id = 1, Key = AppSettingKeys.TeamDataRefreshAfter, Value = "180" });
            AddIfNotExists(new AppSetting { Id = 2, Key = AppSettingKeys.TeamDataRefreshStartDelay, Value = "10" });
            AddIfNotExists(new AppSetting { Id = 3, Key = AppSettingKeys.FeaturesRefreshInterval, Value = "60" });
            AddIfNotExists(new AppSetting { Id = 4, Key = AppSettingKeys.FeaturesRefreshAfter, Value = "180" });
            AddIfNotExists(new AppSetting { Id = 5, Key = AppSettingKeys.FeaturesRefreshStartDelay, Value = "15" });
            AddIfNotExists(new AppSetting { Id = 9, Key = AppSettingKeys.TeamSettingName, Value = "New Team" });
            AddIfNotExists(new AppSetting { Id = 10, Key = AppSettingKeys.TeamSettingHistory, Value = "30" });
            AddIfNotExists(new AppSetting { Id = 11, Key = AppSettingKeys.TeamSettingFeatureWIP, Value = "1" });
            AddIfNotExists(new AppSetting { Id = 12, Key = AppSettingKeys.TeamSettingWorkItemQuery, Value = string.Empty });
            AddIfNotExists(new AppSetting { Id = 13, Key = AppSettingKeys.TeamSettingWorkItemTypes, Value = "User Story,Bug" });
            AddIfNotExists(new AppSetting { Id = 14, Key = AppSettingKeys.TeamSettingRelationCustomField, Value = string.Empty });
            AddIfNotExists(new AppSetting { Id = 9, Key = AppSettingKeys.ProjectSettingName, Value = "New Project" });
            AddIfNotExists(new AppSetting { Id = 12, Key = AppSettingKeys.ProjectSettingWorkItemQuery, Value = string.Empty });
            AddIfNotExists(new AppSetting { Id = 13, Key = AppSettingKeys.ProjectSettingWorkItemTypes, Value = "Epic" });
            AddIfNotExists(new AppSetting { Id = 14, Key = AppSettingKeys.ProjectSettingUnparentedWorkItemQuery, Value = string.Empty });
            AddIfNotExists(new AppSetting { Id = 14, Key = AppSettingKeys.ProjectSettingDefaultAmountOfWorkItemsPerFeature, Value = "10" });
            AddIfNotExists(new AppSetting { Id = 15, Key = AppSettingKeys.ProjectSettingSizeEstimateField, Value = "" });
            AddIfNotExists(new AppSetting { Id = 16, Key = AppSettingKeys.ProjectSettingUsePercentileToCalculateDefaultAmountOfWorkItems, Value = $"{false}" });
            AddIfNotExists(new AppSetting { Id = 17, Key = AppSettingKeys.ProjectSettingDefaultWorkItemPercentile, Value = "85" });
            AddIfNotExists(new AppSetting { Id = 18, Key = AppSettingKeys.ProjectSettingHistoricalFeaturesWorkItemQuery, Value = string.Empty });

            AddIfNotExists(new AppSetting { Id = 19, Key = AppSettingKeys.ProjectSettingToDoStates, Value = "New,Proposed,To Do" });
            AddIfNotExists(new AppSetting { Id = 20, Key = AppSettingKeys.ProjectSettingDoingStates, Value = "Active,Resolved,In Progress,Committed" });
            AddIfNotExists(new AppSetting { Id = 21, Key = AppSettingKeys.ProjectSettingDoneStates, Value = "Done,Closed" });

            AddIfNotExists(new AppSetting { Id = 22, Key = AppSettingKeys.TeamSettingToDoStates, Value = "New,Proposed,To Do" });
            AddIfNotExists(new AppSetting { Id = 23, Key = AppSettingKeys.TeamSettingDoingStates, Value = "Active,Resolved,In Progress,Committed" });
            AddIfNotExists(new AppSetting { Id = 24, Key = AppSettingKeys.TeamSettingDoneStates, Value = "Done,Closed" });

            AddIfNotExists(new AppSetting { Id = 25, Key = AppSettingKeys.ProjectSettingOverrideRealChildCountStates, Value = string.Empty });

            AddIfNotExists(new AppSetting { Id = 26, Key = AppSettingKeys.CleanUpDataHistorySettingsMaxStorageTimeInDays, Value = "90" });
                        
            AddIfNotExists(new AppSetting { Id = 27, Key = AppSettingKeys.ProjectSettingsFeatureOwnerField, Value = string.Empty });
            AddIfNotExists(new AppSetting { Id = 28, Key = AppSettingKeys.TeamSettingAutomaticallyAdjustFeatureWIP, Value = $"{false}" });

            AddIfNotExists(new AppSetting { Id = 29, Key = AppSettingKeys.TeamSettingTags, Value = string.Empty });
            AddIfNotExists(new AppSetting { Id = 30, Key = AppSettingKeys.ProjectSettingTags, Value = string.Empty });

            AddIfNotExists(new AppSetting { Id = 31, Key = AppSettingKeys.TeamSettingSLEProbability, Value = "0" });
            AddIfNotExists(new AppSetting { Id = 32, Key = AppSettingKeys.TeamSettingSLERange, Value = "0" });

            AddIfNotExists(new AppSetting { Id = 33, Key = AppSettingKeys.ProjectSettingSLEProbability, Value = "0" });
            AddIfNotExists(new AppSetting { Id = 34, Key = AppSettingKeys.ProjectSettingSLERange, Value = "0" });

            SaveSync();
        }

        private void AddIfNotExists(AppSetting defaultAppSetting)
        {
            var existingDefault = GetByPredicate((appSetting) => appSetting.Key == defaultAppSetting.Key);
            if (existingDefault == null)
            {
                Add(defaultAppSetting);
            }
        }
    }
}
