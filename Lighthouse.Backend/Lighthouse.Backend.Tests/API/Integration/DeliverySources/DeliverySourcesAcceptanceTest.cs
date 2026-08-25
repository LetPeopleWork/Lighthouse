using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliverySources;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.DeliverySources
{
    /// <summary>
    /// Acceptance harness for the delivery-source routes. Scenarios reach them the way the tab does:
    /// over HTTP through the real ASP.NET host, so routing, both version prefixes, the RBAC guard, the
    /// licence guard and the real source resolver all take part. A scenario that called the controller
    /// method directly would say nothing about any of them, and every one of them can refuse a request
    /// on its own.
    ///
    /// Real: EF over SQLite, the routing and authorization pipeline, the delivery source resolver, and
    /// the connectors of every tracker other than Jira. Faked: the Jira connector and the licence.
    /// </summary>
    public abstract class DeliverySourcesAcceptanceTest
    {
        protected const string ApiV1Prefix = "api/v1";
        protected const string ApiLatestPrefix = "api/latest";

        protected const string JiraReleaseSourceKey = "jira-release";
        protected const string JiraReleaseSourceDisplayName = "Jira Release";

        private const string DeliverySourcesSegment = "delivery-sources";
        private const string DeliveriesSegment = "deliveries";

        /// <summary>
        /// Web defaults plus the string-enum reading the host writes with. Held in a field because a
        /// fresh options object per call rebuilds the serializer's metadata cache every time.
        /// </summary>
        private static readonly JsonSerializerOptions WireOptions = BuildWireOptions();

        protected TestWebApplicationFactory<Program> RootFactory = null!;
        protected WebApplicationFactory<Program> Factory = null!;
        protected HttpClient Client = null!;

        protected Mock<ILicenseService> LicenseServiceMock = null!;

        /// <summary>
        /// The Jira connector, faked at the interface that actually carries the delivery-source
        /// capability. It must stay <see cref="IJiraWorkTrackingConnector"/> and never be narrowed to
        /// the base work-tracking connector: the controller decides whether a connection can offer
        /// sources by testing the resolved connector for <see cref="IDeliverySourceProvider"/>, so a
        /// base-connector double would hand back nothing, every Jira scenario would quietly take the
        /// "this tracker offers no sources" path, and they would all still pass.
        /// </summary>
        protected Mock<IJiraWorkTrackingConnector> JiraConnector = null!;

        [SetUp]
        public void Init()
        {
            RootFactory = new TestWebApplicationFactory<Program>();

            LicenseServiceMock = new Mock<ILicenseService>();
            LicenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            JiraConnector = new Mock<IJiraWorkTrackingConnector>();

            Factory = TestWebApplicationFactory<Program>.WithTestAuthentication(RootFactory)
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<ILicenseService>();
                        services.AddScoped(_ => LicenseServiceMock.Object);

                        // Only Jira is swapped. The real connector factory still does the resolving, and
                        // the other four trackers keep their production connectors - which is what makes
                        // "this tracker offers no delivery sources" a statement about the shipped code
                        // rather than about a double written here.
                        services.RemoveAll<IJiraWorkTrackingConnector>();
                        services.AddScoped(_ => JiraConnector.Object);

                        AlsoSwap(services);
                    });
                });

            Client = Factory.CreateClient().AsSystemAdmin();

            using var setupScope = Factory.Services.CreateScope();
            var dbContext = setupScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            foreach (var seeder in setupScope.ServiceProvider.GetServices<ISeeder>())
            {
                seeder.Seed().GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// Anything a single fixture needs swapped on top of the Jira connector and the licence.
        /// Scenarios driven over HTTP need nothing here; the ones driven through the scheduled refresh
        /// do, because that refresh starts by fetching Features and this Epic does not own the fetch.
        /// </summary>
        protected virtual void AlsoSwap(IServiceCollection services)
        {
        }

        [TearDown]
        public void Cleanup()
        {
            using (var teardownScope = Factory.Services.CreateScope())
            {
                teardownScope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.EnsureDeleted();
            }

            Client.Dispose();
            Factory.Dispose();
            RootFactory.Dispose();
        }

        // --- Seeding ---

        protected int SeedPortfolioOn(WorkTrackingSystems system)
        {
            using var scope = Factory.Services.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            var connection = new WorkTrackingSystemConnection
            {
                Name = $"Connection {Guid.NewGuid():N}",
                WorkTrackingSystem = system,
            };

            var connectionRepository = serviceProvider.GetRequiredService<IRepository<WorkTrackingSystemConnection>>();
            connectionRepository.Add(connection);
            connectionRepository.Save().GetAwaiter().GetResult();

            var portfolio = new Portfolio
            {
                Name = $"Portfolio {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = connection,
                DoneItemsCutoffDays = 365,
                DataRetrievalValue = "project = TEST",
                WorkItemTypes = ["Epic"],
                ToDoStates = ["New"],
                DoingStates = ["In Progress"],
                DoneStates = ["Done"],
                UpdateTime = DateTime.UtcNow,
            };

            var portfolioRepository = serviceProvider.GetRequiredService<IRepository<Portfolio>>();
            portfolioRepository.Add(portfolio);
            portfolioRepository.Save().GetAwaiter().GetResult();

            return portfolio.Id;
        }

        /// <summary>
        /// A Feature this Portfolio tracks. A Feature's size is the sum of the work its Team carries, so
        /// a Team has to exist for the row to look like anything the grid would render.
        /// </summary>
        protected int SeedTrackedFeature(int portfolioId, string referenceId, string name)
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

            var feature = new Feature(team, 5)
            {
                Name = name,
                ReferenceId = referenceId,
                Type = "Epic",
                State = "In Progress",
                StateCategory = StateCategories.Doing,
                Order = "1",
            };
            feature.Portfolios.Add(portfolio);

            var featureRepository = serviceProvider.GetRequiredService<IRepository<Feature>>();
            featureRepository.Add(feature);
            featureRepository.Save().GetAwaiter().GetResult();

            return feature.Id;
        }

        // --- What the faked Jira connection offers ---

        protected void TheJiraConnectionOffersItsReleases()
        {
            JiraConnector
                .Setup(c => c.AvailableSources(It.IsAny<WorkTrackingSystemConnection>()))
                .Returns(new List<DeliverySourceDescriptor>
                {
                    new(JiraReleaseSourceKey, JiraReleaseSourceDisplayName),
                });
        }

        protected void TheJiraConnectionOffersNothing()
        {
            JiraConnector
                .Setup(c => c.AvailableSources(It.IsAny<WorkTrackingSystemConnection>()))
                .Returns(new List<DeliverySourceDescriptor>());
        }

        protected void TheReleasePickerOffers(params DeliverySourceOption[] options)
        {
            JiraConnector
                .Setup(c => c.GetOptions(It.IsAny<WorkTrackingSystemConnection>(), JiraReleaseSourceKey))
                .ReturnsAsync(options);
        }

        protected void TheRemoteSays(string sourceReference, DeliverySourceResolution resolution)
        {
            JiraConnector
                .Setup(c => c.ResolveMany(It.IsAny<WorkTrackingSystemConnection>(), JiraReleaseSourceKey, It.IsAny<IReadOnlyList<string>>()))
                .ReturnsAsync(new Dictionary<string, DeliverySourceResolution> { [sourceReference] = resolution });
        }

        protected void TheRemoteSays(Dictionary<string, DeliverySourceResolution> byReference)
        {
            JiraConnector
                .Setup(c => c.ResolveMany(It.IsAny<WorkTrackingSystemConnection>(), JiraReleaseSourceKey, It.IsAny<IReadOnlyList<string>>()))
                .ReturnsAsync(byReference);
        }

        /// <summary>
        /// A connection that has stopped offering the source throws out of the read rather than
        /// answering it, which is what the Jira adapter raises when it is asked for a source key it
        /// does not know. It is the one failure that cannot be expressed as a resolution.
        /// </summary>
        protected void TheRemoteCannotBeAskedAtAll()
        {
            JiraConnector
                .Setup(c => c.ResolveMany(It.IsAny<WorkTrackingSystemConnection>(), JiraReleaseSourceKey, It.IsAny<IReadOnlyList<string>>()))
                .ThrowsAsync(new ArgumentException(
                    $"This Jira connection does not offer a delivery source called '{JiraReleaseSourceKey}'."));
        }

        // --- Driving port: the scheduled refresh ---

        /// <summary>
        /// Triggers one Portfolio refresh and waits for the queue to go idle. Admission happens inside
        /// the trigger call, so the key is already active before the polling starts and "not enqueued
        /// yet" cannot be mistaken for "already finished".
        /// </summary>
        protected async Task ThePortfolioRefreshRuns(int portfolioId)
        {
            var statusStore = Factory.Services.GetRequiredService<IUpdateStatusStore>();

            Factory.Services.GetRequiredService<IPortfolioUpdater>().TriggerUpdate(portfolioId);

            var deadline = DateTime.UtcNow.AddSeconds(30);
            while (statusStore.HasActiveWork())
            {
                if (DateTime.UtcNow > deadline)
                {
                    Assert.Fail("The update queue did not go idle within 30s - the refresh never completed.");
                }

                await Task.Delay(20);
            }
        }

        protected void TheInstanceIsNotLicensedForPremium()
        {
            LicenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(false);
        }

        // --- Driving port: HTTP ---

        protected static string DeliveriesOfPortfolioRoute(string prefix, int portfolioId)
            => $"/{prefix}/{DeliveriesSegment}/portfolio/{portfolioId}";

        protected static string DeliveryRoute(string prefix, int deliveryId)
            => $"/{prefix}/{DeliveriesSegment}/{deliveryId}";

        protected Task<HttpResponseMessage> PostTheDelivery(string prefix, int portfolioId, UpdateDeliveryRequest request)
            => Client.PostAsJsonAsync(DeliveriesOfPortfolioRoute(prefix, portfolioId), request);

        protected Task<HttpResponseMessage> PutTheDelivery(string prefix, int deliveryId, UpdateDeliveryRequest request)
            => Client.PutAsJsonAsync(DeliveryRoute(prefix, deliveryId), request);

        protected Task<HttpResponseMessage> GetTheDeliveriesOfPortfolio(string prefix, int portfolioId)
            => Client.GetAsync(DeliveriesOfPortfolioRoute(prefix, portfolioId));

        protected static async Task<PortfolioDeliveriesBody> DeliveriesIn(HttpResponseMessage response)
            => await ReadAs<PortfolioDeliveriesBody>(response);

        /// <summary>
        /// The Delivery as the grid sees it. The count is asserted here rather than left to each
        /// scenario, so a second Delivery appearing from somewhere fails on the spot instead of
        /// quietly becoming whichever one happens to be first.
        /// </summary>
        protected async Task<DeliveryRow> TheOnlyDeliveryOf(string prefix, int portfolioId)
        {
            var response = await GetTheDeliveriesOfPortfolio(prefix, portfolioId);
            var body = await DeliveriesIn(response);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(body.Active, Has.Count.EqualTo(1));
            }

            return body.Active[0];
        }

        protected static string SourcesRoute(string prefix, int portfolioId)
            => $"/{prefix}/portfolios/{portfolioId}/{DeliverySourcesSegment}";

        protected static string OptionsRoute(string prefix, int portfolioId, string sourceKey)
            => $"{SourcesRoute(prefix, portfolioId)}/{sourceKey}/options";

        protected static string PreviewRoute(string prefix, int portfolioId, string sourceKey)
            => $"{SourcesRoute(prefix, portfolioId)}/{sourceKey}/preview";

        protected Task<HttpResponseMessage> GetTheDeliverySources(string prefix, int portfolioId)
            => Client.GetAsync(SourcesRoute(prefix, portfolioId));

        protected Task<HttpResponseMessage> GetTheOptions(string prefix, int portfolioId, string sourceKey)
            => Client.GetAsync(OptionsRoute(prefix, portfolioId, sourceKey));

        protected Task<HttpResponseMessage> PostThePreview(string prefix, int portfolioId, string sourceKey, string sourceReference)
            => Client.PostAsJsonAsync(
                PreviewRoute(prefix, portfolioId, sourceKey),
                new PreviewDeliverySourceRequest { SourceReference = sourceReference });

        // --- Reading the wire ---

        protected static async Task<List<DeliverySourceResponse>> SourcesIn(HttpResponseMessage response)
            => await ReadAs<List<DeliverySourceResponse>>(response);

        protected static async Task<List<DeliverySourceOptionResponse>> OptionsIn(HttpResponseMessage response)
            => await ReadAs<List<DeliverySourceOptionResponse>>(response);

        protected static async Task<DeliverySourcePreviewResponse> PreviewIn(HttpResponseMessage response)
            => await ReadAs<DeliverySourcePreviewResponse>(response);

        private static async Task<T> ReadAs<T>(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<T>(body, WireOptions)
                ?? throw new InvalidOperationException($"The response carried no body to read: {body}");
        }

        private static JsonSerializerOptions BuildWireOptions()
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            options.Converters.Add(new JsonStringEnumConverter());

            return options;
        }

        protected sealed record DeliveryRow(
            int Id,
            string Name,
            DateTime Date,
            List<int> Features,
            DeliverySelectionMode SelectionMode,
            string? SourceKey,
            string? SourceReference,
            // The one field that tells a refresh which chose to write nothing apart from a refresh that
            // never asked. Without it every "nothing changed" scenario passes with the sync pass deleted.
            DateTime? SourceLastSyncedOn,
            Guid ConcurrencyToken);

        protected sealed record PortfolioDeliveriesBody(List<DeliveryRow> Active);

        protected sealed record DeliverySourceResponse(string Key, string DisplayName);

        protected sealed record DeliverySourceOptionResponse(
            string Id, string Name, DateTime? Date, string ProjectKey, string ProjectName,
            bool IsSelectable, SourceOptionBlockReason? BlockedBecause);

        protected sealed record PreviewFeatureResponse(string ReferenceId, string Name);

        protected sealed record DeliverySourcePreviewResponse(
            string Name, DateTime Date, List<PreviewFeatureResponse> Features, DeliverySourcePreviewEmptyReason EmptyBecause);
    }
}
