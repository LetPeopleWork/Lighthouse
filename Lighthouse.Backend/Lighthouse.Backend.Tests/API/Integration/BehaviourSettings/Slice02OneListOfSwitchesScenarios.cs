using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.BehaviourSettings
{
    /// <summary>
    /// DISTILL acceptance scenarios (Story 5876) - Slice 02: one list of switches. Driving ports: the
    /// behaviour-settings read and toggle ports, the deprecated ordering alias, and the Features read
    /// port. US-01 (AC-01.3 ... AC-01.10); AC-01.1, AC-01.2 and AC-01.11 are rendered rather than
    /// served and are asserted in SystemSettingsTab.behaviourSettings.test.tsx.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-5876-behaviour-settings")]
    [Category("slice-02")]
    public partial class Slice02OneListOfSwitchesTest
    {
        // @driving_port @real-io @AC-01.5 - the promise the whole slice rests on, and the regression test
        // for the one decision that can be wrong in a way users see. The tracker's order is the reverse of
        // the order the rows were created in, so a seed that runs after the flip - reading an all-null set
        // of places and numbering them by row id - produces a visibly different list and fails here. A
        // fixture ranked in creation order would pass either way and prove nothing.
        [Test]
        [Ignore("RED scaffold - slice 02 not implemented. The ordering setting is not in the behaviour-settings table yet.")]
        public async Task Turning_the_ordering_setting_on_for_the_first_time_moves_nobody()
        {
            var platform = GivenAPortfolio("Platform");
            GivenFeaturesTheTrackerRankedBackwards(platform);
            GivenTheCallerAdministersTheInstance();

            var before = await WhenTheProductOwnerOpensTheFeaturesView();
            await WhenTheAdminHandsTheOrderOverInBehaviourSettings();
            var after = await WhenTheProductOwnerOpensTheFeaturesView();

            ThenTheListIsUnchanged(before, after);
        }

        // @AC-01.5 - the same promise stated against the places themselves, so a failure names the cause
        // rather than the symptom: the places follow what the tracker ranked, not what the store handed
        // back first.
        [Test]
        public async Task The_places_are_seeded_in_the_order_the_admin_was_looking_at()
        {
            var platform = GivenAPortfolio("Platform");
            GivenFeaturesTheTrackerRankedBackwards(platform);
            GivenTheCallerAdministersTheInstance();

            await WhenTheAdminHandsTheOrderOverInBehaviourSettings();

            ThenThePlacesFollowTheTrackersOrderRatherThanTheRowIds();
        }

        // @driving_port @real-io @AC-01.3 - the migration. It has exactly one chance to run, at first add,
        // and a shipped release that gets it wrong cannot be repaired by a later seed.
        [Test]
        public async Task An_instance_that_already_owned_its_order_still_owns_it_after_the_upgrade()
        {
            var platform = GivenAPortfolio("Platform");
            GivenFeaturesTheTrackerRankedBackwards(platform);
            GivenTheCallerAdministersTheInstance();
            await WhenTheAdminHandsTheOrderOverThroughTheDeprecatedAlias();

            var placesBeforeTheUpgrade = GivenTheOrderingPlacesAsTheyStandNow();
            WhenTheInstanceUpgradesFrom(OrderOwnedByThisInstance);

            ThenTheOrderingSettingReadsOn();
            ThenThisInstanceStillOwnsTheOrder();
            ThenTheStoredPlacesAreUnchangedFrom(placesBeforeTheUpgrade);
        }

        // @driving_port @real-io @AC-01.4 - the other half of the migration: the instance that chose the
        // tracker, the instance that never had a row at all, and the instance whose row holds something
        // that is neither. Without all three, a migration that always seeds on would pass the case above,
        // and the seeder gets exactly one chance at each of them.
        [Test]
        [TestCase(OrderOwnedByTheTracker)]
        [TestCase(null)]
        [TestCase("Nonsense")]
        public async Task An_instance_that_never_took_its_order_over_does_not_acquire_it_in_the_upgrade(string? storedPolicyBeforeTheUpgrade)
        {
            GivenTheCallerAdministersTheInstance();

            WhenTheInstanceUpgradesFrom(storedPolicyBeforeTheUpgrade);

            ThenTheOrderingSettingReadsOff();
            ThenTheTrackerStillOwnsTheOrder();
        }

        // @driving_port @real-io @AC-01.6 - both transitions still re-queue the forecasts. Asserted on the
        // way back as well as the way out: a handler wired only to the enable path would leave an instance
        // that gave the order back with dates computed from an order it no longer uses.
        [Test]
        public async Task Handing_the_order_over_and_giving_it_back_both_re_queue_the_forecasts()
        {
            var platform = GivenAPortfolio("Platform");
            GivenFeaturesTheTrackerRankedBackwards(platform);
            GivenTheCallerAdministersTheInstance();

            await WhenTheAdminHandsTheOrderOverInBehaviourSettings();
            ThenTheForecastsWereReQueuedFor(platform, times: 1);

            await WhenTheAdminGivesTheOrderBackInBehaviourSettings();
            ThenTheForecastsWereReQueuedFor(platform, times: 2);
        }

        // @driving_port @real-io @AC-01.7 - the revert guarantee the move may not lose: giving the order
        // back and taking it over again returns the places this instance chose, not a fresh reading.
        [Test]
        public async Task Taking_the_order_over_again_restores_the_places_this_instance_already_chose()
        {
            var platform = GivenAPortfolio("Platform");
            GivenFeaturesTheTrackerRankedBackwards(platform);
            GivenThisInstanceAlreadyMovedAFeatureToTheTop(platform);
            GivenTheCallerAdministersTheInstance();

            await WhenTheAdminHandsTheOrderOverInBehaviourSettings();
            var placesWhileOwned = GivenTheOrderingPlacesAsTheyStandNow();

            await WhenTheAdminGivesTheOrderBackInBehaviourSettings();
            await WhenTheAdminHandsTheOrderOverInBehaviourSettings();

            ThenTheStoredPlacesAreUnchangedFrom(placesWhileOwned);
        }

        // @driving_port @real-io @AC-01.8 - one store behind two doors. The deprecated alias writes, the
        // new read port shows it: a second write path would let the two answers drift apart on exactly the
        // instances that use the old one.
        [Test]
        [Ignore("RED scaffold - slice 02 not implemented.")]
        public async Task A_write_through_the_deprecated_door_is_visible_through_the_new_one()
        {
            var platform = GivenAPortfolio("Platform");
            GivenFeaturesTheTrackerRankedBackwards(platform);
            GivenTheCallerAdministersTheInstance();

            await WhenTheAdminHandsTheOrderOverThroughTheDeprecatedAlias();

            ThenTheOrderingSettingReadsOn();
            ThenThisInstanceStillOwnsTheOrder();
        }

        // @driving_port @real-io @AC-01.8 - and it carries the same consequences in the same order, so the
        // old door cannot become the one that scrambles the list.
        [Test]
        public async Task A_write_through_the_deprecated_door_moves_nobody_either()
        {
            var platform = GivenAPortfolio("Platform");
            GivenFeaturesTheTrackerRankedBackwards(platform);
            GivenTheCallerAdministersTheInstance();

            var before = await WhenTheProductOwnerOpensTheFeaturesView();
            await WhenTheAdminHandsTheOrderOverThroughTheDeprecatedAlias();
            var after = await WhenTheProductOwnerOpensTheFeaturesView();

            ThenTheListIsUnchanged(before, after);
            ThenTheForecastsWereReQueuedFor(platform, times: 1);
        }

        // @AC-01.9 - Faster Updates is carried across untouched. Its name, its help
        // text, its preview badge, its licence status and whether it is on are all somebody's decision and
        // none of them are this story's.
        [Test]
        public async Task The_setting_that_was_already_in_the_list_is_carried_across_untouched()
        {
            var asShipped = GivenTheShippedNonPremiumSettingAsItReadsNow();

            WhenTheInstanceUpgradesFrom(OrderOwnedByTheTracker);

            ThenTheShippedNonPremiumSettingStillReads(asShipped);
        }

        // @driving_port @real-io @AC-01.1 - two rows in one table have to be switchable one at a time.
        // Today the store keys a setting by its key and nothing generates the number the toggle route
        // addresses it by, so both shipped rows carry zero and the route cannot tell them apart. Nobody
        // has hit it because there has only ever been one row; this slice adds the second. See
        // distill/upstream-issues.md - UI-1.
        [Test]
        public async Task Each_setting_in_the_list_is_switched_on_its_own()
        {
            var platform = GivenAPortfolio("Platform");
            GivenFeaturesTheTrackerRankedBackwards(platform);
            GivenTheCallerAdministersTheInstance();
            var fasterUpdatesAsItWas = GivenTheShippedNonPremiumSettingAsItReadsNow();

            await WhenTheAdminHandsTheOrderOverInBehaviourSettings();

            ThenTheOrderingSettingReadsOn();
            ThenTheShippedNonPremiumSettingStillReads(fasterUpdatesAsItWas);
        }

        // @driving_port @real-io @AC-01.7 - the seed runs on the way out, not on the way back. A Feature
        // that arrived while this instance owned the order has no place yet; giving the order back must
        // leave it that way, because a null place is what "arrived while the switch was off" means and it
        // is what makes re-enabling append rather than renumber. The shipped write path seeds only when
        // the value becomes manual; an applier that seeds unconditionally loses that and nothing else
        // would notice.
        [Test]
        public async Task Giving_the_order_back_writes_no_places()
        {
            var platform = GivenAPortfolio("Platform");
            GivenFeaturesTheTrackerRankedBackwards(platform);
            GivenTheCallerAdministersTheInstance();
            await WhenTheAdminHandsTheOrderOverInBehaviourSettings();

            GivenAFeatureArrivesLater("Arrived after the order changed hands", "FTR-5", "5", platform);
            await WhenTheAdminGivesTheOrderBackInBehaviourSettings();

            ThenTheFeatureThatArrivedLaterStillHasNoPlace("FTR-5");
        }

        // @driving_port @real-io @AC-01.3 - an instance whose licence lapsed while it owned the order keeps
        // owning it. True today: nothing in the ordering read path consults the licence. It is asserted
        // here because the move re-backs that path onto a row marked premium, and the obvious tidy-up -
        // having the provider check the licence - would silently hand every lapsed customer's list back to
        // their tracker and reorder every Feature they had placed, on their renewal date.
        [Test]
        public async Task An_instance_whose_licence_lapsed_keeps_the_order_it_already_owns()
        {
            var platform = GivenAPortfolio("Platform");
            GivenFeaturesTheTrackerRankedBackwards(platform);
            GivenTheCallerAdministersTheInstance();
            await WhenTheAdminHandsTheOrderOverThroughTheDeprecatedAlias();

            var whileLicensed = await WhenTheProductOwnerOpensTheFeaturesView();
            GivenTheLicenceLapses();
            var afterTheLicenceLapsed = await WhenTheProductOwnerOpensTheFeaturesView();

            ThenTheListIsUnchanged(whileLicensed, afterTheLicenceLapsed);
            ThenThisInstanceStillOwnsTheOrder();
        }

        // @driving_port @real-io @AC-01.1 - the read port hands back the setting a caller names, not
        // merely some setting. Every unit test around this endpoint mocks the lookup, so the comparison
        // that picks the row has never once been executed; the toggle now runs through the same shape, and
        // an inverted comparison there would switch whichever row the store happened to return first.
        [Test]
        public async Task The_setting_a_caller_names_is_the_setting_it_gets_back()
        {
            GivenTheCallerAdministersTheInstance();

            var named = await WhenAnyoneReadsTheSettingCalled(ShippedNonPremiumKey);
            var unnamed = await WhenAnyoneReadsTheSettingCalled(KeyNobodySeeded);

            ThenTheSettingReadBackIsTheOneThatWasNamed(named, ShippedNonPremiumKey);
            ThenNoSettingWasFound(unnamed);
        }

        // @AC-01.10 - the old row is left where it is. Additive only: an upgrade that deletes it has no way
        // back if the migration turns out to have been wrong.
        [Test]
        public async Task The_upgrade_leaves_the_setting_it_migrated_from_in_place()
        {
            GivenTheCallerAdministersTheInstance();

            WhenTheInstanceUpgradesFrom(OrderOwnedByThisInstance);

            ThenTheSettingItMigratedFromIsStillStored(OrderOwnedByThisInstance);
        }
    }
}
