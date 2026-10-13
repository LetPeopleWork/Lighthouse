namespace Lighthouse.Backend.Tests.API.Integration.TeamFeatureSize
{
    /// <summary>
    /// Story #6251, slice 02: Feature Size Percentiles on a Team's Flow Overview, read at
    /// GET /api/latest/teams/{id}/metrics/featureSizePercentilesInfo.
    ///
    /// The status and the widget's wording are worked out in the browser from data slice 01 already
    /// serves, so the frontend suites hold them.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-6251-team-feature-size")]
    [Category("slice-02")]
    public partial class Slice02TeamFeatureSizePercentilesTest
    {
        // Gravity's parts of the ten Features closed in the last 30 days are 1 to 10. Sort the sizes
        // from smallest to largest; the Nth percentile is the size at place N% of the count, rounded
        // down, counting the smallest as place 1, or the smallest when that place comes to less than
        // 1. So its percentiles read 5, 7, 8 and 9. In the 30 days before, its parts were 2, 2, 3 and
        // 3, a median of 2: the trend is up.
        [Test]
        [Category("us-02")]
        [Category("walking_skeleton")]
        [Category("contract-shape:pure-function")]
        public async Task A_Team_sees_the_percentiles_of_its_parts_of_the_Features_it_finished_against_the_period_before()
        {
            GivenGravityClosedTenFeaturesInTheWindowWithPartsOneToTen();
            GivenInThePeriodBeforeGravitysPartsWere(2, 2, 3, 3);

            await WhenTheFeatureSizePercentilesAreAskedFor(Gravity);

            ThenThePercentilesAre(5, 7, 8, 9);
            ThenTheTrendReads("up", previousMedian: 2);
        }

        // The direction is taken from the 50th percentile alone.
        [TestCase("down", 8, 8, 8, 9, 9)]
        [TestCase("flat", 5, 5, 5, 5, 5)]
        [Category("us-02")]
        [Category("contract-shape:pure-function")]
        public async Task The_trend_follows_the_Team_s_median_part(string direction, int previousMedian, params int[] partsInThePeriodBefore)
        {
            GivenGravityClosedTenFeaturesInTheWindowWithPartsOneToTen();
            GivenInThePeriodBeforeGravitysPartsWere(partsInThePeriodBefore);

            await WhenTheFeatureSizePercentilesAreAskedFor(Gravity);

            ThenTheTrendReads(direction, previousMedian);
        }

        // Closed in the window as well: a Feature sized by the Portfolio's default with 60 made-up
        // Work Items for Gravity, one only Zenith worked on, and one Gravity's part of dropped to
        // nothing. None of them moves Gravity's percentiles.
        [Test]
        [Category("us-02")]
        [Category("error")]
        [Category("contract-shape:pure-function")]
        public async Task Features_the_Team_has_no_real_part_of_are_left_out_of_its_percentiles()
        {
            GivenGravityClosedTenFeaturesInTheWindowWithPartsOneToTen();
            GivenFeaturesClosedInTheWindowThatAreNotGravitys();

            await WhenTheFeatureSizePercentilesAreAskedFor(Gravity);

            ThenThePercentilesAre(5, 7, 8, 9);
        }

        // Only open Features: nothing finished, so nothing to take percentiles of. The answer says so
        // with no percentiles at all, which the widget shows as its no-data state, never as zeros.
        [Test]
        [Category("us-07")]
        [Category("error")]
        [Category("contract-shape:pure-function")]
        public async Task A_Team_that_finished_no_Feature_in_the_window_has_no_percentiles_rather_than_zeros()
        {
            GivenGravityHasOnlyOpenFeatures();

            await WhenTheFeatureSizePercentilesAreAskedFor(Gravity);

            ThenThereAreNoPercentiles();
        }

        [Test]
        [Category("us-02")]
        [Category("error")]
        [Category("contract-shape:pure-function")]
        public async Task Someone_who_cannot_read_the_Team_is_told_there_is_no_such_Team_while_its_reader_is_answered()
        {
            GivenGravityClosedTenFeaturesInTheWindowWithPartsOneToTen();

            await WhenAReaderOfGravityAndAReaderOfZenithBothAskForGravitysFeatureSizePercentiles();

            ThenOnlyTheReaderOfTheTeamIsAnswered();
        }

        // Asked by a System Admin, who may read every Team, so only the Team's absence can explain
        // the answer; the same request for Gravity is answered.
        [Test]
        [Category("us-02")]
        [Category("error")]
        [Category("contract-shape:pure-function")]
        public async Task A_Team_that_does_not_exist_has_no_Feature_Size_Percentiles()
        {
            GivenGravityClosedTenFeaturesInTheWindowWithPartsOneToTen();

            await WhenTheFeatureSizePercentilesOfATeamThatDoesNotExistAreAskedFor();
            ThenTheAnswerSaysThereIsNoSuchTeam();

            await WhenTheFeatureSizePercentilesAreAskedFor(Gravity);
            ThenTheAnswerArrived();
        }

        [Test]
        [Category("us-02")]
        [Category("error")]
        [Category("contract-shape:pure-function")]
        public async Task A_window_that_ends_before_it_starts_is_refused()
        {
            GivenGravityClosedTenFeaturesInTheWindowWithPartsOneToTen();

            await WhenTheFeatureSizePercentilesAreAskedForAWindowThatEndsBeforeItStarts(Gravity);

            ThenTheWindowIsRefused();
        }

        // A window is read in whole days with both ends counted, so one that starts and ends on the
        // same day is a day long rather than refused, and the period before it is the day before.
        // Gravity's part of the Feature it closed three days ago is 3, and of the one it closed the
        // day before that is 4: the trend is down.
        [Test]
        [Category("us-02")]
        [Category("contract-shape:pure-function")]
        public async Task A_one_day_window_is_compared_with_the_day_before_it()
        {
            GivenGravityClosedTenFeaturesInTheWindowWithPartsOneToTen();

            await WhenTheFeatureSizePercentilesAreAskedForTheSingleDay(Gravity, daysAgo: 3);

            ThenThePercentilesAre(3, 3, 3, 3);
            ThenTheTrendReads("down", previousMedian: 4);
        }

        // The period before a 30-day window is the 30 days just before it, ending the day before the
        // window starts. Gravity closed one Feature on its first day, one on the window's first day,
        // and one the day before the period before began; only the first counts as before.
        [Test]
        [Category("us-02")]
        [Category("contract-shape:pure-function")]
        public async Task The_period_before_is_as_long_as_the_window_and_ends_the_day_before_it_starts()
        {
            GivenGravityClosedTenFeaturesInTheWindowWithPartsOneToTen();
            GivenGravityClosedFeaturesOnTheEdgesOfThePeriodBefore();

            await WhenTheFeatureSizePercentilesAreAskedFor(Gravity);

            ThenThePeriodBeforeRuns(WindowStart.AddDays(-30), WindowStart.AddDays(-1));
            ThenTheMedianOfThePeriodBeforeIs(6);
        }

        // Every Feature is loaded with what hangs off it, so a database command per Feature would
        // slip in easily. Each answer is asked for right after a refresh, so it is computed afresh
        // and never served from what the Team remembers.
        [Test]
        [Category("us-02")]
        [Category("perf")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task Asking_a_Team_for_its_Feature_Size_Percentiles_costs_as_much_of_the_database_for_twenty_Features_as_for_two()
        {
            GivenGravityWorksOnFeatures(2);

            await WhenGravityIsRefreshedAndAskedFor(FeatureSizePercentilesInfo);
            ThenThePercentilesAreThoseOfTheFeaturesGravityWorksOn(2);

            GivenGravityHasTakenOnMoreFeatures(18);
            await WhenGravityIsRefreshedAndAskedFor(FeatureSizePercentilesInfo);
            ThenThePercentilesAreThoseOfTheFeaturesGravityWorksOn(20);

            ThenTheLastAnswerCostAsManyCommandsAsTheFirst();
        }

        // The Portfolio's percentiles count every Team's Work Items, as before. Green before and after.
        [Test]
        [Category("us-02")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task The_Portfolio_s_percentiles_still_count_every_Team_s_Work_Items()
        {
            GivenGravityClosedTenFeaturesInTheWindowWithPartsOneToTen();

            await WhenOceanExplorersFeatureSizePercentilesAreAskedFor();

            ThenThePercentilesAre(25, 27, 28, 29);
        }
    }
}
