using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
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

            var dto = new WorkItemDto(workItem, isBlocked: false);

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
            var dto = new WorkItemDto(workItem, isBlocked: true, [], blockedSince);

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

            var dto = new WorkItemDto(workItem, isBlocked: false, [], null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.IsBlocked, Is.False);
                Assert.That(dto.BlockedSince, Is.Null);
                Assert.That(dto.Approximate, Is.False);
            }
        }
    }
}
