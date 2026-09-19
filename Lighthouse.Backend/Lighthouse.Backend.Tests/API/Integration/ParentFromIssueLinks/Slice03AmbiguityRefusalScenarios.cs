using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.ParentFromIssueLinks
{
    /// <summary>
    /// Acceptance scenarios - Slice 03: what a refresh does with an item whose links offer it more than
    /// one parent. Driving port: a Team refresh, which is what actually writes the parent every forecast
    /// is drawn from.
    ///
    /// An item hangs under one thing, so several candidates cannot be narrowed by keeping one of them.
    /// There is no tie-break here and none is wanted: the first one seen, the one linked most recently and
    /// the one whose key sorts first are all guesses, and a guessed parent moves work under something it
    /// does not belong to while looking exactly like correct data. Refusing leaves the item where it was,
    /// which is visibly unfinished rather than invisibly wrong.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6028-parent-from-issue-links")]
    [Category("slice-03")]
    public partial class Slice03AmbiguityRefusalTest
    {
        /// <summary>
        /// Two candidates and no parent. Emptiness is what refuses every tie-break at once - whichever of
        /// the two a rule would have reached for, it is not there.
        /// </summary>
        [Test]
        public async Task An_item_with_two_distinct_candidates_takes_neither_of_them()
        {
            TheParentOverrideNames(TheLinkTypeTheOverrideNames.Name);
            TheIssueHasLinksOfTheNamedTypeTo(TheAmbiguousItem, OneCandidate, TheOtherCandidate);

            var refreshed = await TheTeamIsRefreshed();

            Assert.That(TheParentOf(refreshed, TheAmbiguousItem), Is.Empty,
                "Picking one of two candidates files the item under something it may not belong to, inflates the size of that thing and every forecast drawn from it, and reads on the screen exactly like a parent somebody configured correctly.");
        }

        /// <summary>
        /// A tracker that recorded one relationship twice is untidy, not ambiguous. Counting entries
        /// instead of the issues they name would refuse a parent that is not in doubt at all, and leave an
        /// administrator correcting links that were never wrong.
        /// </summary>
        [Test]
        public async Task Two_links_to_the_same_issue_are_one_candidate()
        {
            TheParentOverrideNames(TheLinkTypeTheOverrideNames.Name);
            TheIssueHasTwoLinksOfTheNamedTypeToTheSameIssue(TheAmbiguousItem, OneCandidate);

            var refreshed = await TheTeamIsRefreshed();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheParentOf(refreshed, TheAmbiguousItem), Is.EqualTo(OneCandidate),
                    "Both links name the same issue, so there is nothing to choose between - and an item left unparented here would send somebody hunting for a second parent that does not exist.");
                NothingWasWrittenToTheLogAsAWarning();
            }
        }

        /// <summary>
        /// One item Lighthouse cannot place must cost only that item. A refusal that stopped the fetch
        /// would let a single untidy item in a tracker of thousands take the whole Team's hierarchy down
        /// with it.
        /// </summary>
        [Test]
        public async Task An_item_that_cannot_be_placed_costs_only_itself()
        {
            TheParentOverrideNames(TheLinkTypeTheOverrideNames.Name);
            TheIssueHasLinksOfTheNamedTypeTo(TheAmbiguousItem, OneCandidate, TheOtherCandidate);
            TheIssueHasLinksOfTheNamedTypeTo(TheItemWithOneCandidate, TheParentTheItemWithOneCandidateTakes);

            var refreshed = await TheTeamIsRefreshed();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheParentOf(refreshed, TheItemWithOneCandidate), Is.EqualTo(TheParentTheItemWithOneCandidateTakes),
                    "The item beside it says exactly one thing about its parent, and there is no reason a neighbour nobody could place should cost it the parent it names.");
                Assert.That(WhichItemsCameBack(refreshed), Is.EquivalentTo(new[] { TheAmbiguousItem, TheItemWithOneCandidate }),
                    "A refresh that gave up part way leaves a hierarchy that simply stops moving, and the item that could not be placed still has to come back so that what is already stored for it is not lost.");
            }
        }
    }
}
