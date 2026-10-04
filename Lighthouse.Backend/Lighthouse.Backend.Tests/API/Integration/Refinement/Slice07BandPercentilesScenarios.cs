using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// A Team admin chooses the likelihoods the range is read at. Until somebody does, the low end is the
    /// median (50%) and the high end the 85% reading. Both are whole percentages from 1 to 99 and the low one
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
        [Ignore(PendingSlice07)]
        public async Task A_Team_that_never_chose_a_band_reads_its_range_at_50_and_85()
        {
            var gravity = await GivenGravityRefinesWithoutABand();

            ThenTheBandIs(await ReadTheTeamSettings(gravity), 50, 85);
        }

        // @driving_port @real-io @us-07 @slice-07 @contract-shape:bounded-change
        [Test]
        [Ignore(PendingSlice07)]
        public async Task A_Team_admin_sets_the_band_and_it_reads_back()
        {
            var gravity = await GivenGravityRefinesWithoutABand();

            await TheAdminHasSetTheBand(gravity, 30, 95);

            ThenTheBandIs(await ReadTheTeamSettings(gravity), 30, 95);
        }

        // --- What the band changes ---

        // @driving_port @real-io @us-07 @us-05 @slice-05 @slice-07 @contract-shape:pure-function
        // The forecast reads 3 at 30%, 5 at 50%, 6 at 70%, 8 at 85% and 10 at 95%.
        [TestCase(50, 95, 5, 10)]
        [TestCase(30, 85, 3, 8)]
        [TestCase(70, 95, 6, 10)]
        [Ignore(PendingSlice07)]
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
        [Ignore(PendingSlice07)]
        public async Task A_low_end_not_below_the_high_end_is_refused_naming_both_and_nothing_is_saved(int lowPercentile, int highPercentile)
        {
            var gravity = await GivenGravityReadsItsRangeAt30And95();

            using var refused = await WhenTheAdminSavesTheBand(gravity, ABand(lowPercentile, highPercentile));

            await ThenTheSaveIsRefusedNamingAndTheBandIsStill30And95(refused, gravity, lowPercentile, highPercentile);
        }

        // @driving_port @real-io @us-07 @slice-07 @error @boundary @contract-shape:unbounded-preservation
        [TestCase(0, 85)]
        [TestCase(-5, 85)]
        [TestCase(50, 100)]
        [Ignore(PendingSlice07)]
        public async Task A_likelihood_outside_1_to_99_is_refused_and_nothing_is_saved(int lowPercentile, int highPercentile)
        {
            var gravity = await GivenGravityReadsItsRangeAt30And95();

            using var refused = await WhenTheAdminSavesTheBand(gravity, ABand(lowPercentile, highPercentile));

            await ThenTheSaveIsRefusedNamingAndTheBandIsStill30And95(refused, gravity, lowPercentile, highPercentile);
        }

        // @driving_port @real-io @us-07 @slice-07 @boundary @contract-shape:bounded-change
        [Test]
        [Ignore(PendingSlice07)]
        public async Task The_widest_band_of_1_and_99_is_accepted()
        {
            var gravity = await GivenGravityRefinesWithoutABand();

            await TheAdminHasSetTheBand(gravity, 1, 99);

            ThenTheBandIs(await ReadTheTeamSettings(gravity), 1, 99);
        }

        // @driving_port @real-io @us-07 @slice-07 @error @contract-shape:unbounded-preservation
        // Stored 30 and 95: a high end of 20 alone would sit below the stored low end.
        [Test]
        [Ignore(PendingSlice07)]
        public async Task A_save_naming_only_one_end_is_judged_against_the_other_end_already_stored()
        {
            var gravity = await GivenGravityReadsItsRangeAt30And95();

            using var refused = await WhenTheAdminSavesTheBand(gravity, ABand(null, 20));

            await ThenTheSaveIsRefusedNamingAndTheBandIsStill30And95(refused, gravity, 30, 20);
        }

        // @driving_port @real-io @us-07 @slice-07 @boundary @contract-shape:bounded-change
        [Test]
        [Ignore(PendingSlice07)]
        public async Task A_save_naming_only_the_low_end_keeps_the_stored_high_end()
        {
            var gravity = await GivenGravityReadsItsRangeAt30And95();

            using var save = await WhenTheAdminSavesTheBand(gravity, ABand(60, null));

            await ThenTheSaveIsAcceptedAndTheBandIs(save, gravity, 60, 95);
        }

        // @driving_port @real-io @us-07 @slice-07 @boundary @contract-shape:unbounded-preservation
        // An older settings form knows nothing about the band; its saves must not remove it.
        [TestCase(SaveShape.WithoutTheRefinementSection)]
        [TestCase(SaveShape.RefinementSectionWithoutTheMember)]
        [Ignore(PendingSlice07)]
        public async Task A_save_that_says_nothing_about_the_band_keeps_it(SaveShape shape)
        {
            var gravity = await GivenGravityReadsItsRangeAt30And95();

            await WhenTheSettingsAreSaved(gravity, shape);

            ThenTheBandIs(await ReadTheTeamSettings(gravity), 30, 95);
        }

        // @driving_port @real-io @us-07 @slice-07 @boundary @contract-shape:bounded-change
        // The band is a refinement setting; it never makes the Team fetch its Work Items afresh.
        [Test]
        [Ignore(PendingSlice07)]
        public async Task Setting_the_band_keeps_every_Work_Item_the_Team_holds()
        {
            var gravity = await GivenGravityRefinesWithoutABand();
            var before = WorkItemsStoredFor(gravity);

            await TheAdminHasSetTheBand(gravity, 30, 95);

            await ThenTheTeamStillHoldsItsWorkItemsAndReadsItsRangeAt30And95(gravity, before);
        }
    }
}
