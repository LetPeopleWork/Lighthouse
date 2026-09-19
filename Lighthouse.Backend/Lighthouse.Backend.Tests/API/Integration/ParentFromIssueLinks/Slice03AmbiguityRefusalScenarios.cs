using Lighthouse.Backend.Models;
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

        /// <summary>
        /// An item that was refused looks on screen exactly like one nobody ever linked, so unless the
        /// refresh says which item it was and what it pointed at, the person who has to go and remove one of
        /// those links cannot find either the item or the links.
        /// </summary>
        [Test]
        public async Task The_warning_names_the_item_and_every_candidate()
        {
            TheParentOverrideNames(TheLinkTypeTheOverrideNames.Name);
            TheIssueHasLinksOfTheNamedTypeTo(TheAmbiguousItem, OneCandidate, TheOtherCandidate);

            await TheTeamIsRefreshed();

            Assert.That(
                TheOneWarningTheRefreshWrote(),
                Does.Contain(TheAmbiguousItem).And.Contain(OneCandidate).And.Contain(TheOtherCandidate),
                "Naming the item without its candidates sends somebody to open it in the tracker and read the links back for themselves, and naming a number instead of either tells them only that there is work to do somewhere.");
        }

        /// <summary>
        /// A bulk edit that went wrong produces these by the hundred, and a line per item buries the rest of
        /// the refresh under them. One line that grows is readable; a hundred lines that repeat are not.
        /// </summary>
        [Test]
        public async Task Several_items_nobody_could_place_share_one_warning()
        {
            TheParentOverrideNames(TheLinkTypeTheOverrideNames.Name);
            TheIssueHasLinksOfTheNamedTypeTo(TheAmbiguousItem, OneCandidate, TheOtherCandidate);
            TheIssueHasLinksOfTheNamedTypeTo(TheOtherAmbiguousItem, ACandidateOfTheOtherAmbiguousItem, TheOtherCandidateOfTheOtherAmbiguousItem);

            await TheTeamIsRefreshed();

            Assert.That(
                TheOneWarningTheRefreshWrote(),
                Does.Contain(TheAmbiguousItem)
                    .And.Contain(TheOtherAmbiguousItem)
                    .And.Contain(ACandidateOfTheOtherAmbiguousItem)
                    .And.Contain(TheOtherCandidateOfTheOtherAmbiguousItem),
                "One line for the whole refresh only helps if it is the whole refresh - a line that reports the first item and drops the rest is worse than several lines, because it looks complete.");
        }

        /// <summary>
        /// A Portfolio reads its own records and hangs them under their own parents, so the same links can
        /// leave it with the same unanswerable question. An administrator who runs Portfolios and not Teams
        /// would otherwise be shown nothing at all.
        /// </summary>
        [Test]
        public async Task A_portfolio_refresh_reports_it_the_same_way()
        {
            TheParentOverrideNames(TheLinkTypeTheOverrideNames.Name);
            TheIssueHasLinksOfTheNamedTypeTo(TheAmbiguousItem, OneCandidate, TheOtherCandidate);

            var refreshed = await ThePortfolioIsRefreshed();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheParentOf(refreshed, TheAmbiguousItem), Is.Empty,
                    "Whichever door the refresh came through, the links say two things and the item hangs under one, so there is still nothing to choose between them.");
                Assert.That(
                    TheOneWarningNaming(TheAmbiguousItem),
                    Does.Contain(OneCandidate).And.Contain(TheOtherCandidate),
                    "An administrator who reaches this through a Portfolio has the same links to go and fix, and needs to be told which they are in the same words.");
            }
        }

        /// <summary>
        /// A warning scrolls off. Refresh history does not, and it is where somebody goes to ask whether
        /// a tracker is in good order - so the number of records this refresh could not place has to be
        /// recorded on the row, not only said once into the log and lost.
        /// </summary>
        [Test]
        public async Task The_refresh_records_how_many_records_it_could_not_place()
        {
            TheParentOverrideNames(TheLinkTypeTheOverrideNames.Name);
            TheIssueHasLinksOfTheNamedTypeTo(TheAmbiguousItem, OneCandidate, TheOtherCandidate);
            TheIssueHasLinksOfTheNamedTypeTo(TheOtherAmbiguousItem, ACandidateOfTheOtherAmbiguousItem, TheOtherCandidateOfTheOtherAmbiguousItem);
            TheIssueHasLinksOfTheNamedTypeTo(AThirdAmbiguousItem, ACandidateOfTheThirdAmbiguousItem, TheOtherCandidateOfTheThirdAmbiguousItem);

            var teamId = await TheTeamIsRefreshedByTheRunningApplication();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheRowTheRefreshRecorded(RefreshType.Team, teamId).RecordsWhoseLinksNamedMoreThanOneParent, Is.EqualTo(3),
                    "Three records came back with no parent and no way to choose one, and a row that says none of them did tells somebody looking at refresh history that this tracker is in better order than it is.");
                Assert.That(TheOneWarningTheRefreshWrote(), Does.Contain(TheAmbiguousItem),
                    "Recording the number does not replace saying which records they were - a count nobody can act on sends the reader back to the log, and the log has to still be one line.");
            }
        }

        /// <summary>
        /// The number has to mean something when it is zero as well. A row that reports a count only when
        /// there is one to report leaves every quiet refresh looking the same as one nobody has read yet.
        /// </summary>
        [Test]
        public async Task A_refresh_with_nothing_it_could_not_place_records_none()
        {
            TheParentOverrideNames(TheLinkTypeTheOverrideNames.Name);
            TheIssueHasLinksOfTheNamedTypeTo(TheItemWithOneCandidate, TheParentTheItemWithOneCandidateTakes);

            var teamId = await TheTeamIsRefreshedByTheRunningApplication();

            var recorded = TheRowTheRefreshRecorded(RefreshType.Team, teamId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(recorded.RecordsWhoseLinksNamedMoreThanOneParent, Is.Zero,
                    "Every link on this refresh named exactly one issue, so a tracker in good order has to read as one.");
                Assert.That(recorded.Success, Is.True,
                    "positive control: a refresh that failed would record nothing it could not place either, and the zero above would mean nothing.");
                NothingWasWrittenToTheLogAsAWarning();
            }
        }

        /// <summary>
        /// Portfolio refresh history is a page of its own, read by people who never open a Team. The
        /// count reaching only one of the two rows is the same as it reaching neither, for them.
        /// </summary>
        [Test]
        public async Task A_portfolio_refresh_records_the_count_on_its_own_row()
        {
            TheParentOverrideNames(TheLinkTypeTheOverrideNames.Name);
            TheIssueHasLinksOfTheNamedTypeTo(TheAmbiguousItem, OneCandidate, TheOtherCandidate);
            TheIssueHasLinksOfTheNamedTypeTo(TheOtherAmbiguousItem, ACandidateOfTheOtherAmbiguousItem, TheOtherCandidateOfTheOtherAmbiguousItem);
            TheIssueHasLinksOfTheNamedTypeTo(AThirdAmbiguousItem, ACandidateOfTheThirdAmbiguousItem, TheOtherCandidateOfTheThirdAmbiguousItem);

            var portfolioId = await ThePortfolioIsRefreshedByTheRunningApplication();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheRowTheRefreshRecorded(RefreshType.Portfolio, portfolioId).RecordsWhoseLinksNamedMoreThanOneParent, Is.EqualTo(3),
                    "The same three records are unplaceable whichever door the refresh came through, and an administrator who only runs Portfolios reads only this row.");
                Assert.That(
                    TheOneWarningNaming(TheAmbiguousItem),
                    Does.Contain(TheOtherAmbiguousItem).And.Contain(AThirdAmbiguousItem),
                    "Three records nobody could place still cost one line, and that line still has to name all three or the count above sends somebody hunting for records it does not identify.");
            }
        }
    }
}
