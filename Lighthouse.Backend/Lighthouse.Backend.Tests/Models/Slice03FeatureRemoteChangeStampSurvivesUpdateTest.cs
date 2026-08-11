using Lighthouse.Backend.Models;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Models
{
    /// <summary>
    /// DISTILL acceptance specification (Epic 5687 — Faster Updates), slice 03, D6 for the Feature.
    ///
    /// The Feature-side twin of <c>Slice02RemoteChangeStampSurvivesUpdateTest</c>. <c>Feature.Update(…)</c>
    /// is its own method — it calls <c>base.Update(…)</c> and then adds the two members only a Feature has
    /// — so the promise slice 02 pinned on the work item does not automatically hold here, and its failure
    /// mode is the same silent one: a dropped stamp degrades every later portfolio cycle back to a full
    /// download with every other test in the suite still green.
    ///
    /// This is expected GREEN off shipped code: slice 02's one-line change lives in the base method both
    /// copy paths run through. It ships as a regression guard on that reuse, and is recorded as a guard
    /// rather than passed off as a red.
    /// </summary>
    [TestFixture]
    [Category("epic-5687-faster-updates")]
    [Category("slice-03")]
    public class Slice03FeatureRemoteChangeStampSurvivesUpdateTest
    {
        private static readonly DateTime WhenItLastChanged = new(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc);

        // @unit @AC-3.1 @D6 @contract-shape:bounded-change
        [Test]
        public void A_feature_that_is_refreshed_keeps_the_day_the_tracker_says_it_last_changed()
        {
            var stored = new Feature(AFeature()) { LastChangedRemote = WhenItLastChanged.AddDays(-1) };
            var incoming = new Feature(AFeature()) { LastChangedRemote = WhenItLastChanged };

            stored.Update(incoming);

            Assert.That(stored.LastChangedRemote, Is.EqualTo(WhenItLastChanged),
                "Losing the stamp when a Feature is refreshed means the next portfolio cycle finds nothing to compare and "
                + "downloads every Feature again - the saving handed straight back, with every other test still green.");
        }

        // @unit @AC-3.1 @D6 @contract-shape:pure-function
        [Test]
        public void A_feature_copied_from_what_the_tracker_returned_keeps_the_day_it_last_changed()
        {
            var fromTheTracker = AFeature();
            fromTheTracker.LastChangedRemote = WhenItLastChanged;

            var copied = new Feature(fromTheTracker);

            Assert.That(copied.LastChangedRemote, Is.EqualTo(WhenItLastChanged),
                "The copy constructor is how a connector's payload becomes a stored Feature; a stamp lost here never reaches storage.");
        }

        private static WorkItemBase AFeature() => new()
        {
            ReferenceId = "FEAT-1",
            Name = "A feature",
            Type = "Epic",
            State = "In Progress",
            StateCategory = StateCategories.Doing,
            Order = "1",
            ParentReferenceId = string.Empty,
        };
    }
}
