using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Staleness;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.DTO
{
    [TestFixture]
    [Category("epic-5074-blocked-items")]
    public class WorkItemDtoTests
    {
        [Test]
        public void Approximate_IsFalse_ByDefault()
        {
            var workItem = new WorkItem
            {
                Name = "Test Item",
                ReferenceId = "PHX-1",
                State = "In Progress",
                StateCategory = StateCategories.Doing,
            };

            var dto = new WorkItemDto(workItem, TestToday.Clock, isBlocked: false);

            Assert.That(dto.Approximate, Is.False,
                "Approximate must be false by default for non-extrapolated items");
        }

        [Test]
        public void Approximate_PersistsFalse_WhenBlockedSinceProvided()
        {
            var workItem = new WorkItem
            {
                Name = "Blocked Item",
                ReferenceId = "PHX-2",
                State = "Blocked",
                StateCategory = StateCategories.Doing,
                CurrentStateEnteredAt = new DateTime(2026, 6, 10, 8, 0, 0, DateTimeKind.Utc),
            };

            var blockedSince = new DateTime(2026, 6, 10, 8, 0, 0, DateTimeKind.Utc);
            var dto = new WorkItemDto(workItem, TestToday.Clock, isBlocked: true, [], blockedSince);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.Approximate, Is.False,
                    "Approximate must remain false even when blockedSince is provided");
                Assert.That(dto.IsBlocked, Is.True);
                Assert.That(dto.BlockedSince, Is.EqualTo(blockedSince));
                Assert.That(dto.BlockedSince, Is.Not.Null);
            }
        }

        [Test]
        public void BlockedSince_IsNull_WhenItemIsNotBlocked()
        {
            var workItem = new WorkItem
            {
                Name = "Active Item",
                ReferenceId = "PHX-3",
                State = "In Progress",
                StateCategory = StateCategories.Doing,
                CurrentStateEnteredAt = new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc),
            };

            var dto = new WorkItemDto(workItem, TestToday.Clock, isBlocked: false, [], null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.IsBlocked, Is.False);
                Assert.That(dto.BlockedSince, Is.Null);
                Assert.That(dto.Approximate, Is.False);
            }
        }

        [Test]
        [Category("story-6247")]
        public void CarryStaleness_JudgesTheStateAndBlockedStartTheDtoCarries()
        {
            var enteredAndBlocked = new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc);
            var workItem = new WorkItem
            {
                Name = "Blocked Item",
                ReferenceId = "PHX-4",
                State = "Blocked",
                StateCategory = StateCategories.Doing,
                CurrentStateEnteredAt = enteredAndBlocked,
            };
            var team = new Team { StalenessThresholdDays = 5, BlockedStalenessThresholdDays = 3 };
            var dto = new WorkItemDto(workItem, TestToday.Clock, isBlocked: true, [], enteredAndBlocked);

            dto.CarryStaleness(StalenessPolicy.For(team), TestToday.Zone, new DateOnly(2026, 6, 10));

            var expectedReasons = new List<StaleReasonDto>
            {
                new(StaleReasonKind.BlockedDuration, 10, null),
                new(StaleReasonKind.TimeInStateWhileBlocked, 10, "Blocked"),
            };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.IsStale, Is.True);
                Assert.That(dto.StaleReasons, Is.EqualTo(expectedReasons));
            }
        }

        [Test]
        public void StartedDate_FallsBackToCreatedDate_WhenAnItemInProgressHasNoStartDate()
        {
            var created = new DateTime(2026, 5, 4, 9, 0, 0, DateTimeKind.Utc);
            var workItem = AnItem(StateCategories.Doing, created, startedDate: null);

            var dto = new WorkItemDto(workItem, TestToday.Clock, isBlocked: false);

            Assert.That(dto.StartedDate, Is.EqualTo(created),
                "An in-progress item without a start date must reach the client with its created date as start date, the date its age is counted from; otherwise the client sums NaN.");
        }

        [Test]
        public void StartedDate_KeepsTheItemsOwnStartDate_WhenItHasOne()
        {
            var created = new DateTime(2026, 5, 4, 9, 0, 0, DateTimeKind.Utc);
            var started = new DateTime(2026, 5, 8, 9, 0, 0, DateTimeKind.Utc);
            var workItem = AnItem(StateCategories.Doing, created, started);

            var dto = new WorkItemDto(workItem, TestToday.Clock, isBlocked: false);

            Assert.That(dto.StartedDate, Is.EqualTo(started),
                "An item that has a start date keeps it; the created date is only a fallback.");
        }

        [Test]
        public void StartedDate_StaysEmpty_WhenAToDoItemHasNoStartDate()
        {
            var created = new DateTime(2026, 5, 4, 9, 0, 0, DateTimeKind.Utc);
            var workItem = AnItem(StateCategories.ToDo, created, startedDate: null);

            var dto = new WorkItemDto(workItem, TestToday.Clock, isBlocked: false);

            Assert.That(dto.StartedDate, Is.Null,
                "An item that has not started must not be shown as started.");
        }

        private static WorkItem AnItem(StateCategories category, DateTime created, DateTime? startedDate)
        {
            return new WorkItem
            {
                Name = "Item",
                ReferenceId = "PHX-5",
                State = category == StateCategories.Doing ? "In Progress" : "New",
                StateCategory = category,
                CreatedDate = created,
                StartedDate = startedDate,
            };
        }
    }
}
