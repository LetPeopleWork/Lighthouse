using Lighthouse.Backend.API;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    [TestFixture]
    public class RecurringBlackoutRulesControllerTest
    {
        private Mock<IRecurringBlackoutRuleService> serviceMock;
        private RecurringBlackoutRulesController subject;

        [SetUp]
        public void SetUp()
        {
            serviceMock = new Mock<IRecurringBlackoutRuleService>();
            subject = new RecurringBlackoutRulesController(serviceMock.Object);
        }

        [Test]
        public void Constructor_NullService_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new RecurringBlackoutRulesController(null!));
        }

        [Test]
        public void GetAll_ServiceReturnsRules_ReturnsOkWithRules()
        {
            var rules = new[] { ValidDto() };
            serviceMock.Setup(s => s.GetAll()).Returns(rules);

            var result = subject.GetAll();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult!.Value, Is.EqualTo(rules));
            }
        }

        [Test]
        public async Task Create_ServiceThrowsArgumentException_ReturnsBadRequestWithMessage()
        {
            var dto = ValidDto();
            serviceMock.Setup(s => s.Create(dto)).ThrowsAsync(new ArgumentException("Repeat interval must be at least 1 week."));

            var result = await subject.Create(dto);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
                var badRequest = result as BadRequestObjectResult;
                Assert.That(badRequest!.Value, Is.EqualTo("Repeat interval must be at least 1 week."));
            }
        }

        [Test]
        public async Task Update_ServiceThrowsKeyNotFound_ReturnsNotFoundWithMessage()
        {
            var dto = ValidDto();
            serviceMock.Setup(s => s.Update(99, dto)).ThrowsAsync(new KeyNotFoundException("Recurring blackout rule with id 99 not found."));

            var result = await subject.Update(99, dto);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
                var notFound = result as NotFoundObjectResult;
                Assert.That(notFound!.Value, Is.EqualTo("Recurring blackout rule with id 99 not found."));
            }
        }

        [Test]
        public async Task Update_ServiceThrowsArgumentException_ReturnsBadRequest()
        {
            var dto = ValidDto();
            serviceMock.Setup(s => s.Update(1, dto)).ThrowsAsync(new ArgumentException("Select at least one weekday for the rule to repeat on."));

            var result = await subject.Update(1, dto);

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task Delete_ServiceThrowsKeyNotFound_ReturnsNotFound()
        {
            serviceMock.Setup(s => s.Delete(99)).ThrowsAsync(new KeyNotFoundException("Recurring blackout rule with id 99 not found."));

            var result = await subject.Delete(99);

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        private static RecurringBlackoutRuleDto ValidDto()
        {
            return new RecurringBlackoutRuleDto
            {
                Weekdays = [DayOfWeek.Monday],
                IntervalWeeks = 2,
                Start = new DateOnly(2026, 6, 1),
                End = null,
                Description = "Rule",
            };
        }
    }
}
