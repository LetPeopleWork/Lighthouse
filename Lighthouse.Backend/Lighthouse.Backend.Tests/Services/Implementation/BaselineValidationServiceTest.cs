using Lighthouse.Backend.Services.Implementation;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    public class BaselineValidationServiceTest
    {
        [Test]
        public void Validate_BothDatesNull_ReturnsValid()
        {
            var result = BaselineValidationService.Validate(null, null, 180);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_StartDateSetEndDateNull_ReturnsInvalid()
        {
            var start = DateTime.UtcNow.Date.AddDays(-30);

            var result = BaselineValidationService.Validate(start, null, 180);

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

            var result = BaselineValidationService.Validate(null, end, 180);

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

            var result = BaselineValidationService.Validate(start, end, 180);

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

            var result = BaselineValidationService.Validate(start, end, 180);

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

            var result = BaselineValidationService.Validate(start, end, 180);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_EndDateInFuture_ReturnsInvalid()
        {
            var start = DateTime.UtcNow.Date.AddDays(-30);
            var end = DateTime.UtcNow.Date.AddDays(5);

            var result = BaselineValidationService.Validate(start, end, 180);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.ErrorMessage, Does.Contain("future"));
            }
        }

        [Test]
        public void Validate_BaselineOutsideCutoff_ReturnsInvalid()
        {
            var start = DateTime.UtcNow.Date.AddDays(-200);
            var end = DateTime.UtcNow.Date.AddDays(-180);

            var result = BaselineValidationService.Validate(start, end, 180);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.ErrorMessage, Does.Contain("cutoff"));
            }
        }

        [Test]
        public void Validate_ValidBaseline_ReturnsValid()
        {
            var start = DateTime.UtcNow.Date.AddDays(-60);
            var end = DateTime.UtcNow.Date.AddDays(-30);

            var result = BaselineValidationService.Validate(start, end, 180);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_BaselineStartAtCutoffBoundary_ReturnsValid()
        {
            var start = DateTime.UtcNow.Date.AddDays(-179);
            var end = DateTime.UtcNow.Date.AddDays(-1);

            var result = BaselineValidationService.Validate(start, end, 180);

            Assert.That(result.IsValid, Is.True);
        }
    }
}
