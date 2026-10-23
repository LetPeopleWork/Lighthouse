namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// Time zones built in code, so a test that needs one does not depend on the time zone database of
    /// the machine it runs on.
    /// </summary>
    public static class TestZones
    {
        /// <summary>
        /// Fourteen hours ahead of UTC: a wall-clock reading taken here is never the UTC instant, and for
        /// most of the day it lands on the next calendar day. The offset is far larger than any
        /// daylight-saving shift, so "did the value move" always has an unambiguous answer.
        /// </summary>
        public static TimeZoneInfo FarAheadOfUtc { get; } = TimeZoneInfo.CreateCustomTimeZone(
            "Test-Plus14",
            TimeSpan.FromHours(14),
            "UTC+14 (test zone)",
            "UTC+14 (test zone)");
    }
}
