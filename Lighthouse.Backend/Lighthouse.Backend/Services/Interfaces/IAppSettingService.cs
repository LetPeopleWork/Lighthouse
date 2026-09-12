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

        /// <summary>
        /// Creates this instance's usage-data identifier if it has none, and answers with it either
        /// way. Called only when somebody agrees to send usage data: an instance nobody has agreed on
        /// never gets an identifier at all, which is what makes "we hold nothing about instances that
        /// did not opt in" true of the database rather than only of the network.
        /// </summary>
        Task<string> EnsureUsageDataInstanceId();

        /// <summary>
        /// This instance's usage-data identifier, or null if nobody has ever agreed. Never returned
        /// to a browser by any endpoint.
        /// </summary>
        string? GetUsageDataInstanceId();
    }
}
