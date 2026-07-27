using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.Extensions.Time.Testing;

namespace Lighthouse.Backend.Tests.TestDoubles
{
    /// <summary>
    /// Bug #5567 - lets a test set the instant and the instance time zone independently, which is
    /// the whole point: the two disagree for roughly two hours every night and that window is where
    /// the bug lives. The day arithmetic is delegated to the real <see cref="LighthouseClock"/> so
    /// this double can never drift from production behaviour.
    /// </summary>
    public sealed class FakeLighthouseClock : ILighthouseClock
    {
        private readonly FakeTimeProvider timeProvider;

        private LighthouseClock clock;

        public FakeLighthouseClock(DateTimeOffset instant, TimeZoneInfo? zone = null)
        {
            timeProvider = new FakeTimeProvider(instant);
            clock = new LighthouseClock(zone ?? TimeZoneInfo.Utc, timeProvider);
        }

        public DateOnly Today => clock.Today;

        public DateTime TodayAsUtcMidnight => clock.TodayAsUtcMidnight;

        public DateTimeOffset Now => clock.Now;

        public TimeZoneInfo Zone => clock.Zone;

        public DateOnly ToInstanceDay(DateTime utcInstant) => clock.ToInstanceDay(utcInstant);

        public void SetInstant(DateTimeOffset instant) => timeProvider.SetUtcNow(instant);

        public void SetZone(TimeZoneInfo zone) => clock = new LighthouseClock(zone, timeProvider);
    }
}
