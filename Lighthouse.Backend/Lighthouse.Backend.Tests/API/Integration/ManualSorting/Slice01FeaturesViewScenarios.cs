using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.ManualSorting
{
    /// <summary>
    /// DISTILL acceptance scenarios (Epic 5375) — Slice 01: one place listing every Feature you may see,
    /// in the order that drives the forecast, each row saying where it sits across the whole instance.
    /// Walking skeleton: the product owner opens the Features view and reads the ranked list.
    /// Driving port: the Features read endpoint. US-01, AC-1.2 … AC-1.9.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5375-manual-sorting")]
    [Category("slice-01")]
    public partial class Slice01FeaturesViewTest
    {
        private static readonly string[] PlatformOnly = ["Feature ranked 4", "Feature ranked 17"];
        private static readonly int[] PlatformPositions = [4, 17];
        private static readonly int[] RequestedPortfolios = [41, 42, 43];

        // @walking_skeleton @driving_port @AC-1.2
        [Test]
        public async Task The_product_owner_sees_every_feature_from_the_portfolios_they_may_read_and_nothing_else()
        {
            var platform = GivenAPortfolio("Platform");
            var growth = GivenAPortfolio("Growth");
            GivenAFeatureTheTrackerRanked("Rebuild the search index", "1", platform);
            GivenAFeatureTheTrackerRanked("Retire the legacy importer", "2", growth);
            GivenAFeatureTheTrackerRanked("Publish the partner catalogue", "3", platform);
            GivenTheCallerMayReadOnly(platform);

            var response = await WhenTheProductOwnerOpensTheFeaturesView();

            string[] expected = ["Rebuild the search index", "Publish the partner catalogue"];
            ThenExactlyTheseFeaturesAreListed(response, expected);
        }

        // @driving_port @AC-1.5 — the literal proof case: two rows shown next to each other read 4 and 17
        [Test]
        public async Task Two_features_shown_next_to_each_other_report_their_places_across_the_whole_instance()
        {
            var platform = GivenAPortfolio("Platform");
            var growth = GivenAPortfolio("Growth");
            GivenTheInstanceIsRankedFromOneTo(20, rank => rank is 4 or 17 ? [platform] : [growth]);
            GivenTheCallerMayReadOnly(platform);

            var response = await WhenTheProductOwnerOpensTheFeaturesView();

            ThenExactlyTheseFeaturesAreListed(response, PlatformOnly);
            ThenTheListedPositionsAre(response, PlatformPositions);
        }

        // @AC-1.5 — the place belongs to the instance, not to whoever is looking: callers holding disjoint
        // scopes over the two Portfolios a Feature shares must all read the same number for it (ADR-135).
        [Test]
        public async Task Callers_with_disjoint_scopes_read_the_same_place_for_the_feature_they_share()
        {
            var platform = GivenAPortfolio("Platform");
            var payments = GivenAPortfolio("Payments");
            GivenAFeatureTheTrackerRanked("Rebuild the search index", "1", platform);
            GivenAFeatureTheTrackerRanked("Move checkout to the new gateway", "2", platform, payments);
            GivenAFeatureTheTrackerRanked("Retire the legacy importer", "3", payments);

            GivenTheCallerMayReadOnly(platform);
            var throughPlatform = await WhenTheProductOwnerOpensTheFeaturesView();

            GivenTheCallerMayWriteOnly(payments);
            var throughPayments = await WhenTheProductOwnerOpensTheFeaturesView();

            GivenTheCallerAdministersTheInstance();
            var throughTheWholeInstance = await WhenTheProductOwnerOpensTheFeaturesView();

            ThenTheSharedFeatureReportsTheSamePlaceToEveryCaller(
                "Move checkout to the new gateway", throughPlatform, throughPayments, throughTheWholeInstance);
        }

        // @AC-1.4 — many-to-many membership (S7); the shared-Feature case the dev instance has none of
        [Test]
        public async Task A_feature_two_portfolios_share_is_listed_once_and_names_both()
        {
            var platform = GivenAPortfolio("Platform");
            var payments = GivenAPortfolio("Payments");
            GivenAFeatureTheTrackerRanked("Move checkout to the new gateway", "1", platform, payments);
            GivenTheCallerMayReadOnly(platform, payments);

            var response = await WhenTheProductOwnerOpensTheFeaturesView();

            string[] bothPortfolios = ["Platform", "Payments"];
            ThenTheFeatureIsListedOnceNaming(response, "Move checkout to the new gateway", bothPortfolios);
        }

        // @AC-1.2 refined by ADR-136 §1 — a Feature in no Portfolio is visible to everyone
        [Test]
        public async Task A_feature_belonging_to_no_portfolio_stays_visible()
        {
            var platform = GivenAPortfolio("Platform");
            GivenAFeatureTheTrackerRanked("Rebuild the search index", "1", platform);
            GivenAFeatureTheTrackerRanked("Left over from a deleted portfolio", "2");
            GivenTheCallerMayReadOnly(platform);

            var response = await WhenTheProductOwnerOpensTheFeaturesView();

            string[] expected = ["Rebuild the search index", "Left over from a deleted portfolio"];
            ThenExactlyTheseFeaturesAreListed(response, expected);
        }

        // @AC-1.3 — the view is general infrastructure, not a premium sorting page (D12)
        [Test]
        public async Task The_features_view_opens_on_an_instance_with_no_premium_licence()
        {
            var platform = GivenAPortfolio("Platform");
            GivenAFeatureTheTrackerRanked("Rebuild the search index", "1", platform);
            GivenTheInstanceHasNoPremiumLicence();
            GivenTheCallerMayReadOnly(platform);

            var response = await WhenTheProductOwnerOpensTheFeaturesView();

            ThenTheViewOpened(response);
            string[] expected = ["Rebuild the search index"];
            ThenExactlyTheseFeaturesAreListed(response, expected);
        }

        // @AC-1.7 — finished Features keep their place, so hiding them cannot renumber the rest (DDD-5)
        [Test]
        public async Task A_finished_feature_still_occupies_its_place_in_the_order()
        {
            var platform = GivenAPortfolio("Platform");
            GivenAFeatureTheTrackerRanked("Rebuild the search index", "1", platform);
            GivenAFinishedFeature("Ship the pricing page", "2", platform);
            GivenAFeatureTheTrackerRanked("Publish the partner catalogue", "3", platform);
            GivenTheCallerMayReadOnly(platform);

            var response = await WhenTheProductOwnerOpensTheFeaturesView();

            int[] expectedPositions = [1, 2, 3];
            ThenTheListedPositionsAre(response, expectedPositions);
        }

        // @error @AC-1.8 — a tracker that never ranked this Feature (ServiceNow, a CSV without the column)
        [Test]
        public async Task A_feature_the_tracker_never_ranked_still_reports_its_place()
        {
            var platform = GivenAPortfolio("Platform");
            GivenAFeatureTheTrackerRanked("Rebuild the search index", "1", platform);
            GivenAFeatureTheTrackerNeverRanked("Arrived without a rank", platform);
            GivenTheCallerMayReadOnly(platform);

            var response = await WhenTheProductOwnerOpensTheFeaturesView();

            ThenEveryListedFeatureReportsAPosition(response);
        }

        // @AC-1.2 — the rows and their places are numbered by one and the same key, so a rank the tracker
        // repeated, or never gave at all, can never leave the list reading out of position order.
        [Test]
        public async Task Features_the_tracker_ranked_alike_are_listed_in_the_order_their_places_claim()
        {
            var platform = GivenAPortfolio("Platform");
            GivenAFeatureTheTrackerRanked("Rebuild the search index", "5", platform);
            GivenAFeatureTheTrackerRanked("Retire the legacy importer", "5", platform);
            GivenAFeatureTheTrackerNeverRanked("Arrived without a rank", platform);
            GivenAFeatureTheTrackerNeverRanked("Also arrived without a rank", platform);
            GivenAFeatureTheTrackerRanked("Publish the partner catalogue", "1", platform);
            GivenTheCallerMayReadOnly(platform);

            var response = await WhenTheProductOwnerOpensTheFeaturesView();

            ThenTheListedPositionsAscend(response);
        }

        // @AC-1.9 (backend half) — the read port answers for an instance of five hundred Features
        [Test]
        public async Task The_features_view_answers_for_an_instance_of_five_hundred_features()
        {
            var platform = GivenAPortfolio("Platform");
            GivenTheInstanceIsRankedFromOneTo(500, _ => [platform]);
            GivenTheCallerMayReadOnly(platform);

            var response = await WhenTheProductOwnerOpensTheFeaturesView();

            ThenThisManyFeaturesAreListed(response, 500);
            ThenEveryListedFeatureReportsAPosition(response);
        }

        // --- The writable batch (OQ-1 closure). Each early return is its own scenario, because getting
        //     one wrong is silent over-permission on a write path, not a visible error. ---

        // @AC-1.2 @branch — self-hosted single-user default: everyone may move everything
        [Test]
        public async Task With_access_control_switched_off_every_portfolio_is_writable()
        {
            using var store = BuildIsolatedContext();
            var subject = GivenAccessControlIsSwitchedOff(store);

            var writable = await subject.GetWritablePortfolioIdsAsync(PrincipalFor("visitor"), RequestedPortfolios, CancellationToken.None);

            ThenEveryPortfolioIsWritable(writable, RequestedPortfolios);
        }

        // @error @branch — a half-configured instance must fail closed
        [Test]
        public async Task With_access_control_only_half_configured_no_portfolio_is_writable()
        {
            using var store = BuildIsolatedContext();
            var subject = GivenAccessControlIsOnButUnusable(store);

            var writable = await subject.GetWritablePortfolioIdsAsync(PrincipalFor("someone"), RequestedPortfolios, CancellationToken.None);

            ThenNoPortfolioIsWritable(writable);
        }

        // @branch — an access-control manager already passes the per-Portfolio check everywhere
        [Test]
        public async Task Whoever_administers_access_control_may_write_every_portfolio()
        {
            using var store = BuildIsolatedContext();
            var subject = await GivenAccessControlIsOnWithAnAdministrator(store, "the-administrator");

            var writable = await subject.GetWritablePortfolioIdsAsync(PrincipalFor("the-administrator"), RequestedPortfolios, CancellationToken.None);

            ThenEveryPortfolioIsWritable(writable, RequestedPortfolios);
        }

        // @error @branch — an unrecognised caller must fail closed
        [Test]
        public async Task An_unrecognised_caller_may_write_no_portfolio()
        {
            using var store = BuildIsolatedContext();
            var subject = await GivenAccessControlIsOnAndTheCallerIsUnrecognised(store);

            var writable = await subject.GetWritablePortfolioIdsAsync(PrincipalFor("stranger"), RequestedPortfolios, CancellationToken.None);

            ThenNoPortfolioIsWritable(writable);
        }

        // @error @branch — the predicate swap itself: reading a Portfolio never implies writing it
        [Test]
        public async Task Someone_who_may_only_read_a_portfolio_may_not_write_it()
        {
            using var store = BuildIsolatedContext();
            var subject = await GivenAccessControlIsOnWithAReaderOf(store, "the-reader", RequestedPortfolios[0]);

            var readable = await subject.GetReadablePortfolioIdsAsync(PrincipalFor("the-reader"), RequestedPortfolios, CancellationToken.None);
            var writable = await subject.GetWritablePortfolioIdsAsync(PrincipalFor("the-reader"), RequestedPortfolios, CancellationToken.None);

            int[] onlyTheReadOne = [RequestedPortfolios[0]];
            Assert.That(readable, Is.EqualTo(onlyTheReadOne), "The reader may read the one Portfolio they were given.");
            ThenNoPortfolioIsWritable(writable);
        }
    }
}
