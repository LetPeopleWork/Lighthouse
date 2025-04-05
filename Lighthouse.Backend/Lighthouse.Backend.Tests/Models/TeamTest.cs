using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    public class TeamTest
    {
        [Test]
        public void GetThroughputSettings_ReturnsCorrectSettings()
        {
            var team = new Team
            {
                ThroughputHistory = 30,
                UseFixedDatesForThroughput = false
            };

            var settings = team.GetThroughputSettings();

            var expectedStartDate = DateTime.UtcNow.Date.AddDays(-29);
            var expectedEndDate = DateTime.UtcNow.Date;
            var expectedNumberOfDays = 30;

            Assert.That(settings.StartDate, Is.EqualTo(expectedStartDate));
            Assert.That(settings.EndDate, Is.EqualTo(expectedEndDate));
            Assert.That(settings.NumberOfDays, Is.EqualTo(expectedNumberOfDays));
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

            var settings = team.GetThroughputSettings();

            var expectedNumberOfDays = (endDate - startDate).Days + 1;

            Assert.That(settings.StartDate, Is.EqualTo(startDate));
            Assert.That(settings.EndDate, Is.EqualTo(endDate));
            Assert.That(settings.NumberOfDays, Is.EqualTo(expectedNumberOfDays));
        }
    }
}
