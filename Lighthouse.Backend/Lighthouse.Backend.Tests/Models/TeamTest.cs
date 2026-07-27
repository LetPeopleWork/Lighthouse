using Lighthouse.Backend.Models;
using Lighthouse.Backend.Tests.TestDoubles;

namespace Lighthouse.Backend.Tests.Models
{
    public class TeamTest
    {

        /// <summary>
        /// Bug #5567: a fixed instant on a UTC instance. The expectations below are unchanged from
        /// before the anchor moved - the point is that they no longer RE-DERIVE the production
        /// expression, which is root cause D.
        /// </summary>
        private static readonly FakeLighthouseClock Clock =
            new(new DateTimeOffset(2026, 7, 27, 10, 0, 0, TimeSpan.Zero), TimeZoneInfo.Utc);
        [Test]
        public void GetThroughputSettings_ReturnsCorrectSettings()
        {
            var team = new Team
            {
                ThroughputHistory = 30,
                UseFixedDatesForThroughput = false
            };

            var settings = team.GetThroughputSettings(Clock.Today);

            var expectedStartDate = Clock.TodayAsUtcMidnight.AddDays(-29);
            var expectedEndDate = Clock.TodayAsUtcMidnight;
            var expectedNumberOfDays = 30;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(settings.StartDate, Is.EqualTo(expectedStartDate));
                Assert.That(settings.EndDate, Is.EqualTo(expectedEndDate));
                Assert.That(settings.NumberOfDays, Is.EqualTo(expectedNumberOfDays));

                Assert.That(settings.StartDate.Kind, Is.EqualTo(DateTimeKind.Utc));
                Assert.That(settings.EndDate.Kind, Is.EqualTo(DateTimeKind.Utc));
            }
            ;
        }

        [Test]
        public void GetThroughputSettings_WithFixedDates_ReturnsCorrectSettings()
        {
            var startDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(2023, 1, 30, 0, 0, 0, DateTimeKind.Utc);
            var team = new Team
            {
                UseFixedDatesForThroughput = true,
                ThroughputHistoryStartDate = startDate,
                ThroughputHistoryEndDate = endDate
            };

            var settings = team.GetThroughputSettings(Clock.Today);

            var expectedNumberOfDays = (endDate - startDate).Days + 1;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(settings.StartDate, Is.EqualTo(startDate));
                Assert.That(settings.EndDate, Is.EqualTo(endDate));
                Assert.That(settings.NumberOfDays, Is.EqualTo(expectedNumberOfDays));

                Assert.That(settings.StartDate.Kind, Is.EqualTo(DateTimeKind.Utc));
                Assert.That(settings.EndDate.Kind, Is.EqualTo(DateTimeKind.Utc));
            };
        }
    }
}
