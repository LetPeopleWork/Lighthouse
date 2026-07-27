using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation
{
    public sealed class LighthouseClock : ILighthouseClock
    {
        private readonly TimeProvider timeProvider;

        public LighthouseClock(TimeZoneInfo zone, TimeProvider timeProvider)
        {
            Zone = zone;
            this.timeProvider = timeProvider;
        }

        public TimeZoneInfo Zone { get; }

        public DateTimeOffset Now => timeProvider.GetUtcNow();

        public DateOnly Today => ToInstanceDay(timeProvider.GetUtcNow().UtcDateTime);

        public DateTime TodayAsUtcMidnight => InstanceCalendar.AsUtcMidnight(Today);

        public DateOnly ToInstanceDay(DateTime utcInstant) => InstanceCalendar.DayOf(utcInstant, Zone);

        /// <summary>
        /// Resolution order: a configured id wins, then <see cref="TimeZoneInfo.Local"/>, then UTC.
        /// Absent means "no opinion" and resolves silently; an unresolvable configured id means "an
        /// opinion that cannot be honoured" and throws, because silently downgrading the second to
        /// the first is how Bug #5567's class of defect hides.
        /// </summary>
        public static TimeZoneInfo ResolveInstanceTimeZone(
            string? configuredTimeZoneId,
            Func<TimeZoneInfo?>? localTimeZoneProvider = null)
        {
            if (!string.IsNullOrWhiteSpace(configuredTimeZoneId))
            {
                return ConfiguredTimeZone(configuredTimeZoneId.Trim());
            }

            return LocalTimeZoneOrUtc(localTimeZoneProvider ?? (() => TimeZoneInfo.Local));
        }

        private static TimeZoneInfo ConfiguredTimeZone(string configuredTimeZoneId)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(configuredTimeZoneId);
            }
            catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
                throw new InvalidOperationException(
                    $"The configured instance time zone '{configuredTimeZoneId}' could not be resolved on this host. " +
                    "Set Lighthouse:TimeZone (or the Lighthouse__TimeZone environment variable) to a valid IANA id " +
                    "such as 'Europe/Zurich', or remove it to use the host time zone.",
                    exception);
            }
        }

        private static TimeZoneInfo LocalTimeZoneOrUtc(Func<TimeZoneInfo?> localTimeZoneProvider)
        {
            try
            {
                return localTimeZoneProvider() ?? TimeZoneInfo.Utc;
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.Utc;
            }
        }
    }
}
