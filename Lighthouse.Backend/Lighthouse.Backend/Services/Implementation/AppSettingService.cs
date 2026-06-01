using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using System.Globalization;

namespace Lighthouse.Backend.Services.Implementation
{
    public class AppSettingService(IRepository<AppSetting> repository, TimeProvider timeProvider) : IAppSettingService
    {
        private const string RoundtripFormat = "O";

        private const int RemindLaterBackOffThreshold = 2;

        private static readonly TimeSpan RemindLaterCadence = TimeSpan.FromDays(7);

        private const int QuietCadenceInMonths = 6;

        public RefreshSettings GetFeatureRefreshSettings()
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

        public int GetRefreshLogRetentionRuns()
        {
            var setting = repository.GetByPredicate(s => s.Key == AppSettingKeys.RefreshLogRetentionRuns);
            if (setting == null || !int.TryParse(setting.Value, out var value))
            {
                return 30;
            }

            return Math.Clamp(value, 10, 200);
        }

        public async Task EnsureInstallTimestamp()
        {
            var existing = repository.GetByPredicate(s => s.Key == AppSettingKeys.InstallTimestamp);
            if (existing != null)
            {
                return;
            }

            var installedAt = timeProvider.GetUtcNow().ToUniversalTime();
            repository.Add(new AppSetting
            {
                Key = AppSettingKeys.InstallTimestamp,
                Value = installedAt.ToString(RoundtripFormat, CultureInfo.InvariantCulture),
            });

            await repository.Save();
        }

        public DateTimeOffset? GetInstallTimestamp()
        {
            var setting = repository.GetByPredicate(s => s.Key == AppSettingKeys.InstallTimestamp);
            if (setting == null)
            {
                return null;
            }

            if (!DateTimeOffset.TryParse(setting.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
            {
                return null;
            }

            return parsed;
        }

        public DateTimeOffset? GetSurveyNudgeNextEligibleAt()
        {
            var setting = repository.GetByPredicate(s => s.Key == AppSettingKeys.SurveyNudgeNextEligibleAt);
            if (setting == null)
            {
                return null;
            }

            if (!DateTimeOffset.TryParse(setting.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
            {
                return null;
            }

            return parsed;
        }

        public async Task RecordSurveyNudgeAction(SurveyNudgeAction action)
        {
            var now = timeProvider.GetUtcNow().ToUniversalTime();
            var remindLaterCount = action == SurveyNudgeAction.RemindLater ? GetRemindLaterCount() + 1 : 0;
            var nextEligibleAt = ComputeNextEligibleAt(action, now, remindLaterCount);

            UpsertSetting(AppSettingKeys.SurveyNudgeNextEligibleAt, nextEligibleAt.ToString(RoundtripFormat, CultureInfo.InvariantCulture));
            UpsertSetting(AppSettingKeys.SurveyNudgeRemindLaterCount, remindLaterCount.ToString(CultureInfo.InvariantCulture));

            await repository.Save();
        }

        private static DateTimeOffset ComputeNextEligibleAt(SurveyNudgeAction action, DateTimeOffset now, int remindLaterCount)
        {
            if (action != SurveyNudgeAction.RemindLater)
            {
                return now.AddMonths(QuietCadenceInMonths);
            }

            if (remindLaterCount > RemindLaterBackOffThreshold)
            {
                return now.AddMonths(QuietCadenceInMonths);
            }

            return now.Add(RemindLaterCadence);
        }

        private int GetRemindLaterCount()
        {
            var setting = repository.GetByPredicate(s => s.Key == AppSettingKeys.SurveyNudgeRemindLaterCount);
            if (setting == null || !int.TryParse(setting.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count))
            {
                return 0;
            }

            return count;
        }

        private void UpsertSetting(string key, string value)
        {
            var existing = repository.GetByPredicate(s => s.Key == key);
            if (existing == null)
            {
                repository.Add(new AppSetting { Key = key, Value = value });
                return;
            }

            existing.Value = value;
            repository.Update(existing);
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
    }
}
