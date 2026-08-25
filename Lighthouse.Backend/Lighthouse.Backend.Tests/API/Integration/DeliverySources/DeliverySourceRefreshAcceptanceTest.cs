using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.DeliverySources
{
    /// <summary>
    /// The harness for the scenarios driven through the scheduled refresh rather than over HTTP.
    /// Everything the refresh does after fetching Features is the shipped code; the fetch itself is
    /// faked, and that is the one difference from the HTTP-driven fixtures.
    /// </summary>
    public abstract class DeliverySourceRefreshAcceptanceTest : DeliverySourcesAcceptanceTest
    {
        /// <summary>
        /// The refresh fetches Features before it syncs the sources, and that fetch is not this Epic's.
        /// Against a connector double it returns nothing and empties the Portfolio, which would leave
        /// the source pass narrowing a source's work to a Portfolio that tracks none of it - and every
        /// scenario passing for the wrong reason. Faked whole, the Portfolio keeps exactly the Features
        /// the scenario seeded, so what a Delivery ends up saying is the source pass's doing.
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

        /// <summary>
        /// A source that cannot be read, or has gone away, is a state to report on the Delivery - not a
        /// refresh that failed. The refresh carries every other number on the Portfolio, so reporting it
        /// as failed would send an operator looking for a fault that is not there and lose the rest.
        /// </summary>
        protected void ThenTheRefreshWasRecordedAsHavingWorked(int portfolioId)
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

        protected static void ThenNothingAboutTheDeliveryMoved(DeliveryRow before, DeliveryRow after)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(after.Name, Is.EqualTo(before.Name));
                Assert.That(after.Date, Is.EqualTo(before.Date));
                Assert.That(after.Features, Is.EqualTo(before.Features));
                Assert.That(after.SourceUnavailableReason, Is.EqualTo(before.SourceUnavailableReason));
                Assert.That(after.ConcurrencyToken, Is.EqualTo(before.ConcurrencyToken),
                    "a refresh that learned nothing must not expire the version an open browser is holding.");
            }
        }
    }
}
