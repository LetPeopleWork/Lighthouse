using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.OnlyFeaturesAsParentCandidates
{
    /// <summary>
    /// Acceptance scenarios: a Team's Work Item whose links name a Feature and some issues that
    /// are not Features counts toward that Feature. Driving port: a Team refresh through the running
    /// application. What it recorded is read back through Refresh History.
    ///
    /// The customer's hierarchy and their incidents share one link type, so an item caused by its Feature
    /// and by an incident names two issues. Only one of them is something Lighthouse can hang it under: the
    /// one it holds as a Feature. Everything else is set aside, said so in one line, and counted.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-6301-only-features-as-parent-candidates")]
    [Category("slice-01")]
    public class Slice01NarrowToFeaturesTest : ParentPlacementAcceptanceTest
    {
        private const string TheFeature = "PAY-88";

        private const string TheOtherFeature = "PAY-91";

        private const string AFeatureStoredLater = "PAY-95";

        private const string TheInitiativeAbove = "PAY-7";

        private const string TheItemCausedByItsFeatureAndAnIncident = "PAY-311";

        private const string TheItemCausedOnlyByIncidents = "PAY-402";

        private const string TheItemWithOneLink = "PAY-415";

        private const string TheItemThatGainsASecondLink = "PAY-416";

        private const string TheItemLinkedToTwoFeatures = "PAY-420";

        private const string TheItemWhoseFeatureIsStoredLater = "PAY-430";

        private const string TheItemCausedByTheLevelAbove = "PAY-440";

        private const string AnotherItemCausedByItsFeatureAndAnIncident = "PAY-450";

        private const string AnotherItemCausedOnlyByIncidents = "PAY-451";

        private const string AnIncident = "INC-5531";

        private const string AnotherIncident = "INC-5602";

        private const string AThirdIncident = "INC-5610";

        private const string TheLoneIncident = "INC-5620";

        private const string TheTrackersOwnParent = "PAY-1";

        private const string TheOnlyFeatureAmongItsLinks = "only Feature";

        /// <summary>
        /// The thinnest slice through every step an administrator cares about: the links are read, the
        /// incident is set aside, the Work Item is stored under its Feature, the Additional Field shows the
        /// Feature it found, and Refresh History counts the placement apart from the refusals.
        /// </summary>
        [Test]
        [Category("walking-skeleton")]
        [Category("kpi")]
        public async Task A_Work_Item_linked_to_one_Feature_and_an_incident_counts_toward_the_Feature()
        {
            TheTeamReadsItsParentsFromTheCausedByLink();
            ThePortfolioHoldsTheFeature(TheFeature);
            TheTeamHoldsTheWorkItem(TheItemCausedByItsFeatureAndAnIncident);
            TheIssueIsCausedBy(TheItemCausedByItsFeatureAndAnIncident, TheFeature, AnIncident);
            await ThePortfolioRefreshes();

            await TheTeamRefreshes();

            var figures = await WhatRefreshHistoryShowsForTheTeam();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheStoredParentOfTheWorkItem(TheItemCausedByItsFeatureAndAnIncident), Is.EqualTo(TheFeature),
                    "The incident is a cause, not a place to hang work. Leaving the item with no parent sizes the Feature by the Portfolio default, which is the customer's complaint.");
                Assert.That(WhatTheParentOverrideFieldShowsOnTheWorkItem(TheItemCausedByItsFeatureAndAnIncident), Is.EqualTo(TheFeature),
                    "The administrator checks what the links resolved to on the record itself. Showing nothing there while the item is stored under a Feature is two answers to one question.");
                Assert.That(figures.Placed, Is.EqualTo(1),
                    "Refresh History has to say how many records a refresh placed from several candidates, or the administrator cannot tell that anything was decided on their behalf.");
                Assert.That(figures.NotPlaced, Is.Zero,
                    "Nothing was left without a parent, so a not-placed figure above zero sends the administrator to fix links that are fine.");
            }
        }

        /// <summary>
        /// The administrator checks a placement from one line, without opening Jira: which Feature it went
        /// under, why that one, and what was set aside on the way.
        /// </summary>
        [Test]
        [Ignore(PendingStory6301.Slice01)]
        public async Task The_refresh_names_the_Feature_it_chose_why_and_what_it_set_aside()
        {
            TheTeamReadsItsParentsFromTheCausedByLink();
            ThePortfolioHoldsTheFeature(TheFeature);
            TheTeamHoldsTheWorkItem(TheItemCausedByItsFeatureAndAnIncident);
            TheIssueIsCausedBy(TheItemCausedByItsFeatureAndAnIncident, TheFeature, AnIncident);
            await ThePortfolioRefreshes();

            await TheTeamRefreshes();

            Assert.That(TheOneLineThatPlacedFor(TheTeam), Is.EqualTo(
                $"{TheTeam} placed 1 record whose links name more than one issue: "
                + $"{TheItemCausedByItsFeatureAndAnIncident} under {TheFeature} ({TheOnlyFeatureAmongItsLinks}; set aside {AnIncident})"),
                "A placement nobody can see is a guess that looks like configuration. The line is where the administrator checks it.");
        }

        /// <summary>
        /// An item whose links name only incidents cannot be hung under any of them, and it must not look
        /// like an item nobody ever linked: it is counted, named with every candidate, and given the reason.
        /// </summary>
        [Test]
        [Ignore(PendingStory6301.Slice01)]
        [Category("error")]
        public async Task A_Work_Item_linked_only_to_non_Features_is_reported_not_silently_dropped()
        {
            TheTeamReadsItsParentsFromTheCausedByLink();
            ThePortfolioHoldsTheFeature(TheFeature);
            TheTeamHoldsTheWorkItem(TheItemCausedOnlyByIncidents);
            TheIssueIsCausedBy(TheItemCausedOnlyByIncidents, AnotherIncident, AThirdIncident);
            await ThePortfolioRefreshes();

            await TheTeamRefreshes();

            var figures = await WhatRefreshHistoryShowsForTheTeam();
            var line = TheOneLineThatCouldNotPlaceFor(TheTeam);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheStoredParentOfTheWorkItem(TheItemCausedOnlyByIncidents), Is.Empty,
                    "Neither incident is a Feature, and Jira's own parent field is empty here, so the item has no parent to take.");
                Assert.That(figures.NotPlaced, Is.EqualTo(1),
                    "An item that could not be placed has to be counted, or Refresh History reports a tracker in better order than it is.");
                Assert.That(figures.Placed, Is.Zero,
                    "Nothing was placed, so a placed figure would claim a decision that was never made.");
                Assert.That(line, Is.EqualTo(
                    $"{TheTeam} could not place 1 record whose links name more than one issue: "
                    + $"{TheItemCausedOnlyByIncidents} (linked to {AnotherIncident}, {AThirdIncident}; none is a Feature Lighthouse holds). "
                    + "It takes no parent from its links."),
                    "The administrator has to find the item and its links from the line alone, and know why none of them was used.");
            }
        }

        /// <summary>
        /// Placed yesterday, and today its Feature link is gone and another incident was added. The item gets
        /// what this reading gives it, Jira's own parent field, never yesterday's Feature: keeping a stale
        /// placement would show an old answer as if it were current.
        /// </summary>
        [Test]
        [Ignore(PendingStory6301.Slice01)]
        [Category("error")]
        public async Task A_Work_Item_that_can_no_longer_be_placed_takes_the_trackers_own_parent_not_the_Feature_it_had()
        {
            TheTeamReadsItsParentsFromTheCausedByLink();
            ThePortfolioHoldsTheFeature(TheFeature);
            TheTeamHoldsTheWorkItem(TheItemCausedOnlyByIncidents);
            TheTrackersOwnParentOf(TheItemCausedOnlyByIncidents, TheTrackersOwnParent);
            TheIssueIsCausedBy(TheItemCausedOnlyByIncidents, TheFeature, AnotherIncident);
            await ThePortfolioRefreshes();
            await TheTeamIsInSteadyUse();
            var whereTheFirstReadingPutIt = TheStoredParentOfTheWorkItem(TheItemCausedOnlyByIncidents);

            TheIssueLosesItsLinkTo(TheItemCausedOnlyByIncidents, TheFeature);
            TheIssueIsCausedBy(TheItemCausedOnlyByIncidents, AThirdIncident);
            TheIssueMoves(TheItemCausedOnlyByIncidents);
            await TheTeamRefreshes();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(whereTheFirstReadingPutIt, Is.EqualTo(TheFeature),
                    "positive control: the first reading has to have placed the item under its Feature, or there is no earlier placement for this scenario to drop.");
                Assert.That(TheStoredParentOfTheWorkItem(TheItemCausedOnlyByIncidents), Is.EqualTo(TheTrackersOwnParent),
                    "The links no longer name a Feature, so the item is where this reading puts it. Keeping the Feature from the day before would show a stale answer as current.");
                Assert.That(WhatTheParentOverrideFieldShowsOnTheWorkItem(TheItemCausedOnlyByIncidents), Is.EqualTo(NoValueShown),
                    "The links resolved to nothing, so the Additional Field shows nothing. A key there would send the administrator looking for a parent that was not used.");
            }
        }

        /// <summary>
        /// One link is not a question, Feature or not. It is stored exactly as before, so a rule matching
        /// "Parent Reference ID equals INC-5620" keeps matching, and it counts in neither figure.
        /// </summary>
        [Test]
        [Ignore(PendingStory6301.Slice01)]
        [Category("edge")]
        [Category("kpi")]
        public async Task A_Work_Item_with_a_single_link_keeps_it_Feature_or_not()
        {
            TheTeamReadsItsParentsFromTheCausedByLink();
            ThePortfolioHoldsTheFeature(TheFeature);
            TheTeamHoldsTheWorkItem(TheItemWithOneLink);
            TheIssueIsCausedBy(TheItemWithOneLink, TheLoneIncident);
            await ThePortfolioRefreshes();

            await TheTeamRefreshes();

            var figures = await WhatRefreshHistoryShowsForTheTeam();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheStoredParentOfTheWorkItem(TheItemWithOneLink), Is.EqualTo(TheLoneIncident),
                    "Rules the administrator already wrote against this value would silently stop matching if a lone link were narrowed too.");
                Assert.That(figures.Placed, Is.Zero,
                    "Nothing had more than one candidate, so nothing was placed among several.");
                Assert.That(figures.NotPlaced, Is.Zero,
                    "Nothing had more than one candidate, so nothing was refused either.");
                Assert.That(WhatTheLastRefreshWarned(), Is.Empty,
                    "A refresh where every link named one issue has nothing to warn about.");
            }
        }

        /// <summary>
        /// Stored under its one incident yesterday, and today a second incident is linked. Now there is a
        /// question, and no Feature answers it, so the item is not placed and takes what the tracker reports
        /// for it now, which is nothing here. Keeping yesterday's incident would show an old answer as current.
        /// </summary>
        [Test]
        [Ignore(PendingStory6301.Slice01)]
        [Category("error")]
        public async Task A_Work_Item_kept_under_its_lone_link_is_not_placed_once_a_second_non_Feature_is_linked()
        {
            TheTeamReadsItsParentsFromTheCausedByLink();
            ThePortfolioHoldsTheFeature(TheFeature);
            TheTeamHoldsTheWorkItem(TheItemThatGainsASecondLink);
            TheIssueIsCausedBy(TheItemThatGainsASecondLink, TheLoneIncident);
            await ThePortfolioRefreshes();
            await TheTeamIsInSteadyUse();
            var whereTheLoneLinkPutIt = TheStoredParentOfTheWorkItem(TheItemThatGainsASecondLink);

            TheIssueIsCausedBy(TheItemThatGainsASecondLink, AnotherIncident);
            TheIssueMoves(TheItemThatGainsASecondLink);
            await TheTeamRefreshes();

            var figures = await WhatRefreshHistoryShowsForTheTeam();
            var line = TheOneLineThatCouldNotPlaceFor(TheTeam);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(whereTheLoneLinkPutIt, Is.EqualTo(TheLoneIncident),
                    "positive control: a lone link is stored as it is, so the item has to start under its incident for this scenario to replace anything.");
                Assert.That(TheStoredParentOfTheWorkItem(TheItemThatGainsASecondLink), Is.Empty,
                    "Neither incident is a Feature and the tracker reports no parent of its own, so the item has none. Keeping the lone incident would show yesterday's reading as today's.");
                Assert.That(figures.NotPlaced, Is.EqualTo(1),
                    "The refused item has to be counted, or the change in its links goes unnoticed.");
                Assert.That(line, Does.Contain($"{TheItemThatGainsASecondLink} (linked to "),
                    "The administrator finds the item that lost its parent from the line alone.");
            }
        }

        /// <summary>
        /// Two Features are left after the incidents go, and until the oldest-link rule exists there is no
        /// stated way to choose. The item is refused and named, never guessed. The oldest-link rule replaces
        /// this scenario when it ships.
        /// </summary>
        [Test]
        [Ignore(PendingStory6301.Slice01)]
        [Category("error")]
        public async Task Two_Features_left_after_narrowing_are_not_guessed_before_the_oldest_link_rule_exists()
        {
            TheTeamReadsItsParentsFromTheCausedByLink();
            ThePortfolioHoldsTheFeature(TheFeature);
            ThePortfolioHoldsTheFeature(TheOtherFeature);
            TheTeamHoldsTheWorkItem(TheItemLinkedToTwoFeatures);
            TheIssueIsCausedBy(TheItemLinkedToTwoFeatures, TheFeature, TheOtherFeature);
            await ThePortfolioRefreshes();

            await TheTeamRefreshes();

            var figures = await WhatRefreshHistoryShowsForTheTeam();
            var line = TheOneLineThatCouldNotPlaceFor(TheTeam);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheStoredParentOfTheWorkItem(TheItemLinkedToTwoFeatures), Is.Empty,
                    "Whichever of two Features a refresh reached for, without a stated rule it is a guess that reads as configuration.");
                Assert.That(figures.NotPlaced, Is.EqualTo(1),
                    "The refusal has to be counted, or the administrator cannot see that items are waiting for a choice.");
                Assert.That(line, Does.Contain(
                    $"{TheItemLinkedToTwoFeatures} (linked to {TheFeature}, {TheOtherFeature}; more than one is a Feature Lighthouse holds)"),
                    "The line has to say that both are Features, so the administrator knows this is not an untidy link but a real choice.");
            }
        }

        /// <summary>
        /// A Portfolio stores the level above its Features as well as the Features. Those count as Features
        /// Lighthouse holds, so an item caused by one of them and by an incident is placed under it.
        /// </summary>
        [Test]
        [Ignore(PendingStory6301.Slice01)]
        [Category("edge")]
        public async Task A_Feature_stored_only_as_the_level_above_a_Portfolio_counts_as_a_Feature_Lighthouse_holds()
        {
            TheTeamReadsItsParentsFromTheCausedByLink();
            ThePortfolioHoldsTheFeature(TheFeature);
            TheTrackersOwnParentOf(TheFeature, TheInitiativeAbove);
            TheTrackerHoldsTheIssueAbove(TheInitiativeAbove, SomeLongAgoDay);
            TheTeamHoldsTheWorkItem(TheItemCausedByTheLevelAbove);
            TheIssueIsCausedBy(TheItemCausedByTheLevelAbove, TheInitiativeAbove, AnIncident);
            await ThePortfolioRefreshes();

            await TheTeamRefreshes();

            Assert.That(TheStoredParentOfTheWorkItem(TheItemCausedByTheLevelAbove), Is.EqualTo(TheInitiativeAbove),
                "Any stored Feature counts, the level above included, because refusing it would treat a record Lighthouse holds as one it does not.");
        }

        /// <summary>
        /// The Feature arrives after the item was read. A cheap refresh does not read the item again unless
        /// it moved, so it keeps the reading it had, and the refresh counts nothing it did not download.
        /// </summary>
        [Test]
        [Ignore(PendingStory6301.Slice01)]
        [Category("edge")]
        public async Task A_Work_Item_that_did_not_move_keeps_its_reading_until_it_is_next_read()
        {
            TheTeamReadsItsParentsFromTheCausedByLink();
            TheTeamHoldsTheWorkItem(TheItemWhoseFeatureIsStoredLater);
            TheIssueIsCausedBy(TheItemWhoseFeatureIsStoredLater, AFeatureStoredLater, AnIncident);
            await TheTeamIsInSteadyUse();
            ThePortfolioHoldsTheFeature(AFeatureStoredLater);
            await ThePortfolioRefreshes();

            await TheTeamRefreshes();

            var figures = await WhatRefreshHistoryShowsForTheTeam();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheStoredParentOfTheWorkItem(TheItemWhoseFeatureIsStoredLater), Is.Empty,
                    "Nothing goes back over items after a Portfolio refresh. The item corrects itself the next time it is read, and that is the documented behaviour.");
                Assert.That(figures.Mode, Is.EqualTo("Delta"),
                    $"positive control: the second refresh has to be the cheap one, or this scenario says nothing about what a cheap refresh does. It warned: {string.Join(" | ", WhatTheLastRefreshWarned())}");
                Assert.That(figures.Placed, Is.Zero,
                    "A cheap refresh counts only what it downloaded, and it downloaded nothing.");
                Assert.That(figures.NotPlaced, Is.Zero,
                    "A cheap refresh counts only what it downloaded, and it downloaded nothing.");
            }
        }

        /// <summary>The same item, once it moves: it is read again, and the Feature stored since is found.</summary>
        [Test]
        [Ignore(PendingStory6301.Slice01)]
        [Category("edge")]
        public async Task A_Feature_stored_after_the_Work_Item_was_read_is_picked_up_when_the_Work_Item_is_next_read()
        {
            TheTeamReadsItsParentsFromTheCausedByLink();
            TheTeamHoldsTheWorkItem(TheItemWhoseFeatureIsStoredLater);
            TheIssueIsCausedBy(TheItemWhoseFeatureIsStoredLater, AFeatureStoredLater, AnIncident);
            await TheTeamIsInSteadyUse();
            ThePortfolioHoldsTheFeature(AFeatureStoredLater);
            await ThePortfolioRefreshes();
            TheIssueMoves(TheItemWhoseFeatureIsStoredLater);

            await TheTeamRefreshes();

            var figures = await WhatRefreshHistoryShowsForTheTeam();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheStoredParentOfTheWorkItem(TheItemWhoseFeatureIsStoredLater), Is.EqualTo(AFeatureStoredLater),
                    "The item was read again, and the Feature is stored by now, so it is the only Feature among the links.");
                Assert.That(figures.Placed, Is.EqualTo(1),
                    "A cheap refresh that re-read a record and placed it has to count it, the same as a full one.");
            }
        }

        /// <summary>
        /// A bulk edit that went wrong produces these by the hundred. One placed line and one not-placed line
        /// per refresh stay readable, and the old sentence claiming the items were left where they were is
        /// gone, because it was never true.
        /// </summary>
        [Test]
        [Ignore(PendingStory6301.Slice01)]
        [Category("edge")]
        public async Task Several_records_share_one_placed_line_and_one_not_placed_line()
        {
            TheTeamReadsItsParentsFromTheCausedByLink();
            ThePortfolioHoldsTheFeature(TheFeature);
            TheTeamHoldsTheWorkItem(TheItemCausedByItsFeatureAndAnIncident);
            TheIssueIsCausedBy(TheItemCausedByItsFeatureAndAnIncident, TheFeature, AnIncident);
            TheTeamHoldsTheWorkItem(AnotherItemCausedByItsFeatureAndAnIncident);
            TheIssueIsCausedBy(AnotherItemCausedByItsFeatureAndAnIncident, TheFeature, AnotherIncident);
            TheTeamHoldsTheWorkItem(TheItemCausedOnlyByIncidents);
            TheIssueIsCausedBy(TheItemCausedOnlyByIncidents, AnotherIncident, AThirdIncident);
            TheTeamHoldsTheWorkItem(AnotherItemCausedOnlyByIncidents);
            TheIssueIsCausedBy(AnotherItemCausedOnlyByIncidents, AnIncident, TheLoneIncident);
            await ThePortfolioRefreshes();

            await TheTeamRefreshes();

            var placed = TheOneLineThatPlacedFor(TheTeam);
            var notPlaced = TheOneLineThatCouldNotPlaceFor(TheTeam);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(placed, Does.StartWith($"{TheTeam} placed 2 records whose links name more than one issue: ")
                    .And.Contain($"{TheItemCausedByItsFeatureAndAnIncident} under {TheFeature}")
                    .And.Contain($"{AnotherItemCausedByItsFeatureAndAnIncident} under {TheFeature}"),
                    "One line for the whole refresh only helps if it names every record it counts.");
                Assert.That(notPlaced, Does.StartWith($"{TheTeam} could not place 2 records whose links name more than one issue: ")
                    .And.Contain(TheItemCausedOnlyByIncidents)
                    .And.Contain(AnotherItemCausedOnlyByIncidents),
                    "The same holds for the records nobody could place: a line that drops some of them looks complete and is not.");
                Assert.That(WhatTheLastRefreshWarned(), Has.None.Contains("where it was"),
                    "No record is left where it was: each gets what this reading gives it. A line saying otherwise sends the administrator looking for a parent that was replaced.");
            }
        }

        /// <summary>
        /// The count is a statement about every record with two or more candidates: each is placed or not
        /// placed, never both and never neither, whatever the mix. Records with one link or none are left
        /// exactly as they were read.
        /// </summary>
        [TestCaseSource(nameof(MixesOfRecords))]
        [Ignore(PendingStory6301.Slice01)]
        [Category("property")]
        [Category("kpi")]
        public async Task Placed_and_not_placed_add_up_to_every_Work_Item_whose_links_name_two_or_more_issues(
            int withOneFeatureAndAnIncident, int withOnlyIncidents, int withOneLink, int withNoLink)
        {
            TheTeamReadsItsParentsFromTheCausedByLink();
            ThePortfolioHoldsTheFeature(TheFeature);
            var lone = TheTeamHoldsAMixOf(withOneFeatureAndAnIncident, withOnlyIncidents, withOneLink, withNoLink);
            await ThePortfolioRefreshes();

            await TheTeamRefreshes();

            var figures = await WhatRefreshHistoryShowsForTheTeam();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(figures.Placed, Is.EqualTo(withOneFeatureAndAnIncident),
                    "Every record with one Feature among its links is placed, and nothing else is.");
                Assert.That(figures.NotPlaced, Is.EqualTo(withOnlyIncidents),
                    "Every record with no Feature among two or more links is refused, and nothing else is.");
                Assert.That(lone.ConvertAll(TheStoredParentOfTheWorkItem), Is.EqualTo(lone.ConvertAll(TheOneLinkOf)),
                    "A record with one link keeps exactly what it names. Changing it would change what every rule written against that value matches.");
            }
        }

        /// <summary>
        /// Placing reads only what the refresh already downloaded. The same Team, re-reading the same two
        /// records the same cheap way, asks the tracker exactly as often once each record names several
        /// issues as it did when each named one. This holds today and has to keep holding.
        /// </summary>
        [Test]
        [Category("kpi")]
        public async Task Reading_the_links_costs_the_tracker_no_extra_request()
        {
            TheTeamReadsItsParentsFromTheCausedByLink();
            ThePortfolioHoldsTheFeature(TheFeature);
            TheTeamHoldsTheWorkItem(TheItemCausedByItsFeatureAndAnIncident);
            TheIssueIsCausedBy(TheItemCausedByItsFeatureAndAnIncident, TheFeature);
            TheTeamHoldsTheWorkItem(TheItemCausedOnlyByIncidents);
            TheIssueIsCausedBy(TheItemCausedOnlyByIncidents, AnotherIncident);
            await ThePortfolioRefreshes();
            await TheTeamIsInSteadyUse();

            TheIssueMoves(TheItemCausedByItsFeatureAndAnIncident);
            TheIssueMoves(TheItemCausedOnlyByIncidents);
            await TheTeamRefreshes();
            var requestsWithOneLinkEach = RequestsTheLastRefreshMade;
            var withOneLinkEach = await WhatRefreshHistoryShowsForTheTeam();

            TheIssueIsCausedBy(TheItemCausedByItsFeatureAndAnIncident, AnIncident);
            TheIssueIsCausedBy(TheItemCausedOnlyByIncidents, AThirdIncident);
            TheIssueMoves(TheItemCausedByItsFeatureAndAnIncident);
            TheIssueMoves(TheItemCausedOnlyByIncidents);
            await TheTeamRefreshes();
            var withSeveralLinksEach = await WhatRefreshHistoryShowsForTheTeam();

            using (Assert.EnterMultipleScope())
            {
                Assert.That((withOneLinkEach.Mode, withSeveralLinksEach.Mode), Is.EqualTo(("Delta", "Delta")),
                    "positive control: both refreshes have to be the same cheap one, or the two counts measure different kinds of refresh.");
                Assert.That(requestsWithOneLinkEach, Is.Positive,
                    "positive control: a refresh that reached the tracker not at all would make any two counts equal.");
                Assert.That(RequestsTheLastRefreshMade, Is.EqualTo(requestsWithOneLinkEach),
                    "The links and the dates come with the records the search already returned. A request per record would make every refresh of a large Team slower for an answer that was already in hand.");
            }
        }

        private static readonly object[] MixesOfRecords =
        [
            new object[] { 3, 2, 2, 1 },
            new object[] { 1, 0, 0, 0 },
            new object[] { 0, 3, 1, 0 },
            new object[] { 0, 0, 3, 2 },
        ];

        /// <summary>Records numbered from PAY-500 up, so a mix never collides with the named records above.</summary>
        private List<string> TheTeamHoldsAMixOf(int withOneFeatureAndAnIncident, int withOnlyIncidents, int withOneLink, int withNoLink)
        {
            var next = 500;
            var lone = new List<string>();

            for (var i = 0; i < withOneFeatureAndAnIncident; i++)
            {
                var key = $"PAY-{next++}";
                TheTeamHoldsTheWorkItem(key);
                TheIssueIsCausedBy(key, TheFeature, $"INC-{next}");
            }

            for (var i = 0; i < withOnlyIncidents; i++)
            {
                var key = $"PAY-{next++}";
                TheTeamHoldsTheWorkItem(key);
                TheIssueIsCausedBy(key, $"INC-{next}", $"INC-{next + 1000}");
            }

            for (var i = 0; i < withOneLink; i++)
            {
                var key = $"PAY-{next++}";
                TheTeamHoldsTheWorkItem(key);
                TheIssueIsCausedBy(key, TheOneLinkOf(key));
                lone.Add(key);
            }

            for (var i = 0; i < withNoLink; i++)
            {
                TheTeamHoldsTheWorkItem($"PAY-{next++}");
            }

            return lone;
        }

        private static string TheOneLinkOf(string key) => $"INC-{key[4..]}";
    }
}
