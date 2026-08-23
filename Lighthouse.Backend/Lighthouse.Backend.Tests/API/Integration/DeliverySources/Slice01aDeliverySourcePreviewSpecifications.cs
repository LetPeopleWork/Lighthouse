using System.Net;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliverySources;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.DeliverySources
{
    /// <summary>
    /// Step definitions for the three delivery-source routes, seen from outside the process. The
    /// backend-observable contract: a Portfolio whose connection can read remote delivery objects is
    /// offered them, one whose connection cannot is handed an empty list rather than an error, a key
    /// the connection does not offer is not found, and asking what a binding would mean needs both a
    /// premium licence and write access - while merely listing needs neither.
    /// </summary>
    public partial class Slice01aDeliverySourcePreviewTest : DeliverySourcesAcceptanceTest
    {
        private const string TheBoundRelease = "10007";
        private const string TheDatedRelease = "10004";
        private const string TheUndatedRelease = "10005";
        private const string TheTrackedFeature = "LGH-1";
        private const string TheTrackedFeatureName = "Ship the thing";
        private const string AKeyNobodyOffers = "release-train";

        private static readonly DateTime TheReleaseDate = new(2026, 8, 22, 0, 0, 0, DateTimeKind.Utc);

        private static readonly DeliverySourceProject TheJiraProject = new("LGH", "Lighthouse");

        private static readonly DeliverySourceOption[] ADatedAndAnUndatedRelease =
        [
            new DeliverySourceOption(TheDatedRelease, "Release 1.0", TheReleaseDate, TheJiraProject, false, false, null),
            new DeliverySourceOption(TheUndatedRelease, "Release 2.0", null, TheJiraProject, false, false, SourceOptionBlockReason.NoDateSet),
        ];

        /// <summary>
        /// Every tracker other than Jira. None of their connectors implements the delivery-source
        /// reading port, and that absence is the whole degradation mechanism - so these run against the
        /// production connectors rather than against a double written here.
        /// </summary>
        private static readonly WorkTrackingSystems[] SystemsThatOfferNoDeliverySources =
        [
            WorkTrackingSystems.AzureDevOps,
            WorkTrackingSystems.Linear,
            WorkTrackingSystems.ServiceNow,
            WorkTrackingSystems.Csv,
        ];

        // --- Given ---

        private int GivenAJiraPortfolioOfferingItsReleases()
        {
            var portfolioId = SeedPortfolioOn(WorkTrackingSystems.Jira);

            TheJiraConnectionOffersItsReleases();
            TheReleasePickerOffers(ADatedAndAnUndatedRelease);

            return portfolioId;
        }

        private int GivenAPortfolioOn(WorkTrackingSystems system) => SeedPortfolioOn(system);

        private int GivenAJiraPortfolioWhoseConnectionOffersNothing()
        {
            var portfolioId = SeedPortfolioOn(WorkTrackingSystems.Jira);
            TheJiraConnectionOffersNothing();

            return portfolioId;
        }

        private void GivenTheReleaseCarriesWorkThisPortfolioTracks(int portfolioId)
        {
            SeedTrackedFeature(portfolioId, TheTrackedFeature, TheTrackedFeatureName);
            TheRemoteSays(TheBoundRelease, new DeliverySourceResolution.Resolved(
                new DeliverySourceSnapshot("Release 3.0", TheReleaseDate, [TheTrackedFeature])));
        }

        private void GivenNobodyTaggedAnythingAgainstTheRelease()
        {
            TheRemoteSays(TheBoundRelease, new DeliverySourceResolution.Resolved(
                new DeliverySourceSnapshot("Release 3.0", TheReleaseDate, [])));
        }

        private void GivenTheCallerMayOnlyReadThisPortfolio(int portfolioId)
            => Client.AsPortfolioViewer(portfolioId);

        private void GivenTheInstanceIsNotLicensed() => TheInstanceIsNotLicensedForPremium();

        // --- When ---

        private Task<HttpResponseMessage> WhenTheDeliverySourcesAreListed(string prefix, int portfolioId)
            => GetTheDeliverySources(prefix, portfolioId);

        private Task<HttpResponseMessage> WhenThePickerIsOpened(string prefix, int portfolioId, string sourceKey = JiraReleaseSourceKey)
            => GetTheOptions(prefix, portfolioId, sourceKey);

        private Task<HttpResponseMessage> WhenThePreviewIsAskedFor(string prefix, int portfolioId, string sourceKey = JiraReleaseSourceKey)
            => PostThePreview(prefix, portfolioId, sourceKey, TheBoundRelease);

        // --- Then ---

        private static async Task ThenTheOnlySourceOfferedIsTheJiraRelease(HttpResponseMessage response)
        {
            var sources = await SourcesIn(response);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(sources, Has.Count.EqualTo(1));
                Assert.That(sources[0].Key, Is.EqualTo(JiraReleaseSourceKey));
                Assert.That(sources[0].DisplayName, Is.EqualTo(JiraReleaseSourceDisplayName));
            }
        }

        private static async Task ThenNoSourceIsOffered(HttpResponseMessage response)
        {
            Assert.That(await SourcesIn(response), Is.Empty,
                "a connection with nothing to offer is answered rather than failed, so the tab disappears instead of showing a screen that can only break.");
        }

        private static async Task ThenThePickerShowsBothReleasesAndGreysOutTheUndatedOne(HttpResponseMessage response)
        {
            var options = await OptionsIn(response);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(options, Has.Count.EqualTo(2));
                Assert.That(options.Single(o => o.Id == TheDatedRelease).IsSelectable, Is.True);
                Assert.That(options.Single(o => o.Id == TheUndatedRelease).IsSelectable, Is.False);
                Assert.That(options.Single(o => o.Id == TheUndatedRelease).BlockedBecause,
                    Is.EqualTo(SourceOptionBlockReason.NoDateSet));
            }
        }

        private static async Task ThenThePreviewShowsTheReleaseAndTheWorkComingAlong(HttpResponseMessage response)
        {
            var preview = await PreviewIn(response);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(preview.Name, Is.EqualTo("Release 3.0"));
                Assert.That(preview.Date, Is.EqualTo(TheReleaseDate));
                Assert.That(preview.Features, Has.Count.EqualTo(1));
                Assert.That(preview.Features[0].ReferenceId, Is.EqualTo(TheTrackedFeature));
                Assert.That(preview.EmptyBecause, Is.EqualTo(DeliverySourcePreviewEmptyReason.None));
            }
        }

        private static async Task ThenThePreviewSaysNothingIsTaggedAgainstTheSource(HttpResponseMessage response)
        {
            var preview = await PreviewIn(response);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(preview.Features, Is.Empty);
                Assert.That(preview.EmptyBecause, Is.EqualTo(DeliverySourcePreviewEmptyReason.NothingTaggedAgainstTheSource),
                    "an empty preview without a reason leaves the reader guessing between two problems fixed in completely different places.");
            }
        }

        private static void ThenTheAnswerIs(HttpResponseMessage response, HttpStatusCode expected)
            => Assert.That(response.StatusCode, Is.EqualTo(expected));

        /// <summary>
        /// The guard against the whole fixture passing for the wrong reason: a connector double that did
        /// not carry the delivery-source port would leave the controller on the "nothing offered" path,
        /// where every assertion about an empty answer still holds.
        /// </summary>
        private void ThenTheJiraConnectionWasTheOneAsked()
            => JiraConnector.Verify(c => c.AvailableSources(It.IsAny<WorkTrackingSystemConnection>()), Times.AtLeastOnce);

        private void ThenTheJiraConnectionWasNeverAsked()
            => JiraConnector.Verify(c => c.AvailableSources(It.IsAny<WorkTrackingSystemConnection>()), Times.Never);
    }
}
