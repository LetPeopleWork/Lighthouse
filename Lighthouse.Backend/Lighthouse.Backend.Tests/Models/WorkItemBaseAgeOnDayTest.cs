using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    /// <summary>
    /// Story 5508 (widget-loose-ends) slice 03 — <see cref="WorkItemBase.AgeOnDay"/>.
    ///
    /// DESIGN D13: AgeOnDay answers "how old was this item on the given day", which is a DIFFERENT
    /// question from the <see cref="WorkItemBase.WorkItemAge"/> property ("how old is it right now,
    /// and only if it is Doing right now"). AgeOnDay therefore carries NO StateCategory guard — the
    /// callers establish the population via WasItemProgressOnDay before projecting. The two must not
    /// be refactored into one another.
    ///
    /// The arithmetic is pinned against the shipped reference in BaseMetricsService.GenerateTotalWorkItemAgeByDay:
    ///     age = (day - (StartedDate ?? CreatedDate)) + 1
    /// </summary>
    [TestFixture]
    public class WorkItemBaseAgeOnDayTest
    {
        private static readonly DateTime Jul01 = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Jul04 = new DateTime(2026, 7, 4, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Jul06 = new DateTime(2026, 7, 6, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Jul10 = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);

        [Test]
        public void AgeOnDay_ItemStartedBeforeTheDayAndStillOpen_ReturnsInclusiveDayCount()
        {
            var item = new WorkItemBase { StartedDate = Jul01, StateCategory = StateCategories.Doing };

            Assert.That(item.AgeOnDay(Jul04), Is.EqualTo(4));
        }

        [Test]
        public void AgeOnDay_ItemHasSinceClosed_StillAgesToTheRequestedDay_NotToItsClosedDate()
        {
            // The defect this fixes: WorkItemAge returns 0 for anything not Doing *right now*, so a
            // historically-in-progress item that has closed since was zeroed and then dropped by the
            // `age > 0` filter. AgeOnDay carries no such guard.
            var closedSince = new WorkItemBase
            {
                StartedDate = Jul01,
                ClosedDate = Jul06,
                StateCategory = StateCategories.Done,
            };

            Assert.That(closedSince.AgeOnDay(Jul04), Is.EqualTo(4));
        }

        [Test]
        public void AgeOnDay_NoStartedDate_FallsBackToCreatedDate()
        {
            var item = new WorkItemBase { CreatedDate = Jul01, StateCategory = StateCategories.Doing };

            Assert.That(item.AgeOnDay(Jul04), Is.EqualTo(4));
        }

        [Test]
        public void AgeOnDay_StartedDatePreferredOverCreatedDate()
        {
            var item = new WorkItemBase
            {
                CreatedDate = Jul01,
                StartedDate = Jul04,
                StateCategory = StateCategories.Doing,
            };

            Assert.That(item.AgeOnDay(Jul06), Is.EqualTo(3));
        }

        [Test]
        public void AgeOnDay_StartedOnTheDayItself_ReturnsOne()
        {
            var item = new WorkItemBase { StartedDate = Jul04, StateCategory = StateCategories.Doing };

            Assert.That(item.AgeOnDay(Jul04), Is.EqualTo(1));
        }

        [Test]
        public void AgeOnDay_StartedAfterTheRequestedDay_ReturnsZero()
        {
            // DESIGN open question 1 (the `age > 0` guard disposition) is resolved HERE: AgeOnDay
            // returns 0 — never a negative and never a fabricated 1 — for an item that had not started
            // on the requested day. That keeps `age > 0` meaningful at the call sites (it filters
            // not-yet-started and bad data) instead of accidentally doubling as the "not Doing" filter
            // it used to be. Because valid ages are always >= 1, no legitimate item is dropped.
            var notYetStarted = new WorkItemBase { StartedDate = Jul10, StateCategory = StateCategories.Doing };

            Assert.That(notYetStarted.AgeOnDay(Jul04), Is.EqualTo(0));
        }

        [Test]
        public void AgeOnDay_NeitherStartedNorCreatedDate_ReturnsZero()
        {
            var undated = new WorkItemBase { StateCategory = StateCategories.Doing };

            Assert.That(undated.AgeOnDay(Jul04), Is.EqualTo(0));
        }

        [Test]
        public void AgeOnDay_DoesNotAlterTheWorkItemAgeProperty()
        {
            // CI5: write-back consumes the WorkItemAge property (WriteBackTriggerService.cs:188-205).
            // A Done item must keep reporting 0 there no matter what AgeOnDay says.
            var closedItem = new WorkItemBase
            {
                StartedDate = Jul01,
                ClosedDate = Jul06,
                StateCategory = StateCategories.Done,
            };

            Assert.Multiple(() =>
            {
                Assert.That(closedItem.WorkItemAge, Is.EqualTo(0), "WorkItemAge must stay today-anchored and Doing-guarded");
                Assert.That(closedItem.AgeOnDay(Jul04), Is.EqualTo(4), "AgeOnDay answers the as-of-day question");
            });
        }
    }
}
