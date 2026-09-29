using Lighthouse.Backend.API.DTO;

namespace Lighthouse.Backend.API.Helpers
{
    public static class ThroughputHistoryValidator
    {
        public const string ErrorMessage = "Throughput history must be at least 1 day.";

        // A Team on fixed dates never reads its rolling window, so that value is left alone.
        public static bool IsValid(TeamSettingDto teamSetting)
        {
            return teamSetting.UseFixedDatesForThroughput || teamSetting.ThroughputHistory > 0;
        }

        // Teams saved before this rule existed can still hold 0. Sending that value back unchanged
        // must not block saving their other settings, but nobody may newly set 0 or less.
        public static bool IsValid(TeamSettingDto teamSetting, int storedThroughputHistory)
        {
            return IsValid(teamSetting) || teamSetting.ThroughputHistory == storedThroughputHistory;
        }
    }
}
