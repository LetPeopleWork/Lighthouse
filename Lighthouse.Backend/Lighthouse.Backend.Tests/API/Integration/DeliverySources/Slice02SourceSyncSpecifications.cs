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
    public partial class Slice02SourceSyncTest : DeliverySourcesAcceptanceTest
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

        /// <summary>
        /// The refresh fetches Features before it syncs the sources, and that fetch is not this
        /// Epic's. Faked whole, it leaves the Portfolio tracking exactly the Feature the scenario
        /// seeded, so what the Delivery ends up holding is the source pass's doing and nothing else's.
        /// Everything after the fetch - the resolver, the sync, the aggregate, EF and the read the
        /// grid makes - is the shipped code.
        /// </summary>
        protected override void AlsoSwap(IServiceCollection services)
        {
            var featureFetch = new Mock<IWorkItemService>();
            featureFetch
                .Setup(fetch => fetch.UpdateFeaturesForPortfolio(It.IsAny<Portfolio>()))
                .ReturnsAsync(SyncOutcome.None);

            services.RemoveAll<IWorkItemService>();
            services.AddScoped(_ => featureFetch.Object);
        }

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

        private static void ThenNothingAboutTheDeliveryMoved(DeliveryRow before, DeliveryRow after)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(after.Name, Is.EqualTo(before.Name));
                Assert.That(after.Date, Is.EqualTo(before.Date));
                Assert.That(after.Features, Is.EqualTo(before.Features));
                Assert.That(after.ConcurrencyToken, Is.EqualTo(before.ConcurrencyToken),
                    "moving the version on a refresh that changed nothing fails an open browser's next save for nobody's edit.");
            }
        }

        private void ThenTheRefreshWasRecordedAsHavingWorked(int portfolioId)
        {
            using var scope = Factory.Services.CreateScope();
            var logs = scope.ServiceProvider.GetRequiredService<IRefreshLogService>()
                .GetRefreshLogs()
                .Where(entry => entry.Type == RefreshType.Portfolio && entry.EntityId == portfolioId)
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(logs, Is.Not.Empty, "the refresh has to have run for its outcome to mean anything.");
                Assert.That(logs.TrueForAll(entry => entry.Success), Is.True,
                    "one source nobody can read must not be reported as the whole Portfolio refresh having failed.");
            }
        }

        private void ThenTheRetiredDeliveryStillSays(int deliveryId, string expectedName, DateTime expectedDate)
        {
            using var scope = Factory.Services.CreateScope();
            var delivery = scope.ServiceProvider.GetRequiredService<IDeliveryRepository>().GetById(deliveryId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery, Is.Not.Null);
                Assert.That(delivery!.ArchivedOn, Is.Not.Null);
                Assert.That(delivery.Name, Is.EqualTo(expectedName));
                Assert.That(delivery.Date, Is.EqualTo(expectedDate));
                Assert.That(delivery.SourceLastSyncedOn, Is.Null,
                    "a retired Delivery was never asked, so it cannot have heard from the Release either.");
            }
        }
    }
}
