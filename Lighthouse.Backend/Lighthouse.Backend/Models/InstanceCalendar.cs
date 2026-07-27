namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// Bug #5567: the two conversions between an instant and a calendar day. Lives in Models because
    /// entities take the zone as a value and may not depend on <c>Services.Interfaces</c>;
    /// <c>ILighthouseClock</c> delegates here so each rule exists once.
    /// </summary>
    public static class InstanceCalendar
    {
        public static DateOnly DayOf(DateTime instant, TimeZoneInfo zone)
        {
            var utcInstant = instant.Kind == DateTimeKind.Local
                ? instant.ToUniversalTime()
                : DateTime.SpecifyKind(instant, DateTimeKind.Utc);

            return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utcInstant, zone));
        }

        /// <summary>
        /// A day as the instant every caller must hand to EF and to the DateTime-speaking APIs.
        /// <see cref="DateTimeKind.Utc"/> is load-bearing: the global EF converter shifts a
        /// local-kind midnight back onto the previous day, on writes and on query parameters alike.
        /// </summary>
        public static DateTime AsUtcMidnight(DateOnly day) => day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    }
}
