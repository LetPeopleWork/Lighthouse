using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    [TestFixture]
    public class BlackoutDaysExtensionsTest
    {
        [Test]
        public void GetBlackoutDayIndices_NoBlackoutPeriods_ReturnsEmpty()
        {
            var result = Array.Empty<BlackoutPeriod>().GetBlackoutDayIndices(
                new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc));

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetBlackoutDayIndices_BlackoutFullyWithinRange_ReturnsCorrectIndices()
        {
            var blackoutPeriods = new[]
            {
                new BlackoutPeriod { Start = new DateOnly(2025, 1, 3), End = new DateOnly(2025, 1, 5) }
            };

            var result = blackoutPeriods.GetBlackoutDayIndices(
                new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc));

            Assert.That(result, Is.EquivalentTo([2, 3, 4]));
        }

        [Test]
        public void GetBlackoutDayIndices_BlackoutPartiallyOverlapsStart_ClampsToRange()
        {
            var blackoutPeriods = new[]
            {
                new BlackoutPeriod { Start = new DateOnly(2024, 12, 29), End = new DateOnly(2025, 1, 2) }
            };

            var result = blackoutPeriods.GetBlackoutDayIndices(
                new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc));

            Assert.That(result, Is.EquivalentTo([0, 1]));
        }

        [Test]
        public void GetBlackoutDayIndices_BlackoutPartiallyOverlapsEnd_ClampsToRange()
        {
            var blackoutPeriods = new[]
            {
                new BlackoutPeriod { Start = new DateOnly(2025, 1, 9), End = new DateOnly(2025, 1, 15) }
            };

            var result = blackoutPeriods.GetBlackoutDayIndices(
                new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc));

            Assert.That(result, Is.EquivalentTo([8, 9]));
        }

        [Test]
        public void GetBlackoutDayIndices_BlackoutOutsideRange_ReturnsEmpty()
        {
            var blackoutPeriods = new[]
            {
                new BlackoutPeriod { Start = new DateOnly(2025, 2, 1), End = new DateOnly(2025, 2, 5) }
            };

            var result = blackoutPeriods.GetBlackoutDayIndices(
                new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc));

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetBlackoutDayIndices_MultipleBlackoutPeriods_ReturnsCombinedIndices()
        {
            var blackoutPeriods = new[]
            {
                new BlackoutPeriod { Start = new DateOnly(2025, 1, 2), End = new DateOnly(2025, 1, 3) },
                new BlackoutPeriod { Start = new DateOnly(2025, 1, 7), End = new DateOnly(2025, 1, 8) }
            };

            var result = blackoutPeriods.GetBlackoutDayIndices(
                new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc));

            Assert.That(result, Is.EquivalentTo([1, 2, 6, 7]));
        }

        [Test]
        public void IsBlackoutDay_DateOnly_WithinBlackoutPeriod_ReturnsTrue()
        {
            var blackoutPeriods = new[]
            {
                new BlackoutPeriod { Start = new DateOnly(2025, 1, 3), End = new DateOnly(2025, 1, 5) }
            };

            Assert.That(blackoutPeriods.IsBlackoutDay(new DateOnly(2025, 1, 4)), Is.True);
        }

        [Test]
        public void IsBlackoutDay_DateOnly_OutsideBlackoutPeriod_ReturnsFalse()
        {
            var blackoutPeriods = new[]
            {
                new BlackoutPeriod { Start = new DateOnly(2025, 1, 3), End = new DateOnly(2025, 1, 5) }
            };

            Assert.That(blackoutPeriods.IsBlackoutDay(new DateOnly(2025, 1, 6)), Is.False);
        }

        [Test]
        public void IsBlackoutDay_DateOnly_OnBoundary_ReturnsTrue()
        {
            var blackoutPeriods = new[]
            {
                new BlackoutPeriod { Start = new DateOnly(2025, 1, 3), End = new DateOnly(2025, 1, 5) }
            };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(blackoutPeriods.IsBlackoutDay(new DateOnly(2025, 1, 3)), Is.True);
                Assert.That(blackoutPeriods.IsBlackoutDay(new DateOnly(2025, 1, 5)), Is.True);
            }
        }

        [Test]
        public void IsBlackoutDay_DateTime_WithinBlackoutPeriod_ReturnsTrue()
        {
            var blackoutPeriods = new[]
            {
                new BlackoutPeriod { Start = new DateOnly(2025, 1, 3), End = new DateOnly(2025, 1, 5) }
            };

            Assert.That(blackoutPeriods.IsBlackoutDay(new DateTime(2025, 1, 4, 14, 30, 0, DateTimeKind.Utc)), Is.True);
        }

        [Test]
        public void IsBlackoutDay_NoBlackoutPeriods_ReturnsFalse()
        {
            Assert.That(Array.Empty<BlackoutPeriod>().IsBlackoutDay(new DateOnly(2025, 1, 4)), Is.False);
        }

        [Test]
        public void HasOverlapWithDateRange_NoBlackoutPeriods_ReturnsFalse()
        {
            var result = Array.Empty<BlackoutPeriod>().HasOverlapWithDateRange(
                new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 1, 30, 0, 0, 0, DateTimeKind.Utc));

            Assert.That(result, Is.False);
        }

        [Test]
        public void HasOverlapWithDateRange_BlackoutFullyWithinRange_ReturnsTrue()
        {
            var blackoutPeriods = new[]
            {
                new BlackoutPeriod { Start = new DateOnly(2025, 1, 10), End = new DateOnly(2025, 1, 15) }
            };

            var result = blackoutPeriods.HasOverlapWithDateRange(
                new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 1, 30, 0, 0, 0, DateTimeKind.Utc));

            Assert.That(result, Is.True);
        }

        [Test]
        public void HasOverlapWithDateRange_BlackoutPartiallyOverlapsStart_ReturnsTrue()
        {
            var blackoutPeriods = new[]
            {
                new BlackoutPeriod { Start = new DateOnly(2024, 12, 28), End = new DateOnly(2025, 1, 3) }
            };

            var result = blackoutPeriods.HasOverlapWithDateRange(
                new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 1, 30, 0, 0, 0, DateTimeKind.Utc));

            Assert.That(result, Is.True);
        }

        [Test]
        public void HasOverlapWithDateRange_BlackoutPartiallyOverlapsEnd_ReturnsTrue()
        {
            var blackoutPeriods = new[]
            {
                new BlackoutPeriod { Start = new DateOnly(2025, 1, 28), End = new DateOnly(2025, 2, 5) }
            };

            var result = blackoutPeriods.HasOverlapWithDateRange(
                new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 1, 30, 0, 0, 0, DateTimeKind.Utc));

            Assert.That(result, Is.True);
        }

        [Test]
        public void HasOverlapWithDateRange_BlackoutOutsideRange_ReturnsFalse()
        {
            var blackoutPeriods = new[]
            {
                new BlackoutPeriod { Start = new DateOnly(2025, 3, 1), End = new DateOnly(2025, 3, 10) }
            };

            var result = blackoutPeriods.HasOverlapWithDateRange(
                new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 1, 30, 0, 0, 0, DateTimeKind.Utc));

            Assert.That(result, Is.False);
        }

        [Test]
        public void HasOverlapWithDateRange_BlackoutOnBoundaryDay_ReturnsTrue()
        {
            var blackoutPeriods = new[]
            {
                new BlackoutPeriod { Start = new DateOnly(2025, 1, 30), End = new DateOnly(2025, 1, 30) }
            };

            var result = blackoutPeriods.HasOverlapWithDateRange(
                new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 1, 30, 0, 0, 0, DateTimeKind.Utc));

            Assert.That(result, Is.True);
        }

        [Test]
        public void HasOverlapWithDateRange_MultiplePeriodsOneOverlaps_ReturnsTrue()
        {
            var blackoutPeriods = new[]
            {
                new BlackoutPeriod { Start = new DateOnly(2025, 3, 1), End = new DateOnly(2025, 3, 5) },
                new BlackoutPeriod { Start = new DateOnly(2025, 1, 10), End = new DateOnly(2025, 1, 12) }
            };

            var result = blackoutPeriods.HasOverlapWithDateRange(
                new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 1, 30, 0, 0, 0, DateTimeKind.Utc));

            Assert.That(result, Is.True);
        }
    }
}
