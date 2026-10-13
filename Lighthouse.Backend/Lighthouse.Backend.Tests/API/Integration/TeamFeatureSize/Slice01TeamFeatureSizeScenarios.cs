namespace Lighthouse.Backend.Tests.API.Integration.TeamFeatureSize
{
    /// <summary>
    /// Story #6251, slice 01: the Feature Size scatter on a Team, read at
    /// GET /api/latest/teams/{id}/metrics/allFeaturesForSizeChart and …/sizePercentiles, and the
    /// sizes in the Team's "Features worked on" at …/featuresInProgress.
    ///
    /// On a Team, a Feature is as big as this Team's part of it: how many of this Team's Work Items
    /// sit under it. The world most scenarios share holds the demo Portfolio Ocean Explorer's
    /// Features that several Teams work on, with the demo's numbers, plus a few made up for the rules
    /// the numbers turn on. The scatter's looks and copy are held by the frontend suites; these hold
    /// the numbers.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-6251-team-feature-size")]
    [Category("slice-01")]
    public partial class Slice01TeamFeatureSizeTest
    {
        // OE-002 holds 8 of Equinox's Work Items, 4 of Gravity's, 4 of Lightspeed's and 3 of
        // Zenith's. On Gravity it is 4, not 19.
        [Test]
        [Category("us-01")]
        [Category("walking_skeleton")]
        [Category("contract-shape:pure-function")]
        public async Task A_Team_sees_a_Feature_four_Teams_work_on_as_big_as_its_own_part_of_it()
        {
            GivenTheOceanExplorerPortfolioAndTheTeamsThatWorkOnIt();

            await WhenTheFeatureSizesAreAskedFor(Gravity);

            ThenTheFeatureReads(CoralReefRestoration, 4);
        }

        [TestCase(CoralReefRestoration, 19)]
        [TestCase(KelpForestConservation, 10)]
        [TestCase(DeepSeaMapping, 17)]
        [TestCase(DeepWaterMineralSurvey, 10)]
        [TestCase(OnlyGravityWorkedOnIt, 4)]
        [Category("us-01")]
        [Category("kpi")]
        [Category("contract-shape:pure-function")]
        public async Task The_parts_each_Team_sees_add_up_to_the_Feature_s_size_on_its_Portfolio(string feature, int sizeOnThePortfolio)
        {
            GivenTheOceanExplorerPortfolioAndTheTeamsThatWorkOnIt();

            await WhenEveryTeamThatWorksOnOceanExplorerIsAskedForItsPartOf(feature);
            await WhenOceanExplorersFeatureSizesAreAskedFor();

            ThenThePartsAddUpTo(sizeOnThePortfolio);
            ThenTheFeatureReads(feature, sizeOnThePortfolio);
        }

        // Done Features closed in the window, and every To Do and In Progress Feature whatever the
        // window. A Feature Gravity closed before the window is not among them.
        [Test]
        [Category("us-01")]
        [Category("us-06")]
        [Category("contract-shape:pure-function")]
        public async Task A_Team_sees_the_Features_closed_in_the_window_and_every_open_one()
        {
            GivenTheOceanExplorerPortfolioAndTheTeamsThatWorkOnIt();

            await WhenTheFeatureSizesAreAskedFor(Gravity);

            ThenTheFeaturesShownAre(GravitysFeaturesInTheWindow);
        }

        // Zenith alone works on one, and Gravity's part of the other dropped to nothing. Both belong
        // to the Portfolio Gravity works in, and neither is Gravity's.
        [Test]
        [Category("us-05")]
        [Category("error")]
        [Category("contract-shape:pure-function")]
        public async Task A_Feature_the_Team_has_no_Work_Item_in_is_not_shown_even_from_the_Portfolio_it_works_in()
        {
            GivenTheOceanExplorerPortfolioAndTheTeamsThatWorkOnIt();

            await WhenTheFeatureSizesAreAskedFor(Gravity);

            ThenTheseFeaturesAreNotShown(OnlyZenithWorksOnIt, GravitysPartDroppedToNothing);
        }

        // Sized by Ocean Explorer's Default Feature Size, which hands Gravity 10 made-up Work Items it
        // never had. It is not broken down, so it has no real part to show.
        [Test]
        [Category("us-05")]
        [Category("error")]
        [Category("contract-shape:pure-function")]
        public async Task A_Feature_sized_by_its_Portfolio_s_default_is_not_shown_even_with_work_made_up_for_the_Team()
        {
            GivenTheOceanExplorerPortfolioAndTheTeamsThatWorkOnIt();

            await WhenTheFeatureSizesAreAskedFor(Gravity);

            ThenTheseFeaturesAreNotShown(SizedByThePortfolioDefault);
        }

        // OR-001 sits in Orion, a Portfolio this reader may not read and Gravity is no part of. It
        // still counts on Gravity, but the answer does not tell this reader what Orion is called.
        [Test]
        [Category("us-04")]
        [Category("contract-shape:pure-function")]
        [Ignore(Pending)]
        public async Task A_reader_of_the_Team_sees_its_part_of_a_Feature_from_a_Portfolio_they_cannot_read_without_learning_its_name()
        {
            GivenTheOceanExplorerPortfolioAndTheTeamsThatWorkOnIt();

            await WhenSomeoneWhoReadsTheTeamAndOceanExplorerAsksForItsFeatureSizes(Gravity);

            ThenTheFeatureReads(OrionsFeature, 2);
            ThenTheFeatureNamesThePortfolios(OrionsFeature);
            ThenTheFeatureNamesThePortfolios(CoralReefRestoration, OceanExplorer);
        }

        [Test]
        [Category("us-04")]
        [Category("contract-shape:pure-function")]
        [Ignore(Pending)]
        public async Task Someone_who_may_read_every_Portfolio_sees_each_Feature_name_the_Portfolio_it_comes_from()
        {
            GivenTheOceanExplorerPortfolioAndTheTeamsThatWorkOnIt();

            await WhenTheFeatureSizesAreAskedFor(Gravity);

            ThenTheFeatureNamesThePortfolios(OrionsFeature, Orion);
            ThenTheFeatureNamesThePortfolios(CoralReefRestoration, OceanExplorer);
        }

        // Turned away the way every Team metric turns away a reader: told there is no such Team, so
        // the answer does not even confirm the Team exists.
        [TestCase(AllFeaturesForSizeChart)]
        [TestCase(SizePercentiles)]
        [Category("us-04")]
        [Category("error")]
        [Category("contract-shape:pure-function")]
        [Ignore(Pending)]
        public async Task Someone_who_cannot_read_the_Team_is_told_there_is_no_such_Team_while_its_reader_is_answered(string metric)
        {
            GivenTheOceanExplorerPortfolioAndTheTeamsThatWorkOnIt();

            await WhenAReaderOfGravityAndAReaderOfZenithBothAskForGravitys(metric);

            ThenOnlyTheReaderOfTheTeamIsAnswered();
        }

        // The "Features worked on" list carries Feature sizes too, so it turns away the same reader.
        // Green before and after this story: the list already answers only readers of the Team.
        [Test]
        [Category("us-04")]
        [Category("error")]
        [Category("contract-shape:pure-function")]
        public async Task A_reader_who_cannot_read_the_Team_gets_no_Feature_sizes_from_the_Features_it_is_working_on_either()
        {
            GivenTheOceanExplorerPortfolioAndTheTeamsThatWorkOnIt();
            GivenGravityIsWorkingOn(CoralReefRestoration);

            await WhenAReaderOfGravityAndAReaderOfZenithBothAskForTheFeaturesGravityIsWorkingOn();

            ThenOnlyTheReaderOfTheTeamIsAnswered();
        }

        // Asked by a System Admin, who may read every Team, so only the Team's absence can explain
        // the answer; the same request for Gravity is answered.
        [TestCase(AllFeaturesForSizeChart)]
        [TestCase(SizePercentiles)]
        [Category("us-01")]
        [Category("error")]
        [Category("contract-shape:pure-function")]
        [Ignore(Pending)]
        public async Task A_Team_that_does_not_exist_has_no_Feature_sizes(string metric)
        {
            GivenTheOceanExplorerPortfolioAndTheTeamsThatWorkOnIt();

            await WhenTheFeatureSizesOfATeamThatDoesNotExistAreAskedFor(metric);
            ThenTheAnswerSaysThereIsNoSuchTeam();

            await WhenGravityIsAskedFor(metric);
            ThenTheAnswerArrived();
        }

        [TestCase(AllFeaturesForSizeChart)]
        [TestCase(SizePercentiles)]
        [Category("us-01")]
        [Category("error")]
        [Category("contract-shape:pure-function")]
        [Ignore(Pending)]
        public async Task A_window_that_ends_before_it_starts_is_refused(string metric)
        {
            GivenTheOceanExplorerPortfolioAndTheTeamsThatWorkOnIt();

            await WhenAskedForAWindowThatEndsBeforeItStarts(Gravity, metric);

            ThenTheWindowIsRefused();
        }

        // Lighthouse tracks only the Features some Portfolio loads. A Team with no Work Item at all
        // and a Team whose Work Items sit under a parent no Portfolio loads both get an empty answer,
        // which the scatter shows as its empty state.
        [TestCase(PolarisHas.NoWorkItemAtAll)]
        [TestCase(PolarisHas.WorkOnlyUnderAParentNoPortfolioLoads)]
        [Category("us-03")]
        [Category("error")]
        [Category("contract-shape:pure-function")]
        [Ignore(Pending)]
        public async Task A_Team_none_of_whose_Work_Items_belong_to_a_tracked_Feature_has_no_Features_to_show(PolarisHas work)
        {
            GivenTheOceanExplorerPortfolioAndTheTeamsThatWorkOnIt();
            GivenPolaris(work);

            await WhenTheFeatureSizesAreAskedFor(Polaris);

            ThenNoFeatureIsShown();
        }

        // Gravity's parts of ten Features closed in the window are 1 to 10, while Zenith has 20 Work
        // Items under each. Sort the sizes from smallest to largest; the Nth percentile is the size
        // at place N% of the count, rounded down, counting the smallest as place 1, or the smallest
        // when that place comes to less than 1. So Gravity reads 5, 7, 8 and 9. An open Feature, a
        // default-sized one and one closed before the window are all bigger, and none of them counts.
        [Test]
        [Category("us-01")]
        [Category("contract-shape:pure-function")]
        [Ignore(Pending)]
        public async Task A_Team_s_size_percentiles_come_from_its_parts_of_the_Features_closed_in_the_window()
        {
            GivenGravityClosedTenFeaturesInTheWindowWithPartsOneToTen();
            GivenBiggerFeaturesThatDoNotCount();

            await WhenTheSizePercentilesAreAskedFor(Gravity);

            ThenTheSizePercentilesAre(5, 7, 8, 9);
        }

        [Test]
        [Category("us-01")]
        [Category("error")]
        [Category("contract-shape:pure-function")]
        [Ignore(Pending)]
        public async Task A_Team_with_no_Feature_closed_in_the_window_has_no_size_percentiles()
        {
            GivenBiggerFeaturesThatDoNotCount();

            await WhenTheSizePercentilesAreAskedFor(Gravity);

            ThenThereAreNoSizePercentiles();
        }

        // The "Features worked on" list on a Team's page offers a Size column. On a Team it shows
        // the same part the scatter shows, never the whole Feature's 19 or 10.
        [TestCase(Gravity, CoralReefRestoration, 4)]
        [TestCase(Gravity, KelpForestConservation, 4)]
        [TestCase(Zenith, CoralReefRestoration, 3)]
        [Category("us-01")]
        [Category("contract-shape:bounded-change")]
        [Ignore(Pending)]
        public async Task The_Features_a_Team_is_working_on_are_as_big_as_its_own_part_of_them(string team, string feature, int part)
        {
            GivenTheOceanExplorerPortfolioAndTheTeamsThatWorkOnIt();
            GivenTheTeamIsWorkingOn(team, feature);

            await WhenTheFeaturesWorkedOnAreAskedFor(team);

            ThenTheFeatureReads(feature, part);
        }

        // Every Feature is loaded with what hangs off it, so a database command per Feature would
        // slip in easily. Each answer is asked for right after a refresh, so it is computed afresh
        // and never served from what the Team remembers.
        [TestCase(AllFeaturesForSizeChart)]
        [TestCase(SizePercentiles)]
        [Category("us-01")]
        [Category("perf")]
        [Category("contract-shape:unbounded-preservation")]
        [Ignore(Pending)]
        public async Task Asking_a_Team_for_its_Feature_sizes_costs_as_much_of_the_database_for_twenty_Features_as_for_two(string metric)
        {
            GivenGravityWorksOnFeatures(2);

            await WhenGravityIsRefreshedAndAskedFor(metric);
            ThenTheAnswerCoversTheFeaturesGravityWorksOn(metric, 2);

            GivenGravityHasTakenOnMoreFeatures(18);
            await WhenGravityIsRefreshedAndAskedFor(metric);
            ThenTheAnswerCoversTheFeaturesGravityWorksOn(metric, 20);

            ThenTheLastAnswerCostAsManyCommandsAsTheFirst();
        }

        // The Portfolio's numbers do not change. Green before and after this story.
        [Test]
        [Category("us-01")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task The_Portfolio_still_sizes_a_Feature_by_every_Team_s_Work_Items()
        {
            GivenTheOceanExplorerPortfolioAndTheTeamsThatWorkOnIt();

            await WhenOceanExplorersFeatureSizesAreAskedFor();

            ThenTheFeatureReads(CoralReefRestoration, 19);
        }
    }
}
