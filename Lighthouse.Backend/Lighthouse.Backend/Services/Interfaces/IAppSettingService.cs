using Lighthouse.Backend.Models;
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

        FeatureOrderingPolicy GetFeatureOrderingPolicy();

        /// <summary>
        /// Records who owns the order, and answers whether there was a setting to record it against. A
        /// missing setting means the installation never seeded one; nothing is written and nothing is
        /// created, so the caller has to be told rather than led to believe the change took.
        /// </summary>
        Task<bool> SetFeatureOrderingPolicy(FeatureOrderingPolicy policy);

        DateTimeOffset? GetSurveyNudgeNextEligibleAt();

        Task RecordSurveyNudgeAction(SurveyNudgeAction action);
    }
}
