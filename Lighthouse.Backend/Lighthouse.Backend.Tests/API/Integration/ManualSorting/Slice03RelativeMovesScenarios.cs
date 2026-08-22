using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.ManualSorting
{
    /// <summary>
    /// DISTILL acceptance scenarios (Epic 5375) — Slice 03: moving a Feature up the order. Walking
    /// skeleton: a product owner places a Feature where another one stands and the whole instance reads
    /// the new sequence. Driving ports: the move port, the Features read port and the work-item refresh
    /// port. US-03 (AC-3.1 … AC-3.10; AC-3.11 is the keyboard/screen-reader promise and lives in the
    /// component tests).
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5375-manual-sorting")]
    [Category("slice-03")]
    public partial class Slice03RelativeMovesTest
    {
        private const string SearchIndex = "Rebuild the search index";
        private const string LegacyImporter = "Retire the legacy importer";
        private const string PartnerCatalogue = "Publish the partner catalogue";
        private const string BillingGateway = "Move billing to the new gateway";
        private const string MobileApprovals = "Ship the mobile approvals flow";
        private const string ReportingWarehouse = "Archive the reporting warehouse";

        // @walking_skeleton @driving_port @real-io @AC-3.1 @AC-3.4 — insert-at-target (D4) in its plainest
        // form: the moved Feature takes the place the target held, the block between the two shifts by
        // one, and no other pair changes places.
        [Test]
        public async Task Placing_a_feature_where_another_one_stands_shifts_only_the_block_between_them()
        {
            var platform = GivenAPortfolio("Platform");
            var ids = GivenTheTrackersOrderReads(platform, SearchIndex, LegacyImporter, PartnerCatalogue, BillingGateway, MobileApprovals, ReportingWarehouse);
            await GivenThisInstanceOwnsTheOrder();

            var before = await WhenTheProductOwnerOpensTheFeaturesView();
            await WhenTheProductOwnerPlacesItAbove(ids[4], ids[1]);
            var after = await WhenTheProductOwnerOpensTheFeaturesView();

            ThenTheOrderReads(after, SearchIndex, MobileApprovals, LegacyImporter, PartnerCatalogue, BillingGateway, ReportingWarehouse);
            ThenNobodyButTheMovedFeatureChangedPlacesWithAnybody(before, after, MobileApprovals);
            ThenEveryFeatureHoldsOnePlaceOfItsOwn(after);
        }

        // @driving_port @real-io @AC-3.2 — the assertion that IS D4, and the one the slice exists to test
        // in the field. A Portfolio's Features are non-contiguous in the global order, so "Move to Top"
        // from that Portfolio's list lands the Feature above the Portfolio's own first row, NOT at global
        // rank 1 — and in doing so it crosses Features that are on nobody's screen.
        [Test]
        public async Task Move_to_top_of_my_portfolios_list_lands_above_that_lists_first_feature_not_the_instances()
        {
            var alignment = GivenAPortfolio("Launch Alignment");
            var newProduct = GivenAPortfolio("New Product Initiative");

            // Global 1..6; Launch Alignment owns rows 2, 4 and 5, so its own list reads F2 F4 F5.
            var ids = GivenTheTrackersOrderReads(
                row => row is 1 or 3 or 4 ? [alignment] : [newProduct],
                SearchIndex, LegacyImporter, PartnerCatalogue, BillingGateway, MobileApprovals, ReportingWarehouse);

            await GivenThisInstanceOwnsTheOrder();
            GivenTheCallerMayWrite(alignment);

            await WhenTheProductOwnerPlacesItAbove(ids[4], ids[1]);

            GivenTheCallerAdministersTheInstance();
            var wholeInstance = await WhenTheProductOwnerOpensTheFeaturesView();

            ThenTheOrderReads(wholeInstance, SearchIndex, MobileApprovals, LegacyImporter, PartnerCatalogue, BillingGateway, ReportingWarehouse);
        }

        // @driving_port @real-io @AC-3.3 — "to the end" means past everybody, including the Features that
        // arrived after the order was handed over and hold no place at all. They sort last (INV-O1), so a
        // Move to Bottom that only renumbers the placed rows would leave the moved Feature above them.
        // This is OQ-4, and the answer is forced rather than chosen: the jumped tail has to be given
        // places, or "bottom" is not the bottom.
        [Test]
        public async Task Move_to_bottom_sends_the_feature_past_everybody_including_those_that_hold_no_place_yet()
        {
            var platform = GivenAPortfolio("Platform");
            var ids = GivenTheTrackersOrderReads(platform, SearchIndex, LegacyImporter, PartnerCatalogue);
            await GivenThisInstanceOwnsTheOrder();

            // The tracker would rank the newcomer first; it arrives while this instance owns the order, so
            // it holds no place and sorts last (D7 / AC-2.6).
            await WhenTheTrackerSyncsWithItsOwnNewOrder(platform, ("FTR-1", "1"), ("FTR-2", "2"), ("FTR-3", "3"), ("FTR-LATE", "0"));

            await WhenTheProductOwnerSendsItToTheBottom(ids[0]);
            var after = await WhenTheProductOwnerOpensTheFeaturesView();

            ThenTheOrderReads(after, LegacyImporter, PartnerCatalogue, "FTR-LATE", SearchIndex);
            ThenEveryFeatureHoldsOnePlaceOfItsOwn(after);
        }

        // @driving_port @real-io @AC-3.3 — the mirror gesture. Placing a Feature *below* a target is the
        // same primitive read the other way round, and slice 04 reuses it unchanged.
        [Test]
        public async Task Placing_a_feature_below_another_one_puts_it_immediately_after_it()
        {
            var platform = GivenAPortfolio("Platform");
            var ids = GivenTheTrackersOrderReads(platform, SearchIndex, LegacyImporter, PartnerCatalogue, BillingGateway);
            await GivenThisInstanceOwnsTheOrder();

            await WhenTheProductOwnerPlacesItBelow(ids[0], ids[2]);
            var after = await WhenTheProductOwnerOpensTheFeaturesView();

            ThenTheOrderReads(after, LegacyImporter, PartnerCatalogue, SearchIndex, BillingGateway);
        }

        // @driving_port @real-io @AC-3.5 — the move is this instance's own decision, so the tracker
        // re-ranking everything on the very next sync must not take it back.
        [Test]
        public async Task The_new_place_survives_the_tracker_re_ranking_everything_on_the_next_sync()
        {
            var platform = GivenAPortfolio("Platform");
            var ids = GivenTheTrackersOrderReads(platform, SearchIndex, LegacyImporter, PartnerCatalogue);
            await GivenThisInstanceOwnsTheOrder();

            await WhenTheProductOwnerPlacesItAbove(ids[2], ids[0]);
            var straightAfterTheMove = await WhenTheProductOwnerOpensTheFeaturesView();

            await WhenTheTrackerSyncsWithItsOwnNewOrder(platform, ("FTR-1", "300"), ("FTR-2", "200"), ("FTR-3", "100"));
            var afterTheSync = await WhenTheProductOwnerOpensTheFeaturesView();

            ThenTheOrderReads(straightAfterTheMove, PartnerCatalogue, SearchIndex, LegacyImporter);
            ThenTheOrderReads(afterTheSync, PartnerCatalogue, SearchIndex, LegacyImporter);
        }

        // @driving_port @real-io @AC-3.6 — the promise the whole epic is for: the order is not decoration,
        // it decides which delivery date is credible. The team closes a different number of items every
        // day on purpose — on constant throughput every Feature finishes on the same simulated day and a
        // sequencing change has nothing at all to show up in (Epic 5459's lesson).
        [Test]
        public async Task Bringing_a_feature_to_the_front_of_the_queue_brings_its_date_forward_and_pushes_the_displaced_one_back()
        {
            var platform = GivenAPortfolio("Platform");
            var team = GivenTheresATeam();
            GivenTheTeamClosedItemsUnevenly(team);

            var ids = GivenTheTrackersOrderReads(platform, SearchIndex, LegacyImporter);
            GivenTheTeamHasWorkLeftOn(ids[0], team, 12);
            GivenTheTeamHasWorkLeftOn(ids[1], team, 12);
            await GivenThisInstanceOwnsTheOrder();

            await WhenAForecastRunsFor(platform);
            var whileTheSearchIndexLed = await WhenTheProductOwnerOpensTheFeaturesView();

            await WhenTheProductOwnerPlacesItAbove(ids[1], ids[0]);
            await WhenAForecastRunsFor(platform);
            var onceTheImporterLed = await WhenTheProductOwnerOpensTheFeaturesView();

            ThenTheDateMovedEarlierFor(whileTheSearchIndexLed, onceTheImporterLed, LegacyImporter);
            ThenTheDateMovedLaterFor(whileTheSearchIndexLed, onceTheImporterLed, SearchIndex);
        }

        // @driven-port-probe @AC-3.6 — ADR-133. Whether the dates are recomputed is a promise the queue
        // keeps on its own thread, so it is asserted where it is made: a committed move asks for a fresh
        // forecast for every Portfolio the Feature belongs to. A move that leaves them stale is the one
        // failure indistinguishable from success.
        [Test]
        public async Task A_move_asks_for_a_fresh_forecast_for_every_portfolio_the_feature_belongs_to()
        {
            var alignment = GivenAPortfolio("Launch Alignment");
            var newProduct = GivenAPortfolio("New Product Initiative");
            var shared = GivenAFeatureTheTrackerRanked("Rework the shared onboarding", "FTR-SHARED", "1", alignment, newProduct);
            GivenAFeatureTheTrackerRanked(SearchIndex, "FTR-1", "2", alignment);
            await GivenThisInstanceOwnsTheOrder();
            GivenTheCallerMayWrite(alignment, newProduct);

            await WhenTheProductOwnerSendsItToTheBottom(shared);

            ThenAFreshForecastWasAskedForFor(alignment, newProduct);
        }

        // @error @driving_port @AC-3.7 — reading a Portfolio is not running it. The refusal and the row's
        // own verdict must agree, because the client renders from the verdict and the endpoint enforces.
        [Test]
        public async Task Someone_who_may_only_read_a_portfolio_may_not_move_its_features()
        {
            var platform = GivenAPortfolio("Platform");
            var ids = GivenTheTrackersOrderReads(platform, SearchIndex, LegacyImporter);
            await GivenThisInstanceOwnsTheOrder();
            GivenTheCallerMayOnlyRead(platform);

            var refused = await WhenTheProductOwnerTriesToPlaceItAbove(ids[1], ids[0]);
            var theList = await WhenTheProductOwnerOpensTheFeaturesView();

            ThenTheInstanceRefusesTheCaller(refused);
            ThenTheRowSaysItMayNotBeMoved(theList, LegacyImporter);
            ThenTheOrderReads(theList, SearchIndex, LegacyImporter);
            ThenNoFreshForecastWasAskedFor();
        }

        // @error @driving_port @AC-3.8 — D11's strictness, and the reason it is strict: one move
        // re-sequences a Feature the other Portfolio forecasts against, so writing on one of the two is
        // not enough. Write on both and the same Feature moves.
        [Test]
        public async Task A_feature_two_portfolios_share_may_be_moved_only_by_someone_who_may_write_both()
        {
            var alignment = GivenAPortfolio("Launch Alignment");
            var newProduct = GivenAPortfolio("New Product Initiative");
            var shared = GivenAFeatureTheTrackerRanked("Rework the shared onboarding", "FTR-SHARED", "2", alignment, newProduct);
            var mine = GivenAFeatureTheTrackerRanked(SearchIndex, "FTR-1", "1", alignment);
            await GivenThisInstanceOwnsTheOrder();

            GivenTheCallerRunsOnePortfolioAndOnlyWatchesAnother(runs: alignment, watches: newProduct);
            var refused = await WhenTheProductOwnerTriesToPlaceItAbove(shared, mine);
            var whileHalfOwned = await WhenTheProductOwnerOpensTheFeaturesView();

            GivenTheCallerMayWrite(alignment, newProduct);
            var accepted = await WhenTheProductOwnerTriesToPlaceItAbove(shared, mine);

            ThenTheInstanceRefusesTheCaller(refused);
            ThenTheRowSaysItMayNotBeMoved(whileHalfOwned, "Rework the shared onboarding");
            ThenTheRowSaysItMayBeMoved(whileHalfOwned, SearchIndex);
            ThenTheOrderReads(await WhenTheProductOwnerOpensTheFeaturesView(), "Rework the shared onboarding", SearchIndex);
            Assert.That(accepted.Status, Is.EqualTo(System.Net.HttpStatusCode.OK),
                "Writing on every Portfolio the Feature belongs to is exactly what the rule asks for.");
        }

        // @error @driving_port @AC-3.8 — the disabled action has to say why, or a Portfolio owner is left
        // with a dead button. The Portfolio standing in the way is named when the caller may read it.
        [Test]
        public async Task The_refusal_names_the_portfolio_standing_in_the_way_when_the_caller_may_read_it()
        {
            var alignment = GivenAPortfolio("Launch Alignment");
            var newProduct = GivenAPortfolio("New Product Initiative");
            GivenAFeatureTheTrackerRanked("Rework the shared onboarding", "FTR-SHARED", "1", alignment, newProduct);
            await GivenThisInstanceOwnsTheOrder();

            GivenTheCallerRunsOnePortfolioAndOnlyWatchesAnother(runs: alignment, watches: newProduct);
            var theList = await WhenTheProductOwnerOpensTheFeaturesView();

            ThenTheReasonNamesThePortfolio(theList, "Rework the shared onboarding", "New Product Initiative");
        }

        // @error @driving_port @AC-3.8 — SA-9 / ADR-136 §3, the disclosure half. Naming a Portfolio the
        // caller may not read would tell them it exists; the refusal must still be true and still say
        // something, without naming it.
        [Test]
        public async Task The_refusal_names_no_portfolio_the_caller_may_not_even_read()
        {
            var alignment = GivenAPortfolio("Launch Alignment");
            var secret = GivenAPortfolio("Confidential Acquisition");
            GivenAFeatureTheTrackerRanked("Rework the shared onboarding", "FTR-SHARED", "1", alignment, secret);
            await GivenThisInstanceOwnsTheOrder();

            GivenTheCallerMayWrite(alignment);
            var theList = await WhenTheProductOwnerOpensTheFeaturesView();

            ThenTheReasonNamesNoPortfolioTheCallerMayNotRead(theList, "Rework the shared onboarding", "Confidential Acquisition");
        }

        // @error @driving_port — DDD-9. "Every Portfolio may be written" is vacuously true for a Feature in
        // no Portfolio, so the literal rule would hand the instance's orphans to anybody. An orphan is
        // movable by nobody, and an instance administrator is the caller that proves it.
        [Test]
        public async Task A_feature_in_no_portfolio_may_be_moved_by_nobody_not_even_an_instance_administrator()
        {
            var platform = GivenAPortfolio("Platform");
            var placed = GivenAFeatureTheTrackerRanked(SearchIndex, "FTR-1", "1", platform);
            var orphan = GivenAFeatureTheTrackerRanked("Left behind by a deleted portfolio", "FTR-ORPHAN", "2");
            await GivenThisInstanceOwnsTheOrder();

            var refused = await WhenTheProductOwnerTriesToPlaceItAbove(orphan, placed);
            var theList = await WhenTheProductOwnerOpensTheFeaturesView();

            ThenTheInstanceRefusesTheCaller(refused);
            ThenTheRowSaysItMayNotBeMoved(theList, "Left behind by a deleted portfolio");
        }

        // @error @driving_port @AC-3.10 — the view is free and the position column is free (D12); deciding
        // the order is not.
        [Test]
        public async Task An_instance_without_a_premium_licence_may_not_move_anything()
        {
            var platform = GivenAPortfolio("Platform");
            var ids = GivenTheTrackersOrderReads(platform, SearchIndex, LegacyImporter);
            await GivenThisInstanceOwnsTheOrder();
            GivenTheInstanceHasNoPremiumLicence();

            var refused = await WhenTheProductOwnerTriesToPlaceItAbove(ids[1], ids[0]);
            var theViewItself = await WhenTheProductOwnerOpensTheFeaturesView();

            ThenTheInstanceRefusesForWantOfALicence(refused);
            ThenTheOrderReads(theViewItself, SearchIndex, LegacyImporter);
        }

        // @error @driving_port @AC-3.10 — while the tracker owns the order, a move has nothing to change:
        // the ranks it would write are the ones nobody reads. A silent 200 would leave the caller looking
        // at an unmoved list with no way to tell why.
        [Test]
        public async Task While_the_tracker_owns_the_order_a_move_is_refused_rather_than_quietly_stored()
        {
            var platform = GivenAPortfolio("Platform");
            var ids = GivenTheTrackersOrderReads(platform, SearchIndex, LegacyImporter);
            GivenTheCallerAdministersTheInstance();

            var refused = await WhenTheProductOwnerTriesToPlaceItAbove(ids[1], ids[0]);
            var theList = await WhenTheProductOwnerOpensTheFeaturesView();

            ThenTheInstanceRefusesTheCaller(refused);
            ThenTheOrderReads(theList, SearchIndex, LegacyImporter);
        }

        // @error @driving_port — DDD-7. The command carries exactly one target. Both at once is not a
        // move, and answering it with a guess would make the endpoint's contract whatever the last
        // caller happened to send.
        [Test]
        public async Task A_move_naming_both_a_target_to_go_above_and_one_to_go_below_is_refused()
        {
            var platform = GivenAPortfolio("Platform");
            var ids = GivenTheTrackersOrderReads(platform, SearchIndex, LegacyImporter, PartnerCatalogue);
            await GivenThisInstanceOwnsTheOrder();

            var refused = await MoveFeature(ids[2], $"\"beforeFeatureId\":{ids[0]},\"afterFeatureId\":{ids[1]}");
            var theList = await WhenTheProductOwnerOpensTheFeaturesView();

            ThenTheInstanceCannotMakeSenseOfTheMove(refused);
            ThenTheOrderReads(theList, SearchIndex, LegacyImporter, PartnerCatalogue);
        }

        // @error @driving_port — DDD-7, the two shapes a caller can send that are not a command at all: a
        // body that is not an object, and a target that is not a Feature's identity. Neither is guessed at.
        [Test]
        public async Task A_move_whose_body_is_not_a_command_at_all_is_refused()
        {
            var platform = GivenAPortfolio("Platform");
            var ids = GivenTheTrackersOrderReads(platform, SearchIndex, LegacyImporter);
            await GivenThisInstanceOwnsTheOrder();

            var refused = await WhenTheProductOwnerSendsSomethingThatIsNotACommand(ids[1]);
            var theList = await WhenTheProductOwnerOpensTheFeaturesView();

            ThenTheInstanceCannotMakeSenseOfTheMove(refused);
            ThenTheOrderReads(theList, SearchIndex, LegacyImporter);
        }

        [Test]
        public async Task A_move_naming_a_target_that_is_not_a_feature_id_is_refused()
        {
            var platform = GivenAPortfolio("Platform");
            var ids = GivenTheTrackersOrderReads(platform, SearchIndex, LegacyImporter);
            await GivenThisInstanceOwnsTheOrder();

            var refused = await WhenTheProductOwnerNamesATargetThatIsNotAFeatureId(ids[1]);
            var theList = await WhenTheProductOwnerOpensTheFeaturesView();

            ThenTheInstanceCannotMakeSenseOfTheMove(refused);
            ThenTheOrderReads(theList, SearchIndex, LegacyImporter);
        }

        // @error @driving_port — the Feature itself is gone. Deleted between the list being rendered and the
        // menu being used is the ordinary way this happens, and it must not renumber anybody.
        [Test]
        public async Task A_move_of_a_feature_that_does_not_exist_changes_nothing()
        {
            var platform = GivenAPortfolio("Platform");
            var ids = GivenTheTrackersOrderReads(platform, SearchIndex, LegacyImporter);
            await GivenThisInstanceOwnsTheOrder();

            var refused = await WhenTheProductOwnerTriesToPlaceItAbove(987654, ids[0]);
            var theList = await WhenTheProductOwnerOpensTheFeaturesView();

            ThenTheMoveDidNotSucceed(refused);
            ThenTheOrderReads(theList, SearchIndex, LegacyImporter);
            ThenNoFreshForecastWasAskedFor();
        }

        // @error @driving_port — a target that is not there. Whatever the instance answers, it must not
        // answer "done", and the order it shows afterwards must be the one it showed before.
        [Test]
        public async Task A_move_against_a_target_that_does_not_exist_changes_nothing()
        {
            var platform = GivenAPortfolio("Platform");
            var ids = GivenTheTrackersOrderReads(platform, SearchIndex, LegacyImporter);
            await GivenThisInstanceOwnsTheOrder();

            var refused = await WhenTheProductOwnerTriesToPlaceItAbove(ids[1], 987654);
            var theList = await WhenTheProductOwnerOpensTheFeaturesView();

            ThenTheMoveDidNotSucceed(refused);
            ThenTheOrderReads(theList, SearchIndex, LegacyImporter);
        }

        // @error @driving_port @real-io — INV-O2. DESIGN told DISTILL not to assume the places are
        // contiguous, so the fixture deliberately hands the move a set with a gap, a repeat and a Feature
        // nobody has placed. Whatever the move writes, the instance still reports one place per Feature.
        [Test]
        public async Task Moving_within_a_ragged_set_of_places_still_leaves_one_unambiguous_order()
        {
            var platform = GivenAPortfolio("Platform");
            var searchIndex = GivenAFeatureAlreadyPlacedAt(SearchIndex, 3, "10", platform);
            GivenAFeatureAlreadyPlacedAt(LegacyImporter, 3, "20", platform);
            GivenAFeatureAlreadyPlacedAt(PartnerCatalogue, 900, "30", platform);
            var neverPlaced = GivenAFeatureAlreadyPlacedAt("Never given a place", null, "40", platform);
            await GivenThisInstanceOwnsTheOrder();

            await WhenTheProductOwnerPlacesItAbove(neverPlaced, searchIndex);
            var after = await WhenTheProductOwnerOpensTheFeaturesView();
            var theSameListAgain = await WhenTheProductOwnerOpensTheFeaturesView();

            ThenEveryFeatureHoldsOnePlaceOfItsOwn(after);
            ThenTheOrderReads(theSameListAgain, "Never given a place", SearchIndex, LegacyImporter, PartnerCatalogue);
        }

        // @driven-port-probe @AC-3.1 — the bounded-change complement, and D5's promise stated as
        // something testable: a move writes places and touches nothing else. A Done Feature is in the
        // fixture because ranking is indifferent to it (D15) and it must survive the renumber untouched.
        [Test]
        public async Task A_move_disturbs_nothing_but_the_places()
        {
            var platform = GivenAPortfolio("Platform");
            var ids = GivenTheTrackersOrderReads(platform, SearchIndex, LegacyImporter, PartnerCatalogue);
            GivenAFinishedFeature("Shipped last quarter", "FTR-DONE", "0", platform);
            await GivenThisInstanceOwnsTheOrder();

            var whatTheTrackerWrote = GivenTheOrderingColumnsAsStored();
            await WhenTheProductOwnerPlacesItAbove(ids[2], ids[0]);

            ThenTheTrackersOwnValuesAreUnchangedFrom(whatTheTrackerWrote);
        }
    }
}
