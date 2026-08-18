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

        // The links a Feature already on file picks up, and the ones it loses again. A Feature that
        // arrives with its links already drawn is the scenario above; this is the other half of the
        // refresh, where a row that exists has to follow the tracker rather than keep what it had.
        [Test]
        public async Task What_a_feature_waits_on_follows_the_tracker_across_refreshes()
        {
            var platform = GivenAPortfolio("Platform");
            var withNoLinksDrawnOnIt = AFeatureTheTrackerHolds("F-3", "Publish the partner catalogue");
            await WhenARefreshRunsAlongsideTheFeaturesItCanWaitOn(platform, withNoLinksDrawnOnIt);

            await WhenARefreshRunsAlongsideTheFeaturesItCanWaitOn(
                platform, AFeatureWaitingOn("F-3", "Publish the partner catalogue", TheTwoItWaitsOn));

            ThenTheFeatureWaitsOnExactly("F-3", TheTwoItWaitsOn);
            ThenEveryStoredReferenceNamesTheFeatureThatWaits();

            await WhenARefreshRunsAlongsideTheFeaturesItCanWaitOn(platform, withNoLinksDrawnOnIt);

            ThenTheFeatureWaitsOnNothing("F-3");
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
