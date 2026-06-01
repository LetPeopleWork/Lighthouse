using Lighthouse.Backend.Models.AppSettings;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface IAppSettingService
    {
        RefreshSettings GetTeamDataRefreshSettings();

        Task UpdateTeamDataRefreshSettings(RefreshSettings refreshSettings);
        
        RefreshSettings GetFeatureRefreshSettings();

        Task UpdateFeatureRefreshSettings(RefreshSettings refreshSettings);

        int GetRefreshLogRetentionRuns();

        Task EnsureInstallTimestamp();

        DateTimeOffset? GetInstallTimestamp();

        DateTimeOffset? GetSurveyNudgeNextEligibleAt();

        Task RecordSurveyNudgeAction(SurveyNudgeAction action);
    }
}
