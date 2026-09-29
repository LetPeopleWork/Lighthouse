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
    }
}
