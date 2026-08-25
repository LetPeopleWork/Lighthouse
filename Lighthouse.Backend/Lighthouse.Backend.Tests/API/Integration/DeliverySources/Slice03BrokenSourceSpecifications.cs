using System.Net;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliverySources;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.DeliverySources
{
    /// <summary>
    /// Step definitions for a Delivery whose source stops answering. Everything after the Delivery is
    /// created is the scheduled refresh, and everything observed is the read the grid makes.
    /// </summary>
    public partial class Slice03BrokenSourceTest : DeliverySourceRefreshAcceptanceTest
    {
        private const string TheRelease = "10007";
        private const string TheReleaseName = "Release 3.0";
        private const string TheWorkTheReleaseCarries = "LGH-1";

        private static readonly DateTime TheReleaseDate = new(2027, 3, 18, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime TheDateTheReleaseHasNow = new(2027, 6, 24, 0, 0, 0, DateTimeKind.Utc);

        // --- Given ---

        /// <summary>
        /// The Delivery is created bound and then given one successful refresh, so the values it is
        /// holding when its source goes away are values the source actually gave it — which is what
        /// makes "it kept them" mean anything.
        /// </summary>
        private async Task<int> GivenADeliveryJustBoundToItsRelease(string prefix)
        {
            var portfolioId = SeedPortfolioOn(WorkTrackingSystems.Jira);

            TheJiraConnectionOffersItsReleases();
            SeedTrackedFeature(portfolioId, TheWorkTheReleaseCarries, "Ship the thing");
            GivenTheReleaseIsAliveAndDated(TheReleaseDate);

            var created = await PostTheDelivery(prefix, portfolioId, ARequestToFollowTheRelease());
            Assert.That(created.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            return portfolioId;
        }

        private async Task<int> GivenADeliveryThatHasHeardFromItsRelease(string prefix)
        {
            var portfolioId = SeedPortfolioOn(WorkTrackingSystems.Jira);

            TheJiraConnectionOffersItsReleases();
            SeedTrackedFeature(portfolioId, TheWorkTheReleaseCarries, "Ship the thing");
            GivenTheReleaseIsAliveAndDated(TheReleaseDate);

            var created = await PostTheDelivery(prefix, portfolioId, ARequestToFollowTheRelease());
            Assert.That(created.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            await ThePortfolioRefreshRuns(portfolioId);

            return portfolioId;
        }

        private static UpdateDeliveryRequest ARequestToFollowTheRelease()
            => new()
            {
                Name = TheReleaseName,
                Date = TheReleaseDate,
                FeatureIds = [],
                SelectionMode = DeliverySelectionMode.SourceBound,
                SourceKey = JiraReleaseSourceKey,
                SourceReference = TheRelease,
            };

        private void GivenTheReleaseIsAliveAndDated(DateTime date)
            => TheRemoteSays(TheRelease, new DeliverySourceResolution.Resolved(
                new DeliverySourceSnapshot(TheReleaseName, date, [TheWorkTheReleaseCarries])));

        private void GivenTheReleaseIsBackAndRescheduled() => GivenTheReleaseIsAliveAndDated(TheDateTheReleaseHasNow);

        private void GivenTheReleaseHasBeenDeletedInJira()
            => TheRemoteSays(TheRelease, new DeliverySourceResolution.NotFound());

        private void GivenTheReleaseLostItsDateInJira()
            => TheRemoteSays(TheRelease, new DeliverySourceResolution.NoDate(TheReleaseName));

        private void GivenTheReleaseCouldNotBeReadThisTime()
            => TheRemoteSays(TheRelease, new DeliverySourceResolution.Unavailable(
                DeliverySourceUnavailableReason.SourceReadFailed));

        private void GivenTheConnectionNoLongerOffersReleases() => TheJiraConnectionOffersNothing();

        // --- When ---

        private Task<HttpResponseMessage> WhenTheDeliveryIsTakenBackByHand(string prefix, int deliveryId)
            => PutTheDelivery(prefix, deliveryId, new UpdateDeliveryRequest
            {
                Name = "Taken back by hand",
                Date = TheDateTheReleaseHasNow,
                FeatureIds = [],
                SelectionMode = DeliverySelectionMode.Manual,
            });

        // --- Then ---

        private static void ThenTheDeliverySaysItsSourceIsFinished(DeliveryRow delivery, string expectedReason)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery.SourceUnavailableReason?.ToString(), Is.EqualTo(expectedReason));
                Assert.That(delivery.Name, Is.EqualTo(TheReleaseName),
                    "the values are kept - they are why the Delivery is still worth reading.");
                Assert.That(delivery.Date, Is.EqualTo(TheReleaseDate));
                Assert.That(delivery.Features, Has.Count.EqualTo(1));
                Assert.That(delivery.SelectionMode, Is.EqualTo(DeliverySelectionMode.SourceBound),
                    "nothing unbinds on its own.");
                Assert.That(delivery.SourceLastSyncedOn, Is.Not.Null,
                    "the screen has to be able to say how stale the frozen values are.");
            }
        }

        private static void ThenTheDeliverySaysNothingIsWrongWithItsSource(DeliveryRow delivery)
            => Assert.That(delivery.SourceUnavailableReason, Is.Null);

        private static void ThenTheDeliverySays(DeliveryRow delivery, string expectedName, DateTime expectedDate)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery.Name, Is.EqualTo(expectedName));
                Assert.That(delivery.Date, Is.EqualTo(expectedDate));
            }
        }

        private static void ThenTheDeliveryIsItsOwnAgainCarryingWhatTheReleaseLeft(DeliveryRow delivery)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery.SelectionMode, Is.EqualTo(DeliverySelectionMode.Manual));
                Assert.That(delivery.SourceKey, Is.Null);
                Assert.That(delivery.SourceReference, Is.Null);
                Assert.That(delivery.SourceUnavailableReason, Is.Null,
                    "a Delivery that follows nothing must not go on saying a source it no longer has is broken.");
                Assert.That(delivery.SourceLastSyncedOn, Is.Null);
                Assert.That(delivery.Name, Is.EqualTo(TheReleaseName),
                    "the last synced name is why somebody releases a Delivery rather than deleting it.");
                Assert.That(delivery.Date, Is.EqualTo(TheReleaseDate));
            }
        }
    }
}
