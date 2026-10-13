namespace Lighthouse.Backend.Tests.API.Integration.TeamFeatureSize
{
    /// <summary>
    /// Story #6251, slice 00: a Team's numbers are always the Team's own. Gravity's Flow Overview
    /// reads Total Throughput 3 and Cycle Time 50th 2, whichever page was opened first, never
    /// Ocean Explorer's 5 and 21.
    ///
    /// Teams and Portfolios are numbered in separate tables, so the first Team and the first Portfolio
    /// of every instance are both number 1. Both remember their metrics in one cache, under names
    /// built from the number and the metric alone, so the owner asked second is served the first
    /// owner's answer, and refreshing either one forgets the other's answers too.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-6251-team-feature-size")]
    [Category("slice-00")]
    public partial class Slice00OwnerScopedMetricsTest
    {
        // Gravity finished 3 Work Items in the last 30 days, each in 2 to 4 days; Ocean Explorer
        // closed 5 Features, each after 21 days.
        [TestCase(RememberedMetric.TotalThroughput, Owner.Team)]
        [TestCase(RememberedMetric.TotalThroughput, Owner.Portfolio)]
        [TestCase(RememberedMetric.CycleTimePercentiles, Owner.Team)]
        [TestCase(RememberedMetric.CycleTimePercentiles, Owner.Portfolio)]
        [Category("us-00")]
        [Category("walking_skeleton")]
        [Category("error")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task A_Team_and_a_Portfolio_with_the_same_number_each_see_their_own_numbers_whichever_is_asked_first(
            RememberedMetric metric, Owner askedFirst)
        {
            GivenATeamAndAPortfolioWithTheSameNumber();
            GivenTheTeamFinishedThreeWorkItemsAndThePortfolioClosedFiveFeatures();

            await WhenBothAreAskedFor(metric, askedFirst);

            ThenTheTeamAndThePortfolioShareANumber();
            ThenEachSawItsOwn(metric);
        }

        // Each owner is asked about a different window, so the two never share a remembered answer
        // and this is only about what a refresh forgets. New work lands for both, then one of them is
        // refreshed the way a sync ends: that one counts the new work, and the other keeps the answer
        // it remembered until it is refreshed itself.
        [TestCase(Owner.Team)]
        [TestCase(Owner.Portfolio)]
        [Category("us-00")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task Refreshing_one_owner_leaves_the_numbers_remembered_for_the_other_alone(Owner refreshed)
        {
            GivenATeamAndAPortfolioWithTheSameNumber();
            GivenTheTeamFinishedThreeWorkItemsAndThePortfolioClosedFiveFeatures();

            await WhenEachShowsItsTotalThroughputForItsOwnWindow();
            ThenTheTeamAndThePortfolioShareANumber();
            ThenEachShowedWhatItHadFinished();

            GivenEachHasFinishedOneMore();
            WhenOnlyThisOneIsRefreshed(refreshed);

            await ThenTheRefreshedOneCountsTheNewWorkAndTheOtherStillShowsWhatItRemembered(refreshed);
        }

        // Zenith is the instance's second Team and no Portfolio is number 2, so nothing it remembers
        // could ever be mistaken for a Portfolio's. Green before and after this story.
        [TestCase(RememberedMetric.TotalThroughput)]
        [TestCase(RememberedMetric.CycleTimePercentiles)]
        [Category("us-00")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task A_Team_whose_number_no_Portfolio_shares_reads_its_own_numbers(RememberedMetric metric)
        {
            GivenATeamWhoseNumberNoPortfolioShares();
            GivenTheTeamFinishedThreeWorkItemsAndThePortfolioClosedFiveFeatures();

            await WhenBothAreAskedFor(metric, askedFirst: Owner.Portfolio);

            ThenTheTeamsNumberIsNotThePortfolios();
            ThenEachSawItsOwn(metric);
        }
    }
}
