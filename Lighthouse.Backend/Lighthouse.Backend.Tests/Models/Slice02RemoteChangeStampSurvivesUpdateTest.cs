using Lighthouse.Backend.Models;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Models
{
    /// <summary>
    /// DISTILL acceptance specification (Epic 5687 — Faster Updates), slice 02, AC-2.7 / D6.
    ///
    /// Asserted directly rather than through the refresh, because this is the failure with no symptom:
    /// the copy path silently drops members that are not plain settable properties, and a dropped remote
    /// change stamp degrades every later refresh back to a full download while every other test in the
    /// suite stays green. The repository has already been bitten once by this shape — a `[NotMapped]`
    /// init-only member dropped by the copy constructor (docs/ci-learnings.md, work-item-sync).
    /// </summary>
    [TestFixture]
    [Category("epic-5687-faster-updates")]
    [Category("slice-02")]
    public class Slice02RemoteChangeStampSurvivesUpdateTest
    {
        private static readonly DateTime WhenItLastChanged = new(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc);

        // @unit @AC-2.7 @D6 @contract-shape:bounded-change
        [Test]
        [Ignore("DISTILL scaffold — DELIVER un-ignores this specification when it implements it.")]
        public void An_issue_that_is_refreshed_keeps_the_day_the_tracker_says_it_last_changed()
        {
            var team = ATeam();
            var stored = new WorkItem(AnIssue(), team) { LastChangedRemote = WhenItLastChanged.AddDays(-1) };
            var incoming = new WorkItem(AnIssue(), team) { LastChangedRemote = WhenItLastChanged };

            stored.Update(incoming);

            Assert.That(stored.LastChangedRemote, Is.EqualTo(WhenItLastChanged),
                "Losing the stamp on refresh means the next cycle finds nothing to compare and downloads everything - "
                + "a performance regression with every other test still green.");
        }

        // @unit @AC-2.7 @D6 @contract-shape:pure-function
        [Test]
        [Ignore("DISTILL scaffold — DELIVER un-ignores this specification when it implements it.")]
        public void An_issue_copied_from_what_the_tracker_returned_keeps_the_day_it_last_changed()
        {
            var fromTheTracker = AnIssue();
            fromTheTracker.LastChangedRemote = WhenItLastChanged;

            var copied = new WorkItem(fromTheTracker, ATeam());

            Assert.That(copied.LastChangedRemote, Is.EqualTo(WhenItLastChanged),
                "The copy constructor is how a connector's payload becomes a stored issue; a stamp lost here never reaches storage.");
        }

        private static WorkItemBase AnIssue() => new()
        {
            ReferenceId = "ITEM-1",
            Name = "An issue",
            Type = "Story",
            State = "In Progress",
            StateCategory = StateCategories.Doing,
            Order = "1",
            ParentReferenceId = string.Empty,
        };

        private static Team ATeam() => new() { Name = "A team" };
    }
}
