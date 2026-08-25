using System.Net;
using System.Net.Http.Json;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliverySources;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.DeliverySources
{
    /// <summary>
    /// Step definitions for keeping a bound Delivery in step with its Release. The Delivery is created
    /// the way a person creates one - over HTTP - and then nobody touches it again: everything after
    /// that is the scheduled refresh, and every observation is the read the grid makes.
    /// </summary>
    public partial class Slice02SourceSyncTest : DeliverySourceRefreshAcceptanceTest
    {
        private const string TheRelease = "10007";
        private const string TheReleaseName = "Release 3.0";
        private const string TheNameTheReleaseHasNow = "Release 3.0 (slipped)";
        private const string TheWorkTheReleaseCarries = "LGH-1";

        private const string ANameSomebodyTyped = "Cut by hand";

        private static readonly DateTime TheReleaseDate = new(2027, 3, 18, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime TheDateTheReleaseHasNow = new(2027, 6, 24, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime ADateThatHasBeenAndGone = new(2024, 2, 6, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime ADateSomebodyPicked = new(2027, 9, 30, 0, 0, 0, DateTimeKind.Utc);

        // --- Given ---

        private async Task<int> GivenADeliveryFollowingTheRelease(string prefix)
        {
            var portfolioId = GivenAJiraPortfolioTrackingTheWorkTheReleaseCarries();

            var created = await PostTheDelivery(prefix, portfolioId, new UpdateDeliveryRequest
            {
                Name = TheReleaseName,
                Date = TheReleaseDate,
                FeatureIds = [],
                SelectionMode = DeliverySelectionMode.SourceBound,
                SourceKey = JiraReleaseSourceKey,
                SourceReference = TheRelease,
            });

            Assert.That(created.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            return portfolioId;
        }

        private async Task<int> GivenADeliveryNobodyBoundToAnything(string prefix)
        {
            var portfolioId = GivenAJiraPortfolioTrackingTheWorkTheReleaseCarries();

            var created = await PostTheDelivery(prefix, portfolioId, new UpdateDeliveryRequest
            {
                Name = ANameSomebodyTyped,
                Date = ADateSomebodyPicked,
                FeatureIds = [],
                SelectionMode = DeliverySelectionMode.Manual,
            });

            Assert.That(created.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            return portfolioId;
        }

        private int GivenAJiraPortfolioTrackingTheWorkTheReleaseCarries()
        {
            var portfolioId = SeedPortfolioOn(WorkTrackingSystems.Jira);

            TheJiraConnectionOffersItsReleases();
            SeedTrackedFeature(portfolioId, TheWorkTheReleaseCarries, "Ship the thing");

            GivenTheReleaseNowCarries(TheReleaseName, TheReleaseDate);

            return portfolioId;
        }

        private void GivenTheReleaseHasBeenRenamedAndRescheduledInJira()
            => GivenTheReleaseNowCarries(TheNameTheReleaseHasNow, TheDateTheReleaseHasNow);

        private void GivenTheReleaseNowCarries(string name, DateTime date)
        {
            TheRemoteSays(TheRelease, new DeliverySourceResolution.Resolved(
                new DeliverySourceSnapshot(name, date, [TheWorkTheReleaseCarries])));
        }

        private void GivenJiraCannotBeAskedAboutTheReleaseAtAll() => TheRemoteCannotBeAskedAtAll();

        private async Task GivenTheDeliveryHasBeenRetired(int deliveryId)
        {
            var retired = await Client.PostAsJsonAsync($"/{ApiLatestPrefix}/deliveries/{deliveryId}/archive", new ArchiveDeliveryRequest());

            Assert.That(retired.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        // --- Then ---

        private static void ThenTheDeliverySays(DeliveryRow delivery, string expectedName, DateTime expectedDate)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery.Name, Is.EqualTo(expectedName));
                Assert.That(delivery.Date, Is.EqualTo(expectedDate));
                Assert.That(delivery.SelectionMode, Is.EqualTo(DeliverySelectionMode.SourceBound),
                    "a Delivery that took what the Release says must still be following it afterwards.");
            }
        }

        /// <summary>
        /// Read back over HTTP rather than off the aggregate, so it also says the stamp survived the
        /// save - a value written in memory and lost on the way to the database would leave every
        /// "nothing changed" scenario passing and slice 03 with no date to show.
        /// </summary>
        private static void ThenTheReleaseWasHeardFrom(DeliveryRow delivery)
            => Assert.That(delivery.SourceLastSyncedOn, Is.Not.Null,
                "a refresh that asked the Release and chose to write nothing still records that it asked; " +
                "without this the scenario passes just as well with the sync pass deleted.");

        private static void ThenNobodyAskedAnySourceAbout(DeliveryRow delivery)
            => Assert.That(delivery.SourceLastSyncedOn, Is.Null);

        /// <summary>
        /// The version is what says nothing wrote to it, and it is the only thing that can say so here.
        /// The last-heard-from stamp cannot: binding reads the source successfully and stamps it, and a
        /// refresh on the same day would write the very same day back, so the two are indistinguishable
        /// by that field alone.
        /// </summary>
        private void ThenTheRetiredDeliveryStillSays(DeliveryRow asItWasBeforeRetiring, string expectedName, DateTime expectedDate)
        {
            using var scope = Factory.Services.CreateScope();
            var delivery = scope.ServiceProvider.GetRequiredService<IDeliveryRepository>().GetById(asItWasBeforeRetiring.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery, Is.Not.Null);
                Assert.That(delivery!.ArchivedOn, Is.Not.Null);
                Assert.That(delivery.Name, Is.EqualTo(expectedName));
                Assert.That(delivery.Date, Is.EqualTo(expectedDate));
                Assert.That(delivery.SourceLastSyncedOn, Is.EqualTo(asItWasBeforeRetiring.SourceLastSyncedOn),
                    "a retired Delivery is never asked, so nothing about when it last heard from its source may move.");
            }
        }
    }
}
