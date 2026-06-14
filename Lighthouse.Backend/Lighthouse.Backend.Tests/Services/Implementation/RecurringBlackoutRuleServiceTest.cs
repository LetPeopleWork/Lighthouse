using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    [TestFixture]
    public class RecurringBlackoutRuleServiceTest
    {
        private Mock<IRepository<RecurringBlackoutRule>> repositoryMock;
        private Mock<IDomainEventDispatcher> dispatcherMock;

        [SetUp]
        public void SetUp()
        {
            repositoryMock = new Mock<IRepository<RecurringBlackoutRule>>();
            dispatcherMock = new Mock<IDomainEventDispatcher>();
        }

        [Test]
        public void Constructor_NullRepository_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new RecurringBlackoutRuleService(null!, dispatcherMock.Object));
        }

        private void VerifyBlackoutConfigurationChangedPublished(Times times)
        {
            dispatcherMock.Verify(
                d => d.PublishAsync(It.IsAny<BlackoutConfigurationChanged>(), It.IsAny<CancellationToken>()),
                times);
        }

        [Test]
        public void GetAll_OrdersByStartThenById()
        {
            var rules = new[]
            {
                CreateRule(id: 3, start: new DateOnly(2026, 6, 10)),
                CreateRule(id: 1, start: new DateOnly(2026, 6, 5)),
                CreateRule(id: 5, start: new DateOnly(2026, 6, 5)),
                CreateRule(id: 2, start: new DateOnly(2026, 6, 1)),
            };
            repositoryMock.Setup(r => r.GetAll()).Returns(rules);

            var result = CreateSubject().GetAll().ToList();

            Assert.That(result.Select(dto => dto.Id), Is.EqualTo(new int?[] { 2, 1, 5, 3 }));
        }

        [Test]
        public async Task Create_ValidDto_AddsRuleAndSaves()
        {
            RecurringBlackoutRule? added = null;
            repositoryMock.Setup(r => r.Add(It.IsAny<RecurringBlackoutRule>()))
                .Callback<RecurringBlackoutRule>(rule => added = rule);

            var dto = ValidDto();

            var result = await CreateSubject().Create(dto);

            using (Assert.EnterMultipleScope())
            {
                repositoryMock.Verify(r => r.Add(It.IsAny<RecurringBlackoutRule>()), Times.Once);
                repositoryMock.Verify(r => r.Save(), Times.Once);
                Assert.That(added!.Start, Is.EqualTo(dto.Start));
                Assert.That(result.IntervalWeeks, Is.EqualTo(dto.IntervalWeeks));
                VerifyBlackoutConfigurationChangedPublished(Times.Once());
            }
        }

        [TestCase(0, "Select at least one weekday for the rule to repeat on.", false)]
        [TestCase(1, "Repeat interval must be at least 1 week.", true)]
        public void Create_InvalidDto_ThrowsArgumentExceptionAndDoesNotPersist(int weekdayCount, string expectedMessage, bool zeroInterval)
        {
            var dto = ValidDto();
            dto.Weekdays = weekdayCount == 0 ? [] : [DayOfWeek.Monday];
            dto.IntervalWeeks = zeroInterval ? 0 : dto.IntervalWeeks;

            var exception = Assert.Throws<ArgumentException>(() => CreateSubject().Create(dto).GetAwaiter().GetResult());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(exception!.Message, Is.EqualTo(expectedMessage));
                repositoryMock.Verify(r => r.Add(It.IsAny<RecurringBlackoutRule>()), Times.Never);
                repositoryMock.Verify(r => r.Save(), Times.Never);
                VerifyBlackoutConfigurationChangedPublished(Times.Never());
            }
        }

        [Test]
        public void Create_EndBeforeStart_ThrowsArgumentException()
        {
            var dto = ValidDto();
            dto.Start = new DateOnly(2026, 6, 10);
            dto.End = new DateOnly(2026, 6, 1);

            var exception = Assert.Throws<ArgumentException>(() => CreateSubject().Create(dto).GetAwaiter().GetResult());

            Assert.That(exception!.Message, Is.EqualTo("End date must be on or after the start date."));
        }

        [Test]
        public async Task Update_ExistingRule_AppliesChangesAndSaves()
        {
            var existing = CreateRule(id: 7, start: new DateOnly(2026, 6, 1));
            repositoryMock.Setup(r => r.GetById(7)).Returns(existing);

            var dto = ValidDto();
            dto.Weekdays = [DayOfWeek.Tuesday, DayOfWeek.Thursday];
            dto.IntervalWeeks = 3;
            dto.Start = new DateOnly(2026, 7, 1);
            dto.End = new DateOnly(2026, 12, 1);
            dto.Description = "Updated rule";

            var result = await CreateSubject().Update(7, dto);

            using (Assert.EnterMultipleScope())
            {
                repositoryMock.Verify(r => r.Save(), Times.Once);
                Assert.That(existing.Weekdays, Is.EqualTo(new[] { DayOfWeek.Tuesday, DayOfWeek.Thursday }));
                Assert.That(existing.IntervalWeeks, Is.EqualTo(3));
                Assert.That(existing.Start, Is.EqualTo(new DateOnly(2026, 7, 1)));
                Assert.That(existing.End, Is.EqualTo(new DateOnly(2026, 12, 1)));
                Assert.That(existing.Description, Is.EqualTo("Updated rule"));
                Assert.That(result.Id, Is.EqualTo(7));
                VerifyBlackoutConfigurationChangedPublished(Times.Once());
            }
        }

        [Test]
        public void Update_InvalidDto_ThrowsBeforeTouchingRepository()
        {
            var dto = ValidDto();
            dto.Weekdays = [];

            var exception = Assert.Throws<ArgumentException>(() => CreateSubject().Update(1, dto).GetAwaiter().GetResult());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(exception!.Message, Is.EqualTo("Select at least one weekday for the rule to repeat on."));
                repositoryMock.Verify(r => r.GetById(It.IsAny<int>()), Times.Never);
                repositoryMock.Verify(r => r.Save(), Times.Never);
                VerifyBlackoutConfigurationChangedPublished(Times.Never());
            }
        }

        [Test]
        public void Update_UnknownId_ThrowsKeyNotFoundWithId()
        {
            repositoryMock.Setup(r => r.GetById(42)).Returns((RecurringBlackoutRule?)null);

            var exception = Assert.Throws<KeyNotFoundException>(() => CreateSubject().Update(42, ValidDto()).GetAwaiter().GetResult());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(exception!.Message, Does.Contain("42"));
                Assert.That(exception.Message, Does.Contain("not found"));
                repositoryMock.Verify(r => r.Save(), Times.Never);
                VerifyBlackoutConfigurationChangedPublished(Times.Never());
            }
        }

        [Test]
        public async Task Delete_ExistingRule_RemovesAndSaves()
        {
            repositoryMock.Setup(r => r.Exists(9)).Returns(true);

            await CreateSubject().Delete(9);

            using (Assert.EnterMultipleScope())
            {
                repositoryMock.Verify(r => r.Remove(9), Times.Once);
                repositoryMock.Verify(r => r.Save(), Times.Once);
                VerifyBlackoutConfigurationChangedPublished(Times.Once());
            }
        }

        [Test]
        public void Delete_UnknownId_ThrowsKeyNotFoundWithId()
        {
            repositoryMock.Setup(r => r.Exists(55)).Returns(false);

            var exception = Assert.Throws<KeyNotFoundException>(() => CreateSubject().Delete(55).GetAwaiter().GetResult());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(exception!.Message, Does.Contain("55"));
                Assert.That(exception.Message, Does.Contain("not found"));
                repositoryMock.Verify(r => r.Remove(It.IsAny<int>()), Times.Never);
                repositoryMock.Verify(r => r.Save(), Times.Never);
                VerifyBlackoutConfigurationChangedPublished(Times.Never());
            }
        }

        private RecurringBlackoutRuleService CreateSubject()
        {
            return new RecurringBlackoutRuleService(repositoryMock.Object, dispatcherMock.Object);
        }

        private static RecurringBlackoutRule CreateRule(int id, DateOnly start)
        {
            return new RecurringBlackoutRule
            {
                Id = id,
                Weekdays = [DayOfWeek.Monday],
                IntervalWeeks = 1,
                Start = start,
                End = null,
                Description = $"Rule {id}",
            };
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
