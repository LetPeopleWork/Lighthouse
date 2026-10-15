using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.OnlyFeaturesAsParentCandidates
{
    /// <summary>
    /// Acceptance scenarios: a Team's Work Item linked to several Features counts toward the one
    /// whose link still standing was made first, on every refresh, however the tracker orders the links.
    /// Driving port: a Team refresh through the running application.
    ///
    /// The link dates come from the history Jira already sends with every issue, built in the shape Jira Cloud
    /// was seen to write when a link is added, removed and added again.
    ///
    /// When the history dates nothing, the older Feature wins, then the lower key. Each rule is named in
    /// the line, so the administrator can tell which one decided.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-6301-only-features-as-parent-candidates")]
    [Category("slice-02")]
    public class Slice02OldestLinkWinsTest : ParentPlacementAcceptanceTest
    {
        private const string TheFirstFeature = "PAY-88";

        private const string TheFeatureItWasSplitInto = "PAY-91";

        private const string TheItemLinkedToTwoFeatures = "PAY-420";

        private const string TheItemWhoseLinkWasAddedAgain = "PAY-425";

        private const string TheItemWhoseHistoryArrivesOutOfOrder = "PAY-426";

        private const string TheItemWithALongHistory = "PAY-427";

        private const string TheItemWithAnOlderIncidentLink = "PAY-431";

        private const string AnIncident = "INC-5531";

        private const string TheMigratedFeature = "OPS-12";

        private const string TheFeatureWithNoCreatedDate = "OPS-13";

        private const string TheNewerFeature = "OPS-14";

        private const string TheDatedFeature = "OPS-19";

        private const string TheFeatureWithTheHigherNumber = "OPS-9";

        private const string TheItemWithOneDatedLink = "OPS-77";

        private const string TheItemWithNoLinkDates = "OPS-80";

        private const string TheItemBetweenFeaturesOfTheSameAge = "OPS-81";

        private const string TheItemBesideAFeatureWithNoCreatedDate = "OPS-82";

        private static readonly DateTime March4 = On(2026, 3, 4);

        private static readonly DateTime June17 = On(2026, 6, 17);

        private static readonly DateTime TheSameDay = On(2025, 11, 2);

        [Test]
        [Category("kpi")]
        public async Task A_Work_Item_linked_to_two_Features_counts_toward_the_one_it_was_linked_to_first()
        {
            await TheItemWasLinkedToTwoFeaturesThreeMonthsApart();

            await TheTeamRefreshes();

            var figures = await WhatRefreshHistoryShowsForTheTeam();
            var line = TheOneLineThatPlacedFor(TheTeam);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheStoredParentOfTheWorkItem(TheItemLinkedToTwoFeatures), Is.EqualTo(TheFirstFeature),
                    "The item was split out of its first Feature and nobody removed the link. The Feature it was first linked to is the one the customer calls its parent.");
                Assert.That(figures.Placed, Is.EqualTo(1),
                    "A record placed by the oldest link is placed, and counted as such.");
                Assert.That(figures.NotPlaced, Is.Zero,
                    "Two Features are no longer a reason to refuse: the rule decides.");
                Assert.That(line, Is.EqualTo(
                    $"{TheTeam} placed 1 record whose links name more than one issue: "
                    + $"{TheItemLinkedToTwoFeatures} under {TheFirstFeature} (oldest link, {Day(March4)}; set aside {TheFeatureItWasSplitInto}, {Day(June17)})"),
                    "The administrator decides from this line whether the oldest link is the right Feature, so it names the rule, the date and what lost.");
            }
        }

        /// <summary>
        /// Jira does not promise an order for an issue's links or its history. The same links and the same
        /// Features give the same parent on every refresh, so a refresh that only changed the order must not
        /// move the item.
        /// </summary>
        [Test]
        [Category("edge")]
        [Category("kpi")]
        public async Task The_same_Work_Item_stays_under_the_same_Feature_however_the_tracker_orders_its_links()
        {
            await TheItemWasLinkedToTwoFeaturesThreeMonthsApart();
            await TheTeamIsInSteadyUse();
            var beforeTheReorder = TheStoredParentOfTheWorkItem(TheItemLinkedToTwoFeatures);

            TheTrackerListsTheLinksInReverse(TheItemLinkedToTwoFeatures);
            TheIssueMoves(TheItemLinkedToTwoFeatures);
            await TheTeamRefreshes();

            var figures = await WhatRefreshHistoryShowsForTheTeam();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(beforeTheReorder, Is.EqualTo(TheFirstFeature),
                    "positive control: the item has to be placed before the reorder, or holding still afterwards proves nothing.");
                Assert.That(TheStoredParentOfTheWorkItem(TheItemLinkedToTwoFeatures), Is.EqualTo(TheFirstFeature),
                    "A Feature that changes with the order Jira happens to list links in is a guess, and the item would hop between two Features from one refresh to the next.");
                Assert.That(figures.Mode, Is.EqualTo("Delta"),
                    "positive control: the refresh after the reorder has to be the cheap one, which places by the same rule.");
            }
        }

        [Test]
        [Category("edge")]
        public async Task A_dated_link_wins_over_a_link_with_no_recorded_date()
        {
            TheTeamReadsItsParentsFromTheCausedByLink();
            ThePortfolioHoldsTheFeature(TheMigratedFeature, On(2025, 11, 2));
            ThePortfolioHoldsTheFeature(TheDatedFeature, On(2026, 7, 1));
            TheTeamHoldsTheWorkItem(TheItemWithOneDatedLink);
            TheIssueIsCausedBy(TheItemWithOneDatedLink, TheMigratedFeature);
            TheIssueHasTheLinkHistory(TheItemWithOneDatedLink, LinkEvent.Linked(TheDatedFeature, On(2026, 8, 1)));
            await ThePortfolioRefreshes();

            await TheTeamRefreshes();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheStoredParentOfTheWorkItem(TheItemWithOneDatedLink), Is.EqualTo(TheDatedFeature),
                    "A link with no recorded date was made by an import or a migration at some unknown time. Only the dated one can be said to be the older link.");
                Assert.That(TheOneLineThatPlacedFor(TheTeam), Does.Contain(
                    $"{TheItemWithOneDatedLink} under {TheDatedFeature} (oldest link, 2026-08-01; set aside {TheMigratedFeature})"),
                    "The set-aside link carries no date because none was recorded, and the line must not invent one.");
            }
        }

        [Test]
        [Category("edge")]
        public async Task With_no_link_dates_the_older_Feature_wins()
        {
            TheTeamReadsItsParentsFromTheCausedByLink();
            ThePortfolioHoldsTheFeature(TheMigratedFeature, On(2025, 11, 2));
            ThePortfolioHoldsTheFeature(TheNewerFeature, On(2026, 1, 15));
            TheTeamHoldsTheWorkItem(TheItemWithNoLinkDates);
            TheIssueIsCausedBy(TheItemWithNoLinkDates, TheNewerFeature, TheMigratedFeature);
            await ThePortfolioRefreshes();

            await TheTeamRefreshes();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheStoredParentOfTheWorkItem(TheItemWithNoLinkDates), Is.EqualTo(TheMigratedFeature),
                    "With no link dated, the Feature that existed first is the closest stand-in for the first link that was made.");
                Assert.That(TheOneLineThatPlacedFor(TheTeam), Does.Contain(
                    $"{TheItemWithNoLinkDates} under {TheMigratedFeature} (earliest created, 2025-11-02; set aside {TheNewerFeature})"),
                    "The line has to say a created date decided, or the administrator goes looking for a link date that does not exist.");
            }
        }

        /// <summary>
        /// The last resort is the lower key, compared character by character, which is what makes the answer
        /// the same everywhere. OPS-12 sorts before OPS-9 that way, whatever the numbers say.
        /// </summary>
        [Test]
        [Category("edge")]
        public async Task With_no_link_dates_and_Features_of_the_same_age_the_lower_key_wins()
        {
            TheTeamReadsItsParentsFromTheCausedByLink();
            ThePortfolioHoldsTheFeature(TheMigratedFeature, TheSameDay);
            ThePortfolioHoldsTheFeature(TheFeatureWithTheHigherNumber, TheSameDay);
            TheTeamHoldsTheWorkItem(TheItemBetweenFeaturesOfTheSameAge);
            TheIssueIsCausedBy(TheItemBetweenFeaturesOfTheSameAge, TheFeatureWithTheHigherNumber, TheMigratedFeature);
            await ThePortfolioRefreshes();

            await TheTeamRefreshes();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheStoredParentOfTheWorkItem(TheItemBetweenFeaturesOfTheSameAge), Is.EqualTo(TheMigratedFeature),
                    "Every earlier rule tied, so the key decides, and it has to decide the same way on every instance and every refresh.");
                Assert.That(TheOneLineThatPlacedFor(TheTeam), Does.Contain(
                    $"{TheItemBetweenFeaturesOfTheSameAge} under {TheMigratedFeature} (lowest key; set aside {TheFeatureWithTheHigherNumber})"),
                    "The line says the key decided, so nobody reads meaning into a choice that had none.");
            }
        }

        /// <summary>
        /// Jira sends an empty created stamp for some migrated issues, which Lighthouse stores as the
        /// earliest date there is. Taken at face value that would always win, so it counts as unknown, and a
        /// Feature with a known date comes first.
        /// </summary>
        [Test]
        [Category("edge")]
        public async Task A_Feature_with_no_recorded_created_date_counts_as_unknown_not_as_the_oldest()
        {
            TheTeamReadsItsParentsFromTheCausedByLink();
            ThePortfolioHoldsTheFeatureWithNoCreatedDate(TheFeatureWithNoCreatedDate);
            ThePortfolioHoldsTheFeature(TheNewerFeature, On(2026, 1, 15));
            TheTeamHoldsTheWorkItem(TheItemBesideAFeatureWithNoCreatedDate);
            TheIssueIsCausedBy(TheItemBesideAFeatureWithNoCreatedDate, TheFeatureWithNoCreatedDate, TheNewerFeature);
            await ThePortfolioRefreshes();

            await TheTeamRefreshes();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheStoredParentOfTheWorkItem(TheItemBesideAFeatureWithNoCreatedDate), Is.EqualTo(TheNewerFeature),
                    "A missing date is not the oldest date. Reading it as one would hand every item to whichever Feature lost its date in a migration.");
                Assert.That(TheOneLineThatPlacedFor(TheTeam), Does.Contain(
                    $"{TheItemBesideAFeatureWithNoCreatedDate} under {TheNewerFeature} (earliest created, 2026-01-15; set aside {TheFeatureWithNoCreatedDate})"),
                    "The line names the known date that decided.");
            }
        }

        /// <summary>
        /// A link removed and added again counts from when it was added again: the link that stands is the
        /// new one, and the older one stopped existing when it was removed.
        /// </summary>
        [Test]
        [Category("edge")]
        public async Task A_link_removed_and_added_again_counts_from_when_it_was_added_again()
        {
            TheTeamReadsItsParentsFromTheCausedByLink();
            ThePortfolioHoldsTheFeature(TheFirstFeature);
            ThePortfolioHoldsTheFeature(TheFeatureItWasSplitInto);
            TheTeamHoldsTheWorkItem(TheItemWhoseLinkWasAddedAgain);
            TheIssueHasTheLinkHistory(
                TheItemWhoseLinkWasAddedAgain,
                LinkEvent.Linked(TheFirstFeature, On(2026, 2, 1)),
                LinkEvent.Linked(TheFeatureItWasSplitInto, On(2026, 4, 10)),
                LinkEvent.Unlinked(TheFirstFeature, On(2026, 5, 1)),
                LinkEvent.Linked(TheFirstFeature, On(2026, 7, 1)));
            await ThePortfolioRefreshes();

            await TheTeamRefreshes();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheStoredParentOfTheWorkItem(TheItemWhoseLinkWasAddedAgain), Is.EqualTo(TheFeatureItWasSplitInto),
                    "A re-added link counts from when it was re-added, because the earlier date belongs to a link that no longer exists.");
                Assert.That(TheOneLineThatPlacedFor(TheTeam), Does.Contain(
                    $"{TheItemWhoseLinkWasAddedAgain} under {TheFeatureItWasSplitInto} (oldest link, 2026-04-10; set aside {TheFirstFeature}, 2026-07-01)"),
                    "The date shown for the set-aside link is the one that stands, so the administrator finds it where it is in Jira's history.");
            }
        }

        /// <summary>
        /// The history is read by when each entry happened, not by where it sits in the answer. Read in the
        /// order given here, the re-added link would seem to date from its first adding and win.
        /// </summary>
        [Test]
        [Category("edge")]
        public async Task A_history_that_arrives_out_of_order_is_read_by_when_each_entry_happened()
        {
            TheTeamReadsItsParentsFromTheCausedByLink();
            ThePortfolioHoldsTheFeature(TheFirstFeature);
            ThePortfolioHoldsTheFeature(TheFeatureItWasSplitInto);
            TheTeamHoldsTheWorkItem(TheItemWhoseHistoryArrivesOutOfOrder);
            TheIssueHasTheLinkHistory(
                TheItemWhoseHistoryArrivesOutOfOrder,
                LinkEvent.Linked(TheFeatureItWasSplitInto, On(2026, 4, 10)),
                LinkEvent.Linked(TheFirstFeature, On(2026, 7, 1)),
                LinkEvent.Unlinked(TheFirstFeature, On(2026, 5, 1)),
                LinkEvent.Linked(TheFirstFeature, On(2026, 2, 1)));
            await ThePortfolioRefreshes();

            await TheTeamRefreshes();

            Assert.That(TheStoredParentOfTheWorkItem(TheItemWhoseHistoryArrivesOutOfOrder), Is.EqualTo(TheFeatureItWasSplitInto),
                "Cloud and Data Center do not promise the same order for a history, and an answer that depends on it differs between the two.");
        }

        /// <summary>
        /// A history too long for the search is re-read on the issue's own endpoint, as it already is for
        /// status changes, and the link dates come from the full copy.
        /// </summary>
        [Test]
        [Category("edge")]
        public async Task A_long_history_is_read_in_full_before_a_link_is_dated()
        {
            TheTeamReadsItsParentsFromTheCausedByLink();
            ThePortfolioHoldsTheFeature(TheFirstFeature);
            ThePortfolioHoldsTheFeature(TheFeatureItWasSplitInto);
            TheTeamHoldsTheWorkItem(TheItemWithALongHistory);
            TheIssueHasTheLinkHistory(TheItemWithALongHistory, LinkEvent.Linked(TheFeatureItWasSplitInto, June17), LinkEvent.Linked(TheFirstFeature, March4));
            TheIssuesHistoryIsTooLongForTheSearch(TheItemWithALongHistory);
            await ThePortfolioRefreshes();

            await TheTeamRefreshes();

            Assert.That(TheOneLineThatPlacedFor(TheTeam), Does.Contain(
                $"{TheItemWithALongHistory} under {TheFirstFeature} (oldest link, {Day(March4)}; set aside {TheFeatureItWasSplitInto}, {Day(June17)})"),
                "The search's copy of a long history holds nothing, so dates read from it would leave every link undated and hand the choice to the fallbacks.");
        }

        /// <summary>
        /// The incidents go first, whatever their dates. An incident linked before either Feature is still
        /// not somewhere a Work Item can be hung.
        /// </summary>
        [Test]
        [Category("edge")]
        public async Task An_older_link_to_an_incident_never_wins()
        {
            TheTeamReadsItsParentsFromTheCausedByLink();
            ThePortfolioHoldsTheFeature(TheFirstFeature);
            ThePortfolioHoldsTheFeature(TheFeatureItWasSplitInto);
            TheTeamHoldsTheWorkItem(TheItemWithAnOlderIncidentLink);
            TheIssueHasTheLinkHistory(
                TheItemWithAnOlderIncidentLink,
                LinkEvent.Linked(AnIncident, On(2026, 1, 1)),
                LinkEvent.Linked(TheFirstFeature, March4),
                LinkEvent.Linked(TheFeatureItWasSplitInto, June17));
            await ThePortfolioRefreshes();

            await TheTeamRefreshes();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheStoredParentOfTheWorkItem(TheItemWithAnOlderIncidentLink), Is.EqualTo(TheFirstFeature),
                    "Narrowing to Features comes before the dates, or the oldest cause would win and the item would be hung under an incident.");
                Assert.That(TheOneLineThatPlacedFor(TheTeam), Does.Contain($"{TheItemWithAnOlderIncidentLink} under {TheFirstFeature} (oldest link, {Day(March4)}; set aside ")
                    .And.Contain($"{AnIncident}, 2026-01-01")
                    .And.Contain($"{TheFeatureItWasSplitInto}, {Day(June17)}"),
                    "Everything set aside is named, the incident included, so the administrator sees the whole of what the links said.");
            }
        }

        private async Task TheItemWasLinkedToTwoFeaturesThreeMonthsApart()
        {
            TheTeamReadsItsParentsFromTheCausedByLink();
            ThePortfolioHoldsTheFeature(TheFirstFeature);
            ThePortfolioHoldsTheFeature(TheFeatureItWasSplitInto);
            TheTeamHoldsTheWorkItem(TheItemLinkedToTwoFeatures);
            TheIssueHasTheLinkHistory(TheItemLinkedToTwoFeatures, LinkEvent.Linked(TheFirstFeature, March4), LinkEvent.Linked(TheFeatureItWasSplitInto, June17));
            await ThePortfolioRefreshes();
        }
    }
}
