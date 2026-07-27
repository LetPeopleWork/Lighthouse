using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Tests.TestDoubles;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    public class BaselineValidationServiceTest
    {
        [Test]
        public void Validate_BothDatesNull_ReturnsValid()
        {
            var result = BaselineValidationService.Validate(null, null, 180, TestToday.Ambient);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_StartDateSetEndDateNull_ReturnsInvalid()
        {
            var start = DateTime.UtcNow.Date.AddDays(-30);

            var result = BaselineValidationService.Validate(start, null, 180, TestToday.Ambient);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.ErrorMessage, Does.Contain("both"));
            }
        }

        [Test]
        public void Validate_StartDateNullEndDateSet_ReturnsInvalid()
        {
            var end = DateTime.UtcNow.Date.AddDays(-1);

            var result = BaselineValidationService.Validate(null, end, 180, TestToday.Ambient);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.ErrorMessage, Does.Contain("both"));
            }
        }

        [Test]
        public void Validate_EndDateBeforeStartDate_ReturnsInvalid()
        {
            var start = DateTime.UtcNow.Date.AddDays(-10);
            var end = DateTime.UtcNow.Date.AddDays(-20);

            var result = BaselineValidationService.Validate(start, end, 180, TestToday.Ambient);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.ErrorMessage, Does.Contain("after"));
            }
        }

        [Test]
        public void Validate_BaselineShorterThan14Days_ReturnsInvalid()
        {
            var start = DateTime.UtcNow.Date.AddDays(-10);
            var end = DateTime.UtcNow.Date.AddDays(-1);

            var result = BaselineValidationService.Validate(start, end, 180, TestToday.Ambient);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.ErrorMessage, Does.Contain("14"));
            }
        }

        [Test]
        public void Validate_BaselineExactly14Days_ReturnsValid()
        {
            var start = DateTime.UtcNow.Date.AddDays(-15);
            var end = DateTime.UtcNow.Date.AddDays(-1);

            var result = BaselineValidationService.Validate(start, end, 180, TestToday.Ambient);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_EndDateInFuture_ReturnsInvalid()
        {
            var start = DateTime.UtcNow.Date.AddDays(-30);
            var end = DateTime.UtcNow.Date.AddDays(5);

            var result = BaselineValidationService.Validate(start, end, 180, TestToday.Ambient);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.ErrorMessage, Does.Contain("future"));
            }
        }

        /// <summary>
        /// Bug #5567: 20:00 UTC is already the next day in Auckland. A baseline ending on the
        /// instance's own day is not in the future, however far behind UTC still is.
        /// </summary>
        [Test]
        public void Validate_EndDateIsTheInstanceDayWhileUtcIsStillYesterday_ReturnsValid()
        {
            var clock = new FakeLighthouseClock(
                new DateTimeOffset(2026, 3, 10, 20, 0, 0, TimeSpan.Zero),
                TimeZoneInfo.FindSystemTimeZoneById("Pacific/Auckland"));

            var start = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = new DateTime(2026, 3, 11, 0, 0, 0, DateTimeKind.Utc);

            var result = BaselineValidationService.Validate(start, end, 180, clock.Today);

            Assert.That(result.IsValid, Is.True);
        }

        /// <summary>
        /// The cutoff window is counted in instance days too, so the oldest still-covered day is the
        /// same one the read paths report rather than one UTC day off.
        /// </summary>
        [Test]
        public void Validate_StartDateOnTheCutoffDayInTheInstanceZone_ReturnsValid()
        {
            var clock = new FakeLighthouseClock(
                new DateTimeOffset(2026, 3, 10, 20, 0, 0, TimeSpan.Zero),
                TimeZoneInfo.FindSystemTimeZoneById("Pacific/Auckland"));

            var start = new DateTime(2025, 12, 11, 0, 0, 0, DateTimeKind.Utc);
            var end = new DateTime(2026, 3, 11, 0, 0, 0, DateTimeKind.Utc);

            var result = BaselineValidationService.Validate(start, end, 90, clock.Today);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_BaselineOutsideCutoff_ReturnsInvalid()
        {
            var start = DateTime.UtcNow.Date.AddDays(-200);
            var end = DateTime.UtcNow.Date.AddDays(-180);

            var result = BaselineValidationService.Validate(start, end, 180, TestToday.Ambient);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.ErrorMessage, Does.Contain("cutoff"));
            }
        }

        [Test]
        public void Validate_CutOffSetToZero_ReturnsValid()
        {
            // CutOff = 0 means full history
            var start = DateTime.UtcNow.Date.AddDays(-60);
            var end = DateTime.UtcNow.Date.AddDays(-30);

            var result = BaselineValidationService.Validate(start, end, 0, TestToday.Ambient);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_ValidBaseline_ReturnsValid()
        {
            var start = DateTime.UtcNow.Date.AddDays(-60);
            var end = DateTime.UtcNow.Date.AddDays(-30);

            var result = BaselineValidationService.Validate(start, end, 180, TestToday.Ambient);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_BaselineStartAtCutoffBoundary_ReturnsValid()
        {
            var start = DateTime.UtcNow.Date.AddDays(-179);
            var end = DateTime.UtcNow.Date.AddDays(-1);

            var result = BaselineValidationService.Validate(start, end, 180, TestToday.Ambient);

            Assert.That(result.IsValid, Is.True);
        }
    }
}
