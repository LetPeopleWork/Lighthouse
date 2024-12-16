using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models.AppSettings;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface IAppSettingService
    {
        RefreshSettings GetThroughputRefreshSettings();

        Task UpdateThroughputRefreshSettings(RefreshSettings refreshSettings);
        
        RefreshSettings GetFeaturRefreshSettings();

        Task UpdateFeatureRefreshSettings(RefreshSettings refreshSettings);

        RefreshSettings GetForecastRefreshSettings();

        Task UpdateForecastRefreshSettings(RefreshSettings refreshSettings);

        TeamSettingDto GetDefaultTeamSettings();

        Task UpdateDefaultTeamSettings(TeamSettingDto defaultTeamSetting);

        ProjectSettingDto GetDefaultProjectSettings();

        Task UpdateDefaultProjectSettings(ProjectSettingDto defaultProjectSetting);

        DataRetentionSettings GetDataRetentionSettings();

        Task UpdateDataRetentionSettings(DataRetentionSettings cleanUpDataHistorySettings);
    }
}
