using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// A Team admin chooses the likelihoods the range is read at. Until somebody does, the low end is the
    /// median (50%) and the high end the 85% reading. Both are whole percentages from 50 to 95 and the low one
    /// stays below the high one; a refused save names both values and changes nothing. A save that names only
    /// one end is judged against the end already stored. Like every refinement setting the band never makes
    /// the Team fetch its Work Items afresh, and a save that says nothing about it keeps it.
    ///
    /// Driving ports: the Team settings write and read, and the Refinement tab's read. Step definitions live
    /// in Slice07BandPercentilesSpecifications.cs.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5510-5881-refinement")]
    [Category("slice-07")]
    public partial class Slice07BandPercentilesTest : RefinementNeedAcceptanceTest
    {
        // --- The setting ---

        // @driving_port @real-io @us-07 @slice-07 @contract-shape:pure-function
        [Test]
        public async Task A_Team_that_never_chose_a_band_reads_its_range_at_50_and_85()
        {
            var gravity = await GivenGravityRefinesWithoutABand();

            ThenTheBandIs(await ReadTheTeamSettings(gravity), 50, 85);
        }

        // @driving_port @real-io @us-07 @slice-07 @contract-shape:bounded-change
        [Test]
        public async Task A_Team_admin_sets_the_band_and_it_reads_back()
        {
            var gravity = await GivenGravityRefinesWithoutABand();

            await TheAdminHasSetTheBand(gravity, 60, 95);

            ThenTheBandIs(await ReadTheTeamSettings(gravity), 60, 95);
        }

        // --- What the band changes ---

        // @driving_port @real-io @us-07 @us-05 @slice-05 @slice-07 @contract-shape:pure-function
        // The forecast reads 5 at 50%, 6 at 70%, 8 at 85% and 10 at 95%.
        [TestCase(50, 95, 5, 10)]
        [TestCase(50, 85, 5, 8)]
        [TestCase(70, 95, 6, 10)]
        public async Task The_band_decides_where_the_range_is_read(int lowPercentile, int highPercentile, int low, int high)
        {
            var gravity = await GivenGravityHasTwoReadyAndIsLikelyToPullThreeToTenBeforeThursday();

            await TheAdminHasSetTheBand(gravity, lowPercentile, highPercentile);

            ThenTheNeedIs(await WhenTheCoachOpensTheRefinementTab(gravity),
                new NeedReading(Below, null, low, high, lowPercentile, highPercentile, 6));
        }

        // --- What is refused ---

        // @driving_port @real-io @us-07 @slice-07 @error @contract-shape:unbounded-preservation
        [TestCase(90, 85)]
        [TestCase(85, 85)]
        public async Task A_low_end_not_below_the_high_end_is_refused_naming_both_and_nothing_is_saved(int lowPercentile, int highPercentile)
        {
            var gravity = await GivenGravityReadsItsRangeAt60And95();

            using var refused = await WhenTheAdminSavesTheBand(gravity, ABand(lowPercentile, highPercentile));

            await ThenTheSaveIsRefusedNamingAndTheBandIsStill60And95(refused, gravity, lowPercentile, highPercentile);
        }

        // @driving_port @real-io @us-07 @slice-07 @error @boundary @contract-shape:unbounded-preservation
        [TestCase(49, 85)]
        [TestCase(0, 85)]
        [TestCase(50, 96)]
        [TestCase(50, 100)]
        public async Task A_likelihood_outside_50_to_95_is_refused_and_nothing_is_saved(int lowPercentile, int highPercentile)
        {
            var gravity = await GivenGravityReadsItsRangeAt60And95();

            using var refused = await WhenTheAdminSavesTheBand(gravity, ABand(lowPercentile, highPercentile));

            await ThenTheSaveIsRefusedNamingAndTheBandIsStill60And95(refused, gravity, lowPercentile, highPercentile);
        }

        // @driving_port @real-io @us-07 @slice-07 @boundary @contract-shape:bounded-change
        [TestCase(50, 95)]
        [TestCase(94, 95)]
        public async Task A_band_on_the_bounds_of_50_and_95_is_accepted(int lowPercentile, int highPercentile)
        {
            var gravity = await GivenGravityRefinesWithoutABand();

            await TheAdminHasSetTheBand(gravity, lowPercentile, highPercentile);

            ThenTheBandIs(await ReadTheTeamSettings(gravity), lowPercentile, highPercentile);
        }

        // @driving_port @real-io @us-07 @slice-07 @error @contract-shape:unbounded-preservation
        // Stored 60 and 95: a high end of 55 alone would sit below the stored low end.
        [Test]
        public async Task A_save_naming_only_one_end_is_judged_against_the_other_end_already_stored()
        {
            var gravity = await GivenGravityReadsItsRangeAt60And95();

            using var refused = await WhenTheAdminSavesTheBand(gravity, ABand(null, 55));

            await ThenTheSaveIsRefusedNamingAndTheBandIsStill60And95(refused, gravity, 60, 55);
        }

        // @driving_port @real-io @us-07 @slice-07 @boundary @contract-shape:bounded-change
        [Test]
        public async Task A_save_naming_only_the_low_end_keeps_the_stored_high_end()
        {
            var gravity = await GivenGravityReadsItsRangeAt60And95();

            using var save = await WhenTheAdminSavesTheBand(gravity, ABand(70, null));

            await ThenTheSaveIsAcceptedAndTheBandIs(save, gravity, 70, 95);
        }

        // @driving_port @real-io @us-07 @slice-07 @boundary @contract-shape:unbounded-preservation
        // An older settings form knows nothing about the band; its saves must not remove it.
        [TestCase(SaveShape.WithoutTheRefinementSection)]
        [TestCase(SaveShape.RefinementSectionWithoutTheMember)]
        public async Task A_save_that_says_nothing_about_the_band_keeps_it(SaveShape shape)
        {
            var gravity = await GivenGravityReadsItsRangeAt60And95();

            await WhenTheSettingsAreSaved(gravity, shape);

            ThenTheBandIs(await ReadTheTeamSettings(gravity), 60, 95);
        }

        // @driving_port @real-io @us-07 @slice-07 @boundary @contract-shape:bounded-change
        // The band is a refinement setting; it never makes the Team fetch its Work Items afresh.
        [Test]
        public async Task Setting_the_band_keeps_every_Work_Item_the_Team_holds()
        {
            var gravity = await GivenGravityRefinesWithoutABand();
            var before = WorkItemsStoredFor(gravity);

            await TheAdminHasSetTheBand(gravity, 60, 95);

            await ThenTheTeamStillHoldsItsWorkItemsAndReadsItsRangeAt60And95(gravity, before);
        }
    }
}
