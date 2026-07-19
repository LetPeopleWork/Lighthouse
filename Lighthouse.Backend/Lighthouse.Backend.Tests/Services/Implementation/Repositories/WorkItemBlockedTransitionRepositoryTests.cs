using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Repositories
{
    public class WorkItemBlockedTransitionRepositoryTests : IntegrationTestBase
    {
        private static readonly DateOnly ReconstructDate = new DateOnly(2026, 6, 15);

        public WorkItemBlockedTransitionRepositoryTests() : base()
        {
        }

        [Test]
        public async Task GetBlockedWorkItemIdsAt_ReturnsWorkItemsWhoseBlockedSpellCoversTheDate()
        {
            var openSpellCovering = await GivenPersistedWorkItem();
            var closedSpellCovering = await GivenPersistedWorkItem();
            var closedSpellBefore = await GivenPersistedWorkItem();
            var enteredAfter = await GivenPersistedWorkItem();
            var enteredOnDate = await GivenPersistedWorkItem();
            var leftOnDate = await GivenPersistedWorkItem();
            // Boundary items pinning the exact interval-overlap inequalities (EnteredAt < startOfNextDate,
            // LeftAt >= startOfDate): entered at the first instant of the NEXT day is NOT covering; left at
            // the first instant of the date IS still covering (date-granular, ADR-099).
            var enteredAtStartOfNextDay = await GivenPersistedWorkItem();
            var leftAtStartOfDate = await GivenPersistedWorkItem();

            var subject = CreateSubject();

            subject.Add(BlockedTransition(openSpellCovering.Id, Utc(2026, 6, 10, 9), null));
            subject.Add(BlockedTransition(closedSpellCovering.Id, Utc(2026, 6, 12, 9), Utc(2026, 6, 17, 9)));
            subject.Add(BlockedTransition(closedSpellBefore.Id, Utc(2026, 6, 5, 9), Utc(2026, 6, 11, 9)));
            subject.Add(BlockedTransition(enteredAfter.Id, Utc(2026, 6, 16, 9), null));
            subject.Add(BlockedTransition(enteredOnDate.Id, Utc(2026, 6, 15, 9), null));
            subject.Add(BlockedTransition(leftOnDate.Id, Utc(2026, 6, 13, 9), Utc(2026, 6, 15, 8)));
            subject.Add(BlockedTransition(enteredAtStartOfNextDay.Id, Utc(2026, 6, 16, 0), null));
            subject.Add(BlockedTransition(leftAtStartOfDate.Id, Utc(2026, 6, 13, 9), Utc(2026, 6, 15, 0)));
            await subject.Save();

            var blockedWorkItemIds = subject.GetBlockedWorkItemIdsAt(ReconstructDate);

            var expected = new[]
            {
                openSpellCovering.Id,
                closedSpellCovering.Id,
                enteredOnDate.Id,
                leftOnDate.Id,
                leftAtStartOfDate.Id,
            };
            Assert.That(blockedWorkItemIds, Is.EquivalentTo(expected));
        }

        [Test]
        public async Task GetBlockedWorkItemIdsAt_ReturnsEachCoveringWorkItemOnce_WhenMultipleTransitionsCoverTheDate()
        {
            var workItem = await GivenPersistedWorkItem();
            var subject = CreateSubject();

            subject.Add(BlockedTransition(workItem.Id, Utc(2026, 6, 10, 9), Utc(2026, 6, 20, 9)));
            subject.Add(BlockedTransition(workItem.Id, Utc(2026, 6, 14, 9), null));
            await subject.Save();

            var blockedWorkItemIds = subject.GetBlockedWorkItemIdsAt(ReconstructDate);

            Assert.That(blockedWorkItemIds, Is.EquivalentTo(new[] { workItem.Id }));
        }

        [Test]
        public async Task GetWorkItemIdsWithBlockedHistory_ReturnsOnlyRequestedItemsThatHaveTransitions_Deduplicated()
        {
            var itemWithOneSpell = await GivenPersistedWorkItem();
            var itemWithTwoSpells = await GivenPersistedWorkItem();
            var itemWithoutHistory = await GivenPersistedWorkItem();
            var itemOutsideTheRequest = await GivenPersistedWorkItem();

            var subject = CreateSubject();

            subject.Add(BlockedTransition(itemWithOneSpell.Id, Utc(2026, 6, 10, 9), Utc(2026, 6, 11, 9)));
            subject.Add(BlockedTransition(itemWithTwoSpells.Id, Utc(2026, 6, 10, 9), Utc(2026, 6, 11, 9)));
            subject.Add(BlockedTransition(itemWithTwoSpells.Id, Utc(2026, 6, 13, 9), null));
            subject.Add(BlockedTransition(itemOutsideTheRequest.Id, Utc(2026, 6, 10, 9), null));
            await subject.Save();

            // The historic blocked read uses this to tell "no blocked spell on that day" from "this item
            // predates blocked capture entirely" — the latter is the only case allowed to fall back to
            // the live rule, so an item wrongly reported as having history would be silently mis-answered.
            var idsWithHistory = subject.GetWorkItemIdsWithBlockedHistory(
                [itemWithOneSpell.Id, itemWithTwoSpells.Id, itemWithoutHistory.Id]);

            var expected = new[] { itemWithOneSpell.Id, itemWithTwoSpells.Id };
            Assert.That(idsWithHistory, Is.EquivalentTo(expected));
        }

        private static WorkItemBlockedTransition BlockedTransition(int workItemId, DateTime enteredAt, DateTime? leftAt)
        {
            return new WorkItemBlockedTransition
            {
                WorkItemId = workItemId,
                EnteredAt = enteredAt,
                LeftAt = leftAt,
            };
        }

        private static DateTime Utc(int year, int month, int day, int hour)
        {
            return new DateTime(year, month, day, hour, 0, 0, DateTimeKind.Utc);
        }

        private async Task<WorkItem> GivenPersistedWorkItem()
        {
            var workTrackingSystemConnection = new WorkTrackingSystemConnection { Name = "Connection", WorkTrackingSystem = WorkTrackingSystems.AzureDevOps };
            workTrackingSystemConnection.Options.Add(new WorkTrackingSystemConnectionOption { Key = "key", Value = "value" });
            var team = new Team { Name = "Name", WorkTrackingSystemConnection = workTrackingSystemConnection };

            var workItemBase = new WorkItemBase { ClosedDate = DateTime.UtcNow, StartedDate = DateTime.UtcNow, Name = "Item", Order = "12", ReferenceId = "1337", State = "Done", StateCategory = StateCategories.Done, Type = "Bug", Url = "https://letpeople.work/1886" };
            var workItem = new WorkItem(workItemBase, team);

            var workItemRepository = new WorkItemRepository(DatabaseContext, Mock.Of<ILogger<WorkItemRepository>>());
            workItemRepository.Add(workItem);
            await workItemRepository.Save();

            return workItem;
        }

        private WorkItemBlockedTransitionRepository CreateSubject()
        {
            return new WorkItemBlockedTransitionRepository(DatabaseContext, Mock.Of<ILogger<WorkItemBlockedTransitionRepository>>());
        }
    }
}
