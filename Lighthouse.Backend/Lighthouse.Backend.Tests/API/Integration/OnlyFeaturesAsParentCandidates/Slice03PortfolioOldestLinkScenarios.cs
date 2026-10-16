using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.OnlyFeaturesAsParentCandidates
{
    /// <summary>
    /// Acceptance scenarios: a Portfolio's Feature linked to several issues above it gets one
    /// parent, the one whose link was made first. Driving port: a Portfolio refresh through the running
    /// application.
    ///
    /// Nothing is set aside first at this level. The issues above a Feature are stored only once a refresh
    /// has read them, so "only the Features Lighthouse holds" would throw away every parent the first time
    /// it is seen. The pick alone decides, and every candidate is named in the
    /// line, so a Portfolio reading its links in the wrong direction stays easy to spot.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-6301-only-features-as-parent-candidates")]
    [Category("slice-03")]
    public class Slice03PortfolioOldestLinkTest : ParentPlacementAcceptanceTest
    {
        private const string TheFeature = "PAY-88";

        private const string TheFeatureCausedByAProblemFirst = "PAY-90";

        private const string TheFeatureWithUndatedLinks = "PAY-92";

        private const string TheFeatureThatNamesTheOlderInitiative = "PAY-93";

        private const string TheFeatureThatGainsASecondLink = "PAY-94";

        private const string TheInitiative = "PAY-7";

        private const string TheOtherInitiative = "PAY-8";

        private const string TheOlderInitiative = "PAY-9";

        private const string AProblemRecord = "PRB-120";

        private const string AnotherProblemRecord = "PRB-130";

        private const string TheTheme = "INIT-1";

        private const string TheOtherTheme = "INIT-2";

        private static readonly DateTime December1 = On(2025, 12, 1);

        private static readonly DateTime February11 = On(2026, 2, 11);

        [Test]
        [Category("kpi")]
        public async Task A_Feature_linked_to_two_issues_above_it_rolls_up_to_the_one_it_was_linked_to_first()
        {
            ThePortfolioReadsItsFeaturesParentsFromTheCausedByLink();
            ThePortfolioHoldsTheFeature(TheFeature);
            TheTrackerHoldsTheIssueAbove(TheInitiative, SomeLongAgoDay);
            TheTrackerHoldsTheIssueAbove(AProblemRecord, SomeLongAgoDay);
            TheIssueHasTheLinkHistory(TheFeature, LinkEvent.Linked(TheInitiative, December1), LinkEvent.Linked(AProblemRecord, February11));

            await ThePortfolioRefreshes();

            var figures = await WhatRefreshHistoryShowsForThePortfolio();
            var line = TheOneLineThatPlacedFor(ThePortfolio);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheStoredParentOfTheFeature(TheFeature), Is.EqualTo(TheInitiative),
                    "The Feature was linked to its Initiative first. Refusing it, as before, leaves it rolling up to nothing.");
                Assert.That(WhatTheParentOverrideFieldShowsOnTheFeature(TheFeature), Is.EqualTo(TheInitiative),
                    "The Feature shows the parent it was given, the same way a Work Item does.");
                Assert.That(figures.Placed, Is.EqualTo(1),
                    "An administrator who only runs Portfolios reads only this row, so the placement is counted here.");
                Assert.That(figures.NotPlaced, Is.Zero,
                    "At this level the pick always ends somewhere, so nothing is left not placed.");
                Assert.That(line, Is.EqualTo(
                    $"{ThePortfolio} placed 1 record whose links name more than one issue: "
                    + $"{TheFeature} under {TheInitiative} (oldest link, {Day(December1)}; set aside {AProblemRecord}, {Day(February11)})"),
                    "The Portfolio's line reads like the Team's, in the Feature term, so one habit of checking serves both.");
            }
        }

        /// <summary>
        /// A problem record linked before the Initiative wins, because nothing is set aside at this level.
        /// The line names it plainly, so the administrator can see a link that also carries causes.
        /// </summary>
        [Test]
        [Category("edge")]
        public async Task At_Portfolio_level_nothing_is_set_aside_before_the_oldest_link_is_picked()
        {
            ThePortfolioReadsItsFeaturesParentsFromTheCausedByLink();
            ThePortfolioHoldsTheFeature(TheFeatureCausedByAProblemFirst);
            TheTrackerHoldsTheIssueAbove(AnotherProblemRecord, SomeLongAgoDay);
            TheTrackerHoldsTheIssueAbove(TheOtherInitiative, SomeLongAgoDay);
            TheIssueHasTheLinkHistory(
                TheFeatureCausedByAProblemFirst,
                LinkEvent.Linked(AnotherProblemRecord, On(2026, 1, 5)),
                LinkEvent.Linked(TheOtherInitiative, On(2026, 3, 1)));

            await ThePortfolioRefreshes();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheStoredParentOfTheFeature(TheFeatureCausedByAProblemFirst), Is.EqualTo(AnotherProblemRecord),
                    "At this level the pick alone decides, because filtering by the level above would need a list of hierarchy issues Lighthouse does not have.");
                Assert.That(TheOneLineThatPlacedFor(ThePortfolio), Does.Contain(
                    $"{TheFeatureCausedByAProblemFirst} under {AnotherProblemRecord} (oldest link, 2026-01-05; set aside {TheOtherInitiative}, 2026-03-01)"),
                    "A pick under a problem record is exactly what the administrator needs to see in order to question it.");
            }
        }

        [Test]
        [Category("edge")]
        public async Task Undated_parents_that_are_not_stored_fall_back_to_the_lowest_key()
        {
            ThePortfolioReadsItsFeaturesParentsFromTheCausedByLink();
            ThePortfolioHoldsTheFeature(TheFeatureWithUndatedLinks);
            TheIssueIsCausedBy(TheFeatureWithUndatedLinks, TheOlderInitiative, TheInitiative);

            await ThePortfolioRefreshes();

            var figures = await WhatRefreshHistoryShowsForThePortfolio();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheStoredParentOfTheFeature(TheFeatureWithUndatedLinks), Is.EqualTo(TheInitiative),
                    "No link is dated and neither parent is stored yet, so nothing but the key can decide, and it decides the same way every time.");
                Assert.That(TheOneLineThatPlacedFor(ThePortfolio), Does.Contain(
                    $"{TheFeatureWithUndatedLinks} under {TheInitiative} (lowest key; set aside {TheOlderInitiative})"),
                    "The line says the key decided.");
                Assert.That(figures.NotPlaced, Is.Zero,
                    "A Feature at this level is never left not placed: the key always ends the pick.");
            }
        }

        /// <summary>
        /// The one documented move. First read, neither parent above is stored, so the key decides. That
        /// refresh stores the parents, and the next reading of the Feature, once it moves, finds the older one
        /// by its created date. After that the answer holds.
        /// </summary>
        [Test]
        [Category("edge")]
        public async Task An_undated_Feature_moves_once_to_the_older_parent_after_the_parents_are_stored_and_then_holds()
        {
            ThePortfolioReadsItsFeaturesParentsFromTheCausedByLink();
            TheTrackerHoldsTheIssueAbove(TheInitiative, On(2026, 2, 1));
            TheTrackerHoldsTheIssueAbove(TheOlderInitiative, On(2025, 10, 1));
            ThePortfolioHoldsTheFeature(TheFeatureWithUndatedLinks);
            TheIssueIsCausedBy(TheFeatureWithUndatedLinks, TheInitiative, TheOlderInitiative);
            ThePortfolioHoldsTheFeature(TheFeatureThatNamesTheOlderInitiative);
            TheIssueIsCausedBy(TheFeatureThatNamesTheOlderInitiative, TheOlderInitiative);

            await ThePortfolioRefreshes();
            var afterTheFirstReading = TheStoredParentOfTheFeature(TheFeatureWithUndatedLinks);
            var firstMode = (await WhatRefreshHistoryShowsForThePortfolio()).Mode;
            TheIssueMoves(TheFeatureWithUndatedLinks);
            await ThePortfolioRefreshes();
            var afterTheParentsWereStored = TheStoredParentOfTheFeature(TheFeatureWithUndatedLinks);
            var secondMode = (await WhatRefreshHistoryShowsForThePortfolio()).Mode;
            TheIssueMoves(TheFeatureWithUndatedLinks);
            await ThePortfolioRefreshes();
            var thirdMode = (await WhatRefreshHistoryShowsForThePortfolio()).Mode;

            using (Assert.EnterMultipleScope())
            {
                Assert.That((firstMode, secondMode, thirdMode), Is.EqualTo(("Full", "Delta", "Delta")),
                    "positive control: the first refresh reads every Feature and stores the parents, and each later one is the cheap one that re-reads the Feature because it moved.");
                Assert.That(
                    new[] { afterTheFirstReading, afterTheParentsWereStored, TheStoredParentOfTheFeature(TheFeatureWithUndatedLinks) },
                    Is.EqualTo(TheOneMoveThenItHolds),
                    "Once both parents are stored, their created dates are known and the older one wins. It moves once, because the stored parents changed, and then it holds.");
            }
        }

        /// <summary>
        /// A Portfolio pointed at a phrase both ends of the link share reads its Feature's children as well
        /// as its parent. The Feature is placed under one of them, and the line names every candidate, which
        /// is how the administrator recognises the configuration.
        /// </summary>
        [Test]
        [Category("error")]
        public async Task A_Portfolio_pointed_at_a_link_that_also_reads_downward_names_every_candidate()
        {
            ThePortfolioReadsItsFeaturesParentsFromAPhraseBothEndsShare();
            ThePortfolioHoldsTheFeature(TheFeature);
            TheTrackerHoldsTheIssueAbove(TheInitiative, SomeLongAgoDay);
            TheIssueHasTheLinkHistory(TheFeature, LinkEvent.Linked(TheInitiative, December1));
            TheIssueCauses(TheFeature, TwelveChildren);

            await ThePortfolioRefreshes();

            var figures = await WhatRefreshHistoryShowsForThePortfolio();
            var line = TheOneLineThatPlacedFor(ThePortfolio);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(line, Does.Contain($"{TheFeature} under {TheInitiative} (oldest link, {Day(December1)}; set aside "),
                    "The only dated link wins, and the line says so.");
                Assert.That(TwelveChildren.Where(child => !line.Contains(child, StringComparison.Ordinal)), Is.Empty,
                    "Twelve children set aside, all named: a line that names a handful hides how far the reading went wrong.");
                Assert.That(figures.Placed, Is.EqualTo(1),
                    "Refresh History shows the placement for this Portfolio, which is where the administrator starts looking.");
            }
        }

        /// <summary>A cheap Portfolio refresh places what it downloaded by the same rule, and counts it.</summary>
        [Test]
        [Category("edge")]
        public async Task A_cheap_Portfolio_refresh_places_and_counts_what_it_downloaded()
        {
            ThePortfolioReadsItsFeaturesParentsFromTheCausedByLink();
            ThePortfolioHoldsTheFeature(TheFeatureThatGainsASecondLink);
            TheTrackerHoldsTheIssueAbove(TheInitiative, SomeLongAgoDay);
            TheTrackerHoldsTheIssueAbove(AProblemRecord, SomeLongAgoDay);
            TheIssueHasTheLinkHistory(TheFeatureThatGainsASecondLink, LinkEvent.Linked(TheInitiative, December1));
            await ThePortfolioIsInSteadyUse();

            TheIssueHasTheLinkHistory(TheFeatureThatGainsASecondLink, LinkEvent.Linked(AProblemRecord, February11));
            TheIssueMoves(TheFeatureThatGainsASecondLink);
            await ThePortfolioRefreshes();

            var figures = await WhatRefreshHistoryShowsForThePortfolio();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheStoredParentOfTheFeature(TheFeatureThatGainsASecondLink), Is.EqualTo(TheInitiative),
                    "A second link made later does not move the Feature: its first link is still the oldest.");
                Assert.That(figures.Mode, Is.EqualTo("Delta"),
                    "positive control: this has to be the cheap refresh, or it says nothing about one.");
                Assert.That(figures.Placed, Is.EqualTo(1),
                    "The cheap refresh downloaded this Feature and placed it, so it counts it.");
            }
        }

        /// <summary>
        /// The issues above a Portfolio's Features are fetched in a sweep of their own, and their links can
        /// name several issues too. They are placed by the same rule and named on a line of their own, but
        /// not counted on the Portfolio's row, which counts only the Portfolio's own Features.
        /// </summary>
        [Test]
        [Category("edge")]
        public async Task The_issues_above_are_placed_too_named_on_their_own_line_and_not_counted()
        {
            ThePortfolioReadsItsFeaturesParentsFromTheCausedByLink();
            ThePortfolioHoldsTheFeature(TheFeature);
            TheIssueIsCausedBy(TheFeature, TheInitiative);
            TheTrackerHoldsTheIssueAbove(TheInitiative, SomeLongAgoDay);
            TheIssueHasTheLinkHistory(TheInitiative, LinkEvent.Linked(TheTheme, On(2025, 1, 10)), LinkEvent.Linked(TheOtherTheme, On(2025, 3, 1)));

            await ThePortfolioRefreshes();

            var figures = await WhatRefreshHistoryShowsForThePortfolio();
            var line = TheOneLineThatPlacedFor(TheParentsOfThePortfoliosFeatures);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheStoredParentOfTheFeature(TheInitiative), Is.EqualTo(TheTheme),
                    "An Initiative linked to two themes would otherwise keep refusing, one level up, the very thing its Features no longer do.");
                Assert.That(line, Does.Contain($"{TheInitiative} under {TheTheme} (oldest link, 2025-01-10; set aside {TheOtherTheme}, 2025-03-01)"),
                    "The line names its owner as the level above, so it is not mistaken for one of the Portfolio's own Features.");
                Assert.That(figures.Placed, Is.Zero,
                    "The Portfolio's row counts its own Features, and none of them had more than one candidate.");
            }
        }

        /// <summary>
        /// A Portfolio Feature can also be the issue above another of its Features, so the sweep for the
        /// issues above downloads it a second time in the same refresh. By then that refresh has stored the
        /// created dates the first placing could not see, so placing it again could pick another parent, and
        /// the counted line would name a parent that was never stored.
        /// </summary>
        [Test]
        [Category("edge")]
        public async Task A_Feature_that_is_also_the_parent_of_another_Feature_is_placed_once_and_stored_as_its_line_says()
        {
            ThePortfolioReadsItsFeaturesParentsFromTheCausedByLink();
            ThePortfolioHoldsTheFeature(TheLaterCreatedFeature, On(2025, 6, 1));
            ThePortfolioHoldsTheFeature(TheEarlierCreatedFeature, On(2025, 1, 1));
            ThePortfolioHoldsTheFeature(TheFeatureAboveAnother);
            ThePortfolioHoldsTheFeature(TheFeatureBelowIt);
            TheIssueIsCausedBy(TheFeatureAboveAnother, TheEarlierCreatedFeature, TheLaterCreatedFeature);
            TheIssueIsCausedBy(TheFeatureBelowIt, TheFeatureAboveAnother);

            await ThePortfolioRefreshes();

            var placedLine = TheOneLineThatPlacedFor(ThePortfolio);
            var linesThatPlacedIt = WhatTheLastRefreshWarned().Count(line => line.Contains($"{TheFeatureAboveAnother} under ", StringComparison.Ordinal));
            var parentsLines = WhatTheLastRefreshWarned().Where(line => line.StartsWith(TheParentsOfThePortfoliosFeatures, StringComparison.Ordinal));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(placedLine, Does.Contain($"{TheFeatureAboveAnother} under {TheStoredParentOfTheFeature(TheFeatureAboveAnother)} ("),
                    "The counted line has to name the parent that was stored, or the administrator checks an answer Lighthouse did not keep.");
                Assert.That(linesThatPlacedIt, Is.EqualTo(1),
                    "One refresh places a record once. Two lines with two different answers leave nobody able to tell which one holds.");
                Assert.That(parentsLines.Where(line => line.Contains(TheFeatureAboveAnother, StringComparison.Ordinal)), Is.Empty,
                    "The line for the issues above exists so they are not mistaken for the Portfolio's own Features, so it must not name one.");
            }
        }

        private const string TheLaterCreatedFeature = "PAY-10";

        private const string TheEarlierCreatedFeature = "PAY-20";

        private const string TheFeatureAboveAnother = "PAY-30";

        private const string TheFeatureBelowIt = "PAY-40";

        private static readonly string[] TwelveChildren = [.. Enumerable.Range(300, 12).Select(number => $"PAY-{number}")];

        private static readonly string[] TheOneMoveThenItHolds = [TheInitiative, TheOlderInitiative, TheOlderInitiative];
    }
}
