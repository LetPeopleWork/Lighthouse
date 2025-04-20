using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models.AppSettings;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface IAppSettingService
    {
        RefreshSettings GetTeamDataRefreshSettings();

        Task UpdateTeamDataRefreshSettings(RefreshSettings refreshSettings);
        
        RefreshSettings GetFeaturRefreshSettings();

        Task UpdateFeatureRefreshSettings(RefreshSettings refreshSettings);

        TeamSettingDto GetDefaultTeamSettings();

        Task UpdateDefaultTeamSettings(TeamSettingDto defaultTeamSetting);

        ProjectSettingDto GetDefaultProjectSettings();

        Task UpdateDefaultProjectSettings(ProjectSettingDto defaultProjectSetting);

        DataRetentionSettings GetDataRetentionSettings();

        Task UpdateDataRetentionSettings(DataRetentionSettings cleanUpDataHistorySettings);
    }
}
