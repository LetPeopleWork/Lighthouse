using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Dependencies
{
    /// <summary>
    /// Acceptance scenarios - Slice 01: what a Feature is waiting on, read off the links somebody already
    /// drew in the work tracking system. Walking skeleton: a refresh runs and the Feature row afterwards
    /// knows what it waits on. Driving port: the work item refresh.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-4365-dependencies")]
    [Category("slice-01")]
    public partial class Slice01DependencyCountTest
    {
        private static readonly string[] TheTwoItWaitsOn = ["F-1", "F-2"];
        private static readonly string[] OneHeldAndOneNobodyHolds = ["F-1", "F-404"];
        private static readonly string[] OnlyTheOneHeld = ["F-1"];
        private static readonly string[] OnlyTheSecondOfThem = ["F-2"];

        // @walking_skeleton @driving_port
        [Test]
        public async Task Predecessor_links_recorded_in_the_tracker_become_a_count_on_the_feature_row()
        {
            var platform = GivenAPortfolio("Platform");

            await WhenARefreshRunsAlongsideTheFeaturesItCanWaitOn(
                platform, AFeatureWaitingOn("F-3", "Publish the partner catalogue", TheTwoItWaitsOn));

            ThenTheFeatureWaitsOnExactly("F-3", TheTwoItWaitsOn);
            ThenEveryStoredReferenceNamesTheFeatureThatWaits();
        }

        // The links a Feature already on file picks up. A Feature that arrives with its links already
        // drawn is the scenario above; this is the other branch of the refresh, where a row that already
        // exists has to take on links it was not stored with.
        [Test]
        public async Task A_feature_already_on_file_picks_up_the_links_drawn_on_it_later()
        {
            var platform = GivenAPortfolio("Platform");
            await WhenARefreshRunsAlongsideTheFeaturesItCanWaitOn(
                platform, AFeatureTheTrackerHolds("F-3", "Publish the partner catalogue"));

            await WhenARefreshRunsAlongsideTheFeaturesItCanWaitOn(
                platform, AFeatureWaitingOn("F-3", "Publish the partner catalogue", TheTwoItWaitsOn));

            ThenTheFeatureWaitsOnExactly("F-3", TheTwoItWaitsOn);
            ThenEveryStoredReferenceNamesTheFeatureThatWaits();
        }

        // Links coming off again, in the three shapes that fail differently. Refreshing on the same
        // links has to leave the stored set as it was rather than a second copy of itself; dropping one
        // has to leave the other one specifically, not merely one of them; dropping the last has to
        // leave nothing. A write that added to what was there instead of replacing it would pass the
        // middle case by luck and fail the two around it.
        [Test]
        public async Task A_link_removed_in_the_tracker_lowers_the_count_on_the_next_refresh()
        {
            var platform = GivenAPortfolio("Platform");
            var waitingOnBoth = AFeatureWaitingOn("F-3", "Publish the partner catalogue", TheTwoItWaitsOn);
            await WhenARefreshRunsAlongsideTheFeaturesItCanWaitOn(platform, waitingOnBoth);

            await WhenARefreshRunsAlongsideTheFeaturesItCanWaitOn(platform, waitingOnBoth);

            ThenTheFeatureWaitsOnExactly("F-3", TheTwoItWaitsOn);

            await WhenARefreshRunsAlongsideTheFeaturesItCanWaitOn(
                platform, AFeatureWaitingOn("F-3", "Publish the partner catalogue", OnlyTheSecondOfThem));

            ThenTheFeatureWaitsOnExactly("F-3", OnlyTheSecondOfThem);
            ThenEveryStoredReferenceNamesTheFeatureThatWaits();

            await WhenARefreshRunsAlongsideTheFeaturesItCanWaitOn(
                platform, AFeatureTheTrackerHolds("F-3", "Publish the partner catalogue"));

            ThenTheFeatureWaitsOnNothing("F-3");
        }

        // A Predecessor link names an id and never says what kind of thing it is, so a link drawn to a
        // Bug, to a Task, or to a Feature this Portfolio has not imported yet all arrive as the same
        // thing: an id matching nothing Lighthouse holds. It is kept as written rather than rejected,
        // because the day that item does show up the link starts counting on its own.
        [Test]
        public async Task A_link_pointing_at_something_lighthouse_does_not_keep_as_a_feature_is_passed_over()
        {
            var platform = GivenAPortfolio("Platform");

            await WhenARefreshRuns(
                platform,
                AFeatureTheTrackerHolds("F-1", "Rebuild the search index"),
                AFeatureWaitingOn("F-3", "Publish the partner catalogue", OneHeldAndOneNobodyHolds));

            ThenAmongTheFeaturesHeldItWaitsOnExactly("F-3", OnlyTheOneHeld);
            ThenTheFeatureWaitsOnExactly("F-3", OneHeldAndOneNobodyHolds);
            ThenTheRestOfTheRowIsThere("F-3", "Publish the partner catalogue");
            ThenNobodyComplained();
        }

        // The one writer is wired up, and it re-keys. A connector reads links off a Feature it has not
        // saved, so every reference it builds names Feature nought; left that way the deduplication key
        // is a constant and anything reading a reference in memory is told the wrong Feature.
        [Test]
        public void The_reconciler_keys_every_reference_to_the_feature_that_waits_on_it()
        {
            var feature = AStoredFeature(id: 42, "F-3");

            WhenTheHostIsAskedForTheReconciler().Reconcile(feature, [
                AReferenceTheConnectorBuiltBeforeSaving("F-1"),
                AReferenceTheConnectorBuiltBeforeSaving("F-2"),
            ]);

            ThenEveryReferenceNames(feature);
            ThenTheFeatureStillWaitsOn(feature, TheTwoItWaitsOn);
        }

        // A Feature arriving for the first time is handed to the reconciler already carrying its own
        // references, so reconciling has to read them before it clears them. Reading them afterwards
        // leaves every new Feature waiting on nothing, and nothing else in the refresh would say so.
        [Test]
        public void A_feature_reconciled_against_its_own_references_keeps_them()
        {
            var feature = AStoredFeature(id: 42, "F-3");
            var reconciler = WhenTheHostIsAskedForTheReconciler();
            reconciler.Reconcile(feature, [
                AReferenceTheConnectorBuiltBeforeSaving("F-1"),
                AReferenceTheConnectorBuiltBeforeSaving("F-2"),
            ]);

            reconciler.Reconcile(feature, feature.DependsOnReferences);

            ThenTheFeatureStillWaitsOn(feature, TheTwoItWaitsOn);
        }
    }
}
