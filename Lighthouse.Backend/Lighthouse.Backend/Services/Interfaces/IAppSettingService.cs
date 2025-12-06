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

        ProjectSettingDto GetDefaultProjectSettings();

        WorkTrackingSystemSettings GetWorkTrackingSystemSettings();

        Task UpdateWorkTrackingSystemSettings(WorkTrackingSystemSettings workTrackingSystemSettings);
    }
}
