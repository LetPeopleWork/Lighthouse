namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// Bug #5567: the one instant-to-calendar-day reduction. It lives in Models because entities take
    /// the zone as a value and may not depend on <c>Services.Interfaces</c>; ILighthouseClock.ToInstanceDay
    /// delegates here so the rule exists once.
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
    }
}
