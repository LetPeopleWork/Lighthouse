using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.OptionalFeatures;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// What earlier releases stored about filling in past days on the over-time charts, while that was still
    /// a behaviour setting: the setting itself, and a record that it had been switched on once by default.
    /// Start-up now removes both. Spelled out rather than read off the product, because an instance upgrading into this
    /// release carries these exact strings whatever the code calls them today.
    /// </summary>
    internal static class TheHistoryFillAsAnEarlierReleaseStoredIt
    {
        public const string TheHistoryFillKey = "OverTimeHistoryFill";

        public const string TheRecordOfTheOneTimeSwitchOn = "OptionalFeatures:HistoryFillSwitchedOnByDefault";

        /// <summary>Id is zero because every stored behaviour setting carries zero; rows are told apart by key.</summary>
        public static OptionalFeature TheFillAsTheLastReleaseStoredIt(bool enabled)
            => new()
            {
                Id = 0,
                Key = TheHistoryFillKey,
                Name = "Fill in past days on over-time charts",
                Description = "A preview. While this is on, opening Percentiles Over Time or PBC Over Time fills in the days the chart is missing.",
                Enabled = enabled,
                IsPreview = true,
                IsPremium = false,
            };

        public static AppSetting TheRecordOfTheOneTimeSwitchOnAsStored()
            => new() { Id = 0, Key = TheRecordOfTheOneTimeSwitchOn, Value = "true" };
    }
}
