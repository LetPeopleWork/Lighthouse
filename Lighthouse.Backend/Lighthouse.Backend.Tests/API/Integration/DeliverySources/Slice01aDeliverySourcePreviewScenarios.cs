using System.Net;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.DeliverySources
{
    /// <summary>
    /// Acceptance scenarios for seeing what a Release would give you, driven over HTTP. Everything
    /// these routes promise - the two version prefixes, the empty-list degradation, the not-found for a
    /// key nobody offers, and the two guards that disagree on purpose about who may ask what - is a
    /// promise about the pipeline in front of the controller, so a scenario has to make a request to
    /// say anything about it.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5565-delivery-date-sync")]
    [Category("slice-01a")]
    public partial class Slice01aDeliverySourcePreviewTest
    {
        [TestCase(ApiV1Prefix)]
        [TestCase(ApiLatestPrefix)]
        public async Task The_three_delivery_source_routes_answer_over_HTTP_on_both_api_versions(string prefix)
        {
            var portfolio = GivenAJiraPortfolioOfferingItsReleases();
            GivenTheReleaseCarriesWorkThisPortfolioTracks(portfolio);

            var sources = await WhenTheDeliverySourcesAreListed(prefix, portfolio);
            var options = await WhenThePickerIsOpened(prefix, portfolio);
            var preview = await WhenThePreviewIsAskedFor(prefix, portfolio);

            ThenTheAnswerIs(sources, HttpStatusCode.OK);
            await ThenTheOnlySourceOfferedIsTheJiraRelease(sources);
            ThenTheAnswerIs(options, HttpStatusCode.OK);
            await ThenThePickerShowsBothReleasesAndGreysOutTheUndatedOne(options);
            ThenTheAnswerIs(preview, HttpStatusCode.OK);
            await ThenThePreviewShowsTheReleaseAndTheWorkComingAlong(preview);
            ThenTheJiraConnectionWasTheOneAsked();
        }

        [TestCaseSource(nameof(SystemsThatOfferNoDeliverySources))]
        public async Task A_Portfolio_on_a_tracker_that_cannot_read_delivery_objects_is_offered_an_empty_list(
            WorkTrackingSystems system)
        {
            var portfolio = GivenAPortfolioOn(system);

            var sources = await WhenTheDeliverySourcesAreListed(ApiLatestPrefix, portfolio);

            ThenTheAnswerIs(sources, HttpStatusCode.OK);
            await ThenNoSourceIsOffered(sources);
            ThenTheJiraConnectionWasNeverAsked();
        }

        [Test]
        public async Task A_source_key_the_connection_does_not_offer_is_not_found_over_HTTP()
        {
            var portfolio = GivenAJiraPortfolioOfferingItsReleases();

            var options = await WhenThePickerIsOpened(ApiLatestPrefix, portfolio, AKeyNobodyOffers);
            var preview = await WhenThePreviewIsAskedFor(ApiLatestPrefix, portfolio, AKeyNobodyOffers);

            ThenTheAnswerIs(options, HttpStatusCode.NotFound);
            ThenTheAnswerIs(preview, HttpStatusCode.NotFound);
        }

        [Test]
        public async Task A_connection_that_currently_offers_no_source_at_all_is_never_asked_to_go_and_look()
        {
            var portfolio = GivenAJiraPortfolioWhoseConnectionOffersNothing();

            var options = await WhenThePickerIsOpened(ApiLatestPrefix, portfolio);
            var preview = await WhenThePreviewIsAskedFor(ApiLatestPrefix, portfolio);

            ThenTheAnswerIs(options, HttpStatusCode.NotFound);
            ThenTheAnswerIs(preview, HttpStatusCode.NotFound);
        }

        [Test]
        public async Task A_reader_may_see_which_sources_exist_but_not_what_binding_one_would_mean()
        {
            var portfolio = GivenAJiraPortfolioOfferingItsReleases();
            GivenTheReleaseCarriesWorkThisPortfolioTracks(portfolio);
            GivenTheCallerMayOnlyReadThisPortfolio(portfolio);

            var sources = await WhenTheDeliverySourcesAreListed(ApiLatestPrefix, portfolio);
            var options = await WhenThePickerIsOpened(ApiLatestPrefix, portfolio);
            var preview = await WhenThePreviewIsAskedFor(ApiLatestPrefix, portfolio);

            ThenTheAnswerIs(sources, HttpStatusCode.OK);
            await ThenTheOnlySourceOfferedIsTheJiraRelease(sources);
            ThenTheAnswerIs(options, HttpStatusCode.Forbidden);
            ThenTheAnswerIs(preview, HttpStatusCode.Forbidden);
        }

        [Test]
        public async Task An_instance_without_premium_still_sees_which_sources_exist_and_is_refused_the_preview()
        {
            var portfolio = GivenAJiraPortfolioOfferingItsReleases();
            GivenTheReleaseCarriesWorkThisPortfolioTracks(portfolio);
            GivenTheInstanceIsNotLicensed();

            var sources = await WhenTheDeliverySourcesAreListed(ApiLatestPrefix, portfolio);
            var preview = await WhenThePreviewIsAskedFor(ApiLatestPrefix, portfolio);

            ThenTheAnswerIs(sources, HttpStatusCode.OK);
            await ThenTheOnlySourceOfferedIsTheJiraRelease(sources);
            ThenTheAnswerIs(preview, HttpStatusCode.Forbidden);
        }

        [Test]
        public async Task A_Release_nobody_tagged_any_work_against_is_answered_rather_than_failed()
        {
            var portfolio = GivenAJiraPortfolioOfferingItsReleases();
            GivenNobodyTaggedAnythingAgainstTheRelease();

            var preview = await WhenThePreviewIsAskedFor(ApiLatestPrefix, portfolio);

            ThenTheAnswerIs(preview, HttpStatusCode.OK);
            await ThenThePreviewSaysNothingIsTaggedAgainstTheSource(preview);
        }
    }
}
