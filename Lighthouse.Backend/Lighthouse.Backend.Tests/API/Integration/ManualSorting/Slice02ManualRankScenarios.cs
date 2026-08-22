using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.ManualSorting
{
    /// <summary>
    /// DISTILL acceptance scenarios (Epic 5375) — Slice 02: the instance takes ownership of the order.
    /// Walking skeleton: a config admin hands ordering over and not one row moves. Driving ports: the
    /// ordering-policy read and write ports, the Features read port, the Portfolio detail read port and
    /// the work-item refresh port. US-02 (AC-2.1 … AC-2.7) and US-05 (AC-5.1 … AC-5.5).
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5375-manual-sorting")]
    [Category("slice-02")]
    public partial class Slice02ManualRankTest
    {
        /// <summary>
        /// Every shape a connector writes into <c>Order</c> (S1, and the premise check's three-at-once
        /// finding): Azure DevOps stack ranks, Jira LexoRanks, Linear's inverted doubles, ServiceNow
        /// record numbers, and a CSV that carried no order column at all.
        /// </summary>
        private static readonly object[] EveryConnectorOrderShape =
        [
            new object[] { "stack ranks", new[] { "1999677094", "1999702849", "1999725208", "1999731004" } },
            new object[] { "LexoRanks", new[] { "0|hzzzzz:", "0|i0000n:", "0|i0001a:", "0|i000zz:" } },
            new object[] { "inverted doubles", new[] { "-952.83", "-11.5", "0.5", "1234.75" } },
            new object[] { "record numbers", new[] { "INC0010023", "TASK0007311", "CHG0030001", "PRB0040009" } },
            new object[] { "no order at all", new[] { string.Empty, string.Empty, string.Empty, string.Empty } },
        ];

        // @walking_skeleton @driving_port @real-io @AC-2.1 — the promise the whole slice rests on: on the
        // instant ownership changes hands, the list a user is looking at does not move.
        [TestCaseSource(nameof(EveryConnectorOrderShape))]
        public async Task Handing_the_order_over_moves_nobody_whatever_the_tracker_wrote(string shape, string[] sourceOrders)
        {
            var platform = GivenAPortfolio($"Platform ranked by {shape}");
            GivenFeaturesTheTrackerRanked(platform, sourceOrders);
            GivenTheCallerAdministersTheInstance();

            var before = await WhenTheProductOwnerOpensTheFeaturesView();
            await WhenTheConfigAdminHandsTheOrderOver();
            var after = await WhenTheProductOwnerOpensTheFeaturesView();

            ThenTheListIsUnchanged(before, after);
        }

        // @driving_port @real-io @AC-2.1 — the multi-connector instance the premise check actually found:
        // three Order shapes at once, which is where a comparison that is not a total order shows up.
        [Test]
        public async Task Handing_the_order_over_moves_nobody_on_an_instance_wired_to_several_trackers()
        {
            var platform = GivenAPortfolio("Platform");
            GivenFeaturesTheTrackerRanked(platform, "1999677094", "0|i0000n:", "-952.83", "INC0010023", string.Empty, "1999702849", "0|hzzzzz:", "-11.5");
            GivenTheCallerAdministersTheInstance();

            var before = await WhenTheProductOwnerOpensTheFeaturesView();
            await WhenTheConfigAdminHandsTheOrderOver();
            var after = await WhenTheProductOwnerOpensTheFeaturesView();

            ThenTheListIsUnchanged(before, after);
        }

        // @driving_port @real-io @AC-2.2 — K2. Five consecutive refreshes, with the tracker rewriting its
        // own order every single time, and nobody moves.
        [Test]
        public async Task The_tracker_may_re_rank_all_it_likes_and_the_order_this_instance_shows_never_moves()
        {
            var platform = GivenAPortfolio("Platform");
            GivenAFeatureTheTrackerRanked("Rebuild the search index", "FTR-1", "10", platform);
            GivenAFeatureTheTrackerRanked("Retire the legacy importer", "FTR-2", "20", platform);
            GivenAFeatureTheTrackerRanked("Publish the partner catalogue", "FTR-3", "30", platform);
            GivenTheCallerAdministersTheInstance();
            await WhenTheConfigAdminHandsTheOrderOver();

            var afterHandover = await WhenTheProductOwnerOpensTheFeaturesView();

            for (var refresh = 1; refresh <= 5; refresh++)
            {
                await WhenTheTrackerSyncsWithItsOwnNewOrder(platform, ("FTR-1", $"{30 + refresh}"), ("FTR-2", $"{20 - refresh}"), ("FTR-3", $"{10 - refresh}"));

                var afterRefresh = await WhenTheProductOwnerOpensTheFeaturesView();
                ThenTheListIsUnchanged(afterHandover, afterRefresh);
            }

            ThenTheTrackerStillOwnsItsOwnOrderValues("FTR-1", "35");
        }

        // @driving_port @real-io @AC-2.3 — K4, and the premise check turned into an assertion. The two
        // Features tied on 5 are the shape the dev instance really carries: a subset sort that resolves a
        // tie by whatever order the store happened to hand the rows back disagrees with the whole-table
        // one, and nothing but this comparison would notice.
        [Test]
        public async Task Every_way_in_reports_the_same_order_even_where_the_tracker_ranked_two_features_alike()
        {
            var platform = GivenAPortfolio("Platform");
            GivenAFeatureTheTrackerRanked("Rebuild the search index", "FTR-1", "5", platform);
            GivenAFeatureTheTrackerRanked("Retire the legacy importer", "FTR-2", "5", platform);
            GivenAFeatureTheTrackerRanked("Publish the partner catalogue", "FTR-3", "1", platform);
            GivenAFeatureTheTrackerNeverRanked("Arrived without a rank", "FTR-4", platform);
            GivenTheCallerAdministersTheInstance();
            await WhenTheConfigAdminHandsTheOrderOver();

            var throughTheFeaturesView = await WhenTheProductOwnerOpensTheFeaturesView();
            var throughThePortfolio = await WhenTheProductOwnerOpensThePortfolio(platform);

            ThenBothWaysInAgreeOnTheOrder(throughTheFeaturesView, throughThePortfolio);
        }

        // @driving_port @real-io @AC-2.4 @AC-5.1 — giving it back takes effect at once, no refresh needed
        [Test]
        public async Task Giving_the_order_back_restores_the_trackers_own_sequence_straight_away()
        {
            var platform = GivenAPortfolio("Platform");
            GivenAFeatureTheTrackerRanked("Rebuild the search index", "FTR-1", "10", platform);
            GivenAFeatureTheTrackerRanked("Retire the legacy importer", "FTR-2", "20", platform);
            GivenAFeatureTheTrackerRanked("Publish the partner catalogue", "FTR-3", "30", platform);
            GivenTheCallerAdministersTheInstance();

            await WhenTheConfigAdminHandsTheOrderOver();
            await WhenTheTrackerSyncsWithItsOwnNewOrder(platform, ("FTR-1", "30"), ("FTR-2", "20"), ("FTR-3", "10"));
            await WhenTheConfigAdminGivesTheOrderBack();

            var afterGivingItBack = await WhenTheProductOwnerOpensTheFeaturesView();

            string[] theTrackersNewSequence = ["Publish the partner catalogue", "Retire the legacy importer", "Rebuild the search index"];
            ThenTheListReads(afterGivingItBack, theTrackersNewSequence);
        }

        // @AC-5.2 — the places this instance chose are kept while the tracker has the order back, so the
        // switch is an experiment rather than a door. Judged against the store: while the policy is off
        // there is, by construction, no read port that would show them.
        [Test]
        public async Task The_places_this_instance_chose_survive_giving_the_order_back()
        {
            var platform = GivenAPortfolio("Platform");
            GivenAFeatureTheTrackerRanked("Rebuild the search index", "FTR-1", "10", platform);
            GivenAFeatureTheTrackerRanked("Retire the legacy importer", "FTR-2", "20", platform);
            GivenTheCallerAdministersTheInstance();

            await WhenTheConfigAdminHandsTheOrderOver();
            var chosenWhileOwned = GivenTheOrderingColumnsAsStored();
            await WhenTheConfigAdminGivesTheOrderBack();

            ThenTheStoredPlacesAreUnchangedFrom(chosenWhileOwned);
        }

        // @driving_port @real-io @AC-5.3 — the one that proves re-enabling restores rather than re-seeds.
        // While the order is given back, the tracker reshuffles everything; taking it over again must
        // return the instance's own sequence, not a fresh reading of the tracker's new one.
        [Test]
        public async Task Taking_the_order_over_again_restores_what_this_instance_chose_not_what_the_tracker_since_decided()
        {
            var platform = GivenAPortfolio("Platform");
            GivenAFeatureTheTrackerRanked("Rebuild the search index", "FTR-1", "10", platform);
            GivenAFeatureTheTrackerRanked("Retire the legacy importer", "FTR-2", "20", platform);
            GivenAFeatureTheTrackerRanked("Publish the partner catalogue", "FTR-3", "30", platform);
            GivenTheCallerAdministersTheInstance();

            await WhenTheConfigAdminHandsTheOrderOver();
            var whatThisInstanceChose = await WhenTheProductOwnerOpensTheFeaturesView();

            await WhenTheConfigAdminGivesTheOrderBack();
            await WhenTheTrackerSyncsWithItsOwnNewOrder(platform, ("FTR-1", "30"), ("FTR-2", "20"), ("FTR-3", "10"));
            await WhenTheConfigAdminHandsTheOrderOver();

            var afterTakingItOverAgain = await WhenTheProductOwnerOpensTheFeaturesView();

            ThenTheListIsUnchanged(whatThisInstanceChose, afterTakingItOverAgain);
        }

        // @driving_port @real-io @AC-5.3 — the second half of the AC, and the only path that exercises
        // appending after an existing place: a Feature arrives while the tracker has the order back, so
        // taking it over again must place the newcomer at the END rather than re-reading the tracker.
        [Test]
        public async Task A_feature_that_arrived_while_the_tracker_had_the_order_back_is_placed_last_when_it_is_taken_over_again()
        {
            var platform = GivenAPortfolio("Platform");
            GivenAFeatureTheTrackerRanked("Rebuild the search index", "FTR-1", "10", platform);
            GivenAFeatureTheTrackerRanked("Retire the legacy importer", "FTR-2", "20", platform);
            GivenTheCallerAdministersTheInstance();

            await WhenTheConfigAdminHandsTheOrderOver();
            await WhenTheConfigAdminGivesTheOrderBack();

            // The tracker would put the newcomer first, and it must still land last.
            await WhenTheTrackerSyncsWithItsOwnNewOrder(platform, ("FTR-1", "10"), ("FTR-2", "20"), ("FTR-LATE", "1"));
            await WhenTheConfigAdminHandsTheOrderOver();

            var afterTakingItOverAgain = await WhenTheProductOwnerOpensTheFeaturesView();

            string[] theNewcomerLast = ["Rebuild the search index", "Retire the legacy importer", "FTR-LATE"];
            ThenTheListReads(afterTakingItOverAgain, theNewcomerLast);
            ThenEveryFeatureHoldsOnePlaceOfItsOwn(afterTakingItOverAgain);
        }

        // @error @driving_port @real-io @AC-2.6 @AC-5.3 — a Feature the tracker would rank first arrives
        // while this instance owns the order, and lands last without announcing itself (D7).
        [Test]
        public async Task A_feature_arriving_while_this_instance_owns_the_order_lands_last()
        {
            var platform = GivenAPortfolio("Platform");
            GivenAFeatureTheTrackerRanked("Rebuild the search index", "FTR-1", "10", platform);
            GivenAFeatureTheTrackerRanked("Retire the legacy importer", "FTR-2", "20", platform);
            GivenTheCallerAdministersTheInstance();
            await WhenTheConfigAdminHandsTheOrderOver();

            await WhenTheTrackerSyncsWithItsOwnNewOrder(platform, ("FTR-1", "10"), ("FTR-2", "20"), ("FTR-LATE", "1"));

            var afterTheNewcomerArrived = await WhenTheProductOwnerOpensTheFeaturesView();

            string[] theNewcomerLast = ["Rebuild the search index", "Retire the legacy importer", "FTR-LATE"];
            ThenTheListReads(afterTheNewcomerArrived, theNewcomerLast);
        }

        // @error @AC-2.5 — the view is free, the ownership is not (D12/S11)
        [Test]
        public async Task An_instance_without_a_premium_licence_may_not_hand_the_order_over()
        {
            var platform = GivenAPortfolio("Platform");
            GivenAFeatureTheTrackerRanked("Rebuild the search index", "FTR-1", "10", platform);
            GivenTheInstanceHasNoPremiumLicence();
            GivenTheCallerAdministersTheInstance();

            var refused = await WhenTheConfigAdminTriesToHandTheOrderOver();
            var theViewItself = await WhenTheProductOwnerOpensTheFeaturesView();

            ThenTheInstanceRefusesForWantOfALicence(refused);
            ThenTheViewIsStillReachable(theViewItself);
        }

        // @error @AC-2.7 — running a Portfolio does not make the instance's ordering yours to change.
        // Reading who owns it is a different question: every feature list asks it to name its position
        // column, so a refusal there would leave everyone but an instance administrator reading the
        // wrong heading over the right order.
        [Test]
        public async Task Someone_who_may_only_run_a_portfolio_may_read_who_owns_the_order_but_not_change_it()
        {
            var platform = GivenAPortfolio("Platform");
            GivenAFeatureTheTrackerRanked("Rebuild the search index", "FTR-1", "10", platform);
            GivenTheCallerMayWriteOnly(platform);

            var refusedWrite = await WhenTheConfigAdminTriesToHandTheOrderOver();
            var read = await WhenAnyoneAsksWhoOwnsTheOrder();

            ThenTheInstanceRefusesTheCaller(refusedWrite);
            ThenTheTrackerOwnsTheOrder(read);
        }

        // @error @AC-5.1 — the downgrade path and the fresh-install path are the same path: nothing has
        // been chosen, so the tracker owns the order and nobody had to write a row to say so.
        [Test]
        public async Task Before_anyone_chooses_the_instance_follows_the_tracker()
        {
            var platform = GivenAPortfolio("Platform");
            GivenAFeatureTheTrackerRanked("Rebuild the search index", "FTR-1", "20", platform);
            GivenAFeatureTheTrackerRanked("Retire the legacy importer", "FTR-2", "10", platform);
            GivenTheCallerAdministersTheInstance();

            var whoOwnsIt = await WhenAnyoneAsksWhoOwnsTheOrder();
            var theList = await WhenTheProductOwnerOpensTheFeaturesView();

            ThenTheTrackerOwnsTheOrder(whoOwnsIt);
            string[] theTrackersOwnSequence = ["Retire the legacy importer", "Rebuild the search index"];
            ThenTheListReads(theList, theTrackersOwnSequence);
        }

        // @error @driving_port @real-io — INV-O2. Gaps, repeats and Features never given a place are all
        // legal, and the instance must still report one place per Feature. DESIGN's instruction to DISTILL
        // was explicit: do not assume the places are contiguous.
        [Test]
        public async Task A_ragged_set_of_places_is_still_one_unambiguous_order()
        {
            var platform = GivenAPortfolio("Platform");
            GivenAFeatureAlreadyPlacedAt("Rebuild the search index", 3, "10", platform);
            GivenAFeatureAlreadyPlacedAt("Retire the legacy importer", 3, "20", platform);
            GivenAFeatureAlreadyPlacedAt("Publish the partner catalogue", 900, "30", platform);
            GivenAFeatureTheTrackerRanked("Never given a place", "FTR-4", "40", platform);
            GivenTheCallerAdministersTheInstance();
            await WhenTheConfigAdminHandsTheOrderOver();

            var theList = await WhenTheProductOwnerOpensTheFeaturesView();
            var theSameListAgain = await WhenTheProductOwnerOpensTheFeaturesView();

            ThenEveryFeatureHoldsOnePlaceOfItsOwn(theList);
            ThenTheListIsUnchanged(theList, theSameListAgain);
        }

        // @error @driving_port @real-io @AC-2.1 — regression, found by hand on a restored database and by
        // nothing in this suite. Writing places over the loaded Feature graph writes back everything else
        // that graph dragged in, and a Portfolio whose Features carry no Teams drags in far less of it.
        // Every fixture here used one, so none of them could express the failure.
        [Test]
        public async Task Handing_the_order_over_works_on_a_portfolio_that_has_teams()
        {
            var platform = GivenAPortfolio("Platform");
            var team = GivenTheresATeam();
            var searchIndex = GivenAFeatureTheTrackerRanked("Rebuild the search index", "FTR-1", "10", platform);
            var legacyImporter = GivenAFeatureTheTrackerRanked("Retire the legacy importer", "FTR-2", "20", platform);
            GivenTheTeamHasWorkLeftOn(searchIndex, team);
            GivenTheTeamHasWorkLeftOn(legacyImporter, team);
            GivenTheCallerAdministersTheInstance();

            var before = await WhenTheProductOwnerOpensTheFeaturesView();
            await WhenTheConfigAdminHandsTheOrderOver();
            var after = await WhenTheProductOwnerOpensTheFeaturesView();

            ThenTheListIsUnchanged(before, after);
        }

        // @AC-2.1 — the bounded-change complement. Taking the order over writes places and nothing else:
        // the tracker's own value, the state and the Portfolio membership all come through untouched (D5).
        [Test]
        public async Task Taking_the_order_over_disturbs_nothing_but_the_places()
        {
            var platform = GivenAPortfolio("Platform");
            GivenAFeatureTheTrackerRanked("Rebuild the search index", "FTR-1", "10", platform);
            GivenAFeatureTheTrackerRanked("Retire the legacy importer", "FTR-2", "0|i0000n:", platform);
            GivenAFinishedFeature("Ship the pricing page", "FTR-3", "-11.5", platform);
            GivenTheCallerAdministersTheInstance();

            var beforeHandover = GivenTheOrderingColumnsAsStored();
            await WhenTheConfigAdminHandsTheOrderOver();

            ThenTheTrackersOwnValuesAreUnchangedFrom(beforeHandover);
        }
    }
}
