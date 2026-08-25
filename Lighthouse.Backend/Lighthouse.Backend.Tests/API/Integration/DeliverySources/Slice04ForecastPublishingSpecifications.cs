using System.Net;
using System.Net.Http.Json;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliverySources;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.DeliverySources
{
    /// <summary>
    /// Step definitions for publishing a forecast onto a Release. The Delivery holds one Feature whose
    /// work is finished, which is the shape that carries a forecast without a throughput history to
    /// arrange: a Delivery with nothing left to do reads as certain on every percentile. What is being
    /// specified here is which Deliveries reach Jira and what arrives when they do - the arithmetic
    /// behind the numbers has its own fixtures.
    /// </summary>
    public partial class Slice04ForecastPublishingTest : DeliverySourceRefreshAcceptanceTest
    {
        protected const string TheRelease = "10007";

        private const string TheReleaseName = "Release 3.0";
        private const string TheWorkTheReleaseCarries = "LGH-1";
        private const string ANameSomebodyTyped = "Cut by hand";

        private static readonly DateTime TheReleaseDate = new(2027, 3, 18, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime ADateSomebodyPicked = new(2027, 9, 30, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime TheDateTheReleaseHasNow = new(2027, 6, 24, 0, 0, 0, DateTimeKind.Utc);

        private const string WhatJiraSaid = "You must have global or project administrator rights in order to modify versions.";

        private readonly List<DeliveryForecastPublication> published = [];

        /// <summary>
        /// NUnit builds one fixture and runs every test in it on that same instance, so what reached
        /// Jira in the scenario before is still here. Left standing, every "nothing was written"
        /// assertion would fail on somebody else's publication.
        /// </summary>
        [SetUp]
        public void ForgetWhatReachedJiraInTheScenarioBefore() => published.Clear();

        // --- Given ---

        private async Task<int> GivenADeliveryBroadcastingToTheRelease(string prefix)
            => await GivenADeliveryFollowingTheRelease(prefix, broadcasting: true);

        private async Task<int> GivenADeliveryFollowingTheReleaseQuietly(string prefix)
            => await GivenADeliveryFollowingTheRelease(prefix, broadcasting: false);

        private async Task<int> GivenADeliveryFollowingTheRelease(string prefix, bool broadcasting)
        {
            var portfolioId = GivenAJiraPortfolioWhoseReleaseCarriesFinishedWork();

            var created = await PostTheDelivery(prefix, portfolioId, new UpdateDeliveryRequest
            {
                Name = TheReleaseName,
                Date = TheReleaseDate,
                FeatureIds = [],
                SelectionMode = DeliverySelectionMode.SourceBound,
                SourceKey = JiraReleaseSourceKey,
                SourceReference = TheRelease,
                PublishForecastToSource = broadcasting,
            });

            Assert.That(created.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            return portfolioId;
        }

        private async Task<int> GivenADeliveryChosenByHandThatAsksToBroadcast(string prefix)
        {
            var portfolioId = GivenAJiraPortfolioWhoseReleaseCarriesFinishedWork();

            var created = await PostTheDelivery(prefix, portfolioId, new UpdateDeliveryRequest
            {
                Name = ANameSomebodyTyped,
                Date = ADateSomebodyPicked,
                FeatureIds = [],
                SelectionMode = DeliverySelectionMode.Manual,
                PublishForecastToSource = true,
            });

            Assert.That(created.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            return portfolioId;
        }

        private int GivenAJiraPortfolioWhoseReleaseCarriesFinishedWork()
        {
            var portfolioId = SeedPortfolioOn(WorkTrackingSystems.Jira);

            TheJiraConnectionOffersItsReleases();
            SeedFinishedFeature(portfolioId, TheWorkTheReleaseCarries, "Ship the thing");

            TheRemoteSays(TheRelease, new DeliverySourceResolution.Resolved(
                new DeliverySourceSnapshot(TheReleaseName, TheReleaseDate, [TheWorkTheReleaseCarries])));

            GivenJiraTakesWhateverItIsSent();

            return portfolioId;
        }

        /// <summary>
        /// A Feature with nothing left to do. It carries a forecast without a throughput history behind
        /// it, because a Delivery whose work is finished reads as certain rather than as unforecastable -
        /// which is what lets these scenarios be about the publishing rather than about the simulation.
        /// </summary>
        private void SeedFinishedFeature(int portfolioId, string referenceId, string name)
        {
            using var scope = Factory.Services.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            var portfolio = serviceProvider.GetRequiredService<IRepository<Portfolio>>().GetById(portfolioId)!;

            var team = new Team
            {
                Name = $"Team {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = portfolio.WorkTrackingSystemConnection,
                DoneItemsCutoffDays = 365,
                DataRetrievalValue = "project = TEST",
                WorkItemTypes = ["Story"],
                ToDoStates = ["New"],
                DoingStates = ["In Progress"],
                DoneStates = ["Done"],
                UpdateTime = DateTime.UtcNow,
            };

            var teamRepository = serviceProvider.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(team);
            teamRepository.Save().GetAwaiter().GetResult();

            var feature = new Feature([(team, 0, 5)])
            {
                Name = name,
                ReferenceId = referenceId,
                Type = "Epic",
                State = "Done",
                StateCategory = StateCategories.Done,
                Order = "1",
            };
            feature.Portfolios.Add(portfolio);

            var featureRepository = serviceProvider.GetRequiredService<IRepository<Feature>>();
            featureRepository.Add(feature);
            featureRepository.Save().GetAwaiter().GetResult();
        }

        private void GivenJiraTakesWhateverItIsSent()
        {
            JiraConnector
                .Setup(connector => connector.SupportsDeliveryForecastPublishing(It.IsAny<WorkTrackingSystemConnection>()))
                .Returns(true);
            JiraConnector
                .Setup(connector => connector.PublishAsync(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<DeliveryForecastPublication>()))
                .Callback<WorkTrackingSystemConnection, DeliveryForecastPublication>((_, publication) => published.Add(publication))
                .ReturnsAsync(new DeliveryForecastPublishResult.Published());
        }

        private void GivenJiraRefusesToBeWrittenTo()
        {
            // Still recorded as having reached the port. A refusal is Jira answering, so the attempt was
            // made - which is what lets a scenario say the next round tried again rather than giving up.
            JiraConnector
                .Setup(connector => connector.PublishAsync(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<DeliveryForecastPublication>()))
                .Callback<WorkTrackingSystemConnection, DeliveryForecastPublication>((_, publication) => published.Add(publication))
                .ReturnsAsync(new DeliveryForecastPublishResult.Refused(WhatJiraSaid));
        }

        private void GivenTheReleaseHasBeenRescheduledInJira()
        {
            TheRemoteSays(TheRelease, new DeliverySourceResolution.Resolved(
                new DeliverySourceSnapshot(TheReleaseName, TheDateTheReleaseHasNow, [TheWorkTheReleaseCarries])));
        }

        private void GivenNothingHasReachedJiraYet() => published.Clear();

        private void GivenTheReleaseIsNoLongerThereToBeWrittenTo()
        {
            JiraConnector
                .Setup(connector => connector.PublishAsync(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<DeliveryForecastPublication>()))
                .ReturnsAsync(new DeliveryForecastPublishResult.TargetMissing());
        }

        private async Task GivenTheDeliveryHasBeenRetired(int deliveryId)
        {
            var retired = await Client.PostAsJsonAsync(
                $"/{ApiLatestPrefix}/deliveries/{deliveryId}/archive", new ArchiveDeliveryRequest());

            Assert.That(retired.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        // --- When ---

        private Task<HttpResponseMessage> WhenTheDeliveryIsTakenBackByHand(string prefix, int deliveryId)
            => PutTheDelivery(prefix, deliveryId, new UpdateDeliveryRequest
            {
                Name = TheReleaseName,
                Date = TheReleaseDate,
                FeatureIds = [],
                SelectionMode = DeliverySelectionMode.Manual,
            });

        /// <summary>
        /// The whole Delivery, sent back as it stands, by something written before the switch existed -
        /// the shape a command-line client or a hand-built request has.
        /// </summary>
        private Task<HttpResponseMessage> WhenTheDeliveryIsSavedByAClientThatKnowsNothingAboutBroadcasting(
            string prefix, int deliveryId)
            => PutTheDelivery(prefix, deliveryId, new UpdateDeliveryRequest
            {
                Name = TheReleaseName,
                Date = TheReleaseDate,
                FeatureIds = [],
                SelectionMode = DeliverySelectionMode.SourceBound,
                SourceKey = JiraReleaseSourceKey,
                SourceReference = TheRelease,
            });

        // --- Then ---

        private static void ThenTheAnswerIs(HttpResponseMessage response, HttpStatusCode expected)
            => Assert.That(response.StatusCode, Is.EqualTo(expected));

        private static void ThenTheDeliverySaysItBroadcasts(DeliveryRow delivery)
            => Assert.That(delivery.PublishForecastToSource, Is.True);

        private static void ThenTheDeliverySaysItDoesNotBroadcast(DeliveryRow delivery)
            => Assert.That(delivery.PublishForecastToSource, Is.False);

        private static void ThenTheDeliveryStillFollowsALiveRelease(DeliveryRow delivery)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery.SourceUnavailableReason, Is.Null,
                    "a credential that may not write says nothing about whether the Release is still there.");
                Assert.That(delivery.PublishForecastToSource, Is.True,
                    "a refused write does not switch the broadcast off - the administrator grants the permission and the next round goes through.");
            }
        }

        private static void ThenTheDeliveryReportsTheRefusal(DeliveryRow delivery)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery.LastPublishRefusalReason, Is.EqualTo(WhatJiraSaid),
                    "Jira's own sentence names what to fix in the words the administrator will search for.");
                Assert.That(delivery.LastPublishRefusedOn, Is.Not.Null);
            }
        }

        private static void ThenTheDeliveryReportsNoRefusal(DeliveryRow delivery)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery.LastPublishRefusalReason, Is.Null);
                Assert.That(delivery.LastPublishRefusedOn, Is.Null);
            }
        }

        private static void ThenTheDeliveryTookTheNewDateAnyway(DeliveryRow delivery)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery.Date, Is.EqualTo(TheDateTheReleaseHasNow),
                    "reading a Release and writing to it are separate capabilities; a refused write may not stop the date sync.");
                Assert.That(delivery.SourceUnavailableReason, Is.Null);
            }
        }

        private static void ThenTheDeliverySaysItsReleaseIsGone(DeliveryRow delivery)
            => Assert.That(delivery.SourceUnavailableReason, Is.EqualTo(DeliverySourceUnavailableReason.SourceNotFound));

        private void ThenTheReleaseWasWrittenTo(string sourceReference)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(published.ConvertAll(publication => publication.SourceReference), Does.Contain(sourceReference));
                Assert.That(published.TrueForAll(publication => publication.SourceKey == JiraReleaseSourceKey), Is.True);
            }
        }

        private void ThenNoReleaseWasWrittenTo()
            => Assert.That(published, Is.Empty);

        /// <summary>
        /// The four things the block has to carry, asserted on the text that actually crossed the port
        /// rather than on the record it was rendered from. Anything less would pass with the rendering
        /// step wired to nothing.
        /// </summary>
        private void ThenWhatReachedJiraCarriesEverythingTheBlockMustSay()
        {
            var block = published[0].BlockText;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(block, Does.Contain("Lighthouse forecast"), "whoever reads it has to be able to tell who wrote it.");
                Assert.That(block, Does.Contain("updated "), "and how old it is.");
                Assert.That(block, Does.Contain("70%: "));
                Assert.That(block, Does.Contain("85%: "));
                Assert.That(block, Does.Contain("95%: "));
                Assert.That(block, Does.Contain($"Target {TheReleaseDate:yyyy-MM-dd}: "), "a likelihood with no date attached is a number about nothing.");
            }
        }
    }
}
