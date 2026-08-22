using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    /// <summary>
    /// A rule enforced at one entrance has as many holes as there are other entrances, so the refusal
    /// is asserted over HTTP at every one of them. What archiving must NOT block is deleting and
    /// re-opening: the first because closing a Delivery was never meant to make it permanent, the
    /// second because closing it was always meant to be undoable.
    /// </summary>
    [TestFixture]
    public class ArchivedDeliveryWriteRefusalIntegrationTest
    {
        private const string ArchivedCode = "delivery-archived";
        private const string NameAtClosure = "Q3 Launch";

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private Mock<ILicenseService> licenseServiceMock = null!;

        [SetUp]
        public void Init()
        {
            rootFactory = new TestWebApplicationFactory<Program>();

            licenseServiceMock = new Mock<ILicenseService>();
            licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory)
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<ILicenseService>();
                        services.AddScoped(_ => licenseServiceMock.Object);
                    });
                });

            client = factory.CreateClient();

            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
        }

        [TearDown]
        public void Cleanup()
        {
            using (var scope = factory.Services.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.EnsureDeleted();
            }

            client.Dispose();
            factory.Dispose();
            rootFactory.Dispose();
        }

        [Test]
        public async Task Rename_ArchivedDelivery_IsRefusedAsAConflictAndLeavesItAsItWas()
        {
            var seeded = await SeedArchivedDelivery();

            var response = await UpdateDelivery(seeded, "Renamed After Closing", Today.AddDays(90));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict), await response.Content.ReadAsStringAsync());
                Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain(ArchivedCode));
                Assert.That(await NameOf(seeded.DeliveryId), Is.EqualTo(NameAtClosure));
            }
        }

        [Test]
        public async Task ChangeToRuleBased_ArchivedDelivery_IsRefusedAndLeavesItPickingByHand()
        {
            var seeded = await SeedArchivedDelivery();

            var response = await client.PutAsync(
                $"/api/latest/deliveries/{seeded.DeliveryId}",
                JsonContent.Create(new UpdateDeliveryRequest
                {
                    Name = NameAtClosure,
                    Date = Today.AddDays(30),
                    FeatureIds = [],
                    SelectionMode = DeliverySelectionMode.RuleBased,
                    Mode = WorkItemRuleSet.ModeAnd,
                    Rules = [new WorkItemRuleCondition { FieldKey = "name", Operator = RuleOperators.Contains, Value = "anything" }],
                }));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict), await response.Content.ReadAsStringAsync());
                Assert.That(await SelectionModeOf(seeded.DeliveryId), Is.EqualTo(DeliverySelectionMode.Manual));
            }
        }

        [Test]
        public async Task ChangeFeatures_ArchivedDelivery_IsRefusedAndLeavesTheFeaturesItClosedWith()
        {
            var seeded = await SeedArchivedDelivery();
            var featuresAtClosure = await FeatureCountOf(seeded.DeliveryId);

            var response = await UpdateDelivery(seeded, NameAtClosure, Today.AddDays(30), featureIds: []);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict), await response.Content.ReadAsStringAsync());
                Assert.That(await FeatureCountOf(seeded.DeliveryId), Is.EqualTo(featuresAtClosure));
            }
        }

        [Test]
        public async Task AddNote_ArchivedDelivery_IsRefusedAndStoresNothing()
        {
            var seeded = await SeedArchivedDelivery();
            var notesBefore = await NoteCountOf(seeded.DeliveryId);

            var response = await AddNote(seeded.DeliveryId, "written after closing");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict), await response.Content.ReadAsStringAsync());
                Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain(ArchivedCode));
                Assert.That(await NoteCountOf(seeded.DeliveryId), Is.EqualTo(notesBefore));
            }
        }

        [Test]
        public async Task CorrectNote_ArchivedDelivery_IsRefusedAndLeavesTheNoteAsItReads()
        {
            var seeded = await SeedArchivedDelivery();

            var response = await client.PutAsync(
                $"/api/latest/deliveries/{seeded.DeliveryId}/notes/{seeded.NoteId}",
                JsonContent.Create(new DeliveryNoteRequest { Text = "corrected after closing" }));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict), await response.Content.ReadAsStringAsync());
                Assert.That(await NoteTextOf(seeded.NoteId), Is.EqualTo("Vendor slipped a week"));
            }
        }

        [Test]
        public async Task WithdrawNote_ArchivedDelivery_IsRefusedAndTheNoteStays()
        {
            var seeded = await SeedArchivedDelivery();

            var response = await client.DeleteAsync($"/api/latest/deliveries/{seeded.DeliveryId}/notes/{seeded.NoteId}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict), await response.Content.ReadAsStringAsync());
                Assert.That(await NoteCountOf(seeded.DeliveryId), Is.EqualTo(1));
            }
        }

        [Test]
        public async Task ReadNotes_ArchivedDelivery_StillListsWhatWasWrittenBeforeItClosed()
        {
            var seeded = await SeedArchivedDelivery();

            var response = await client.GetAsync($"/api/latest/deliveries/{seeded.DeliveryId}/notes");
            var body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(JsonNode.Parse(body)!.AsArray(), Has.Count.EqualTo(1), body);
            }
        }

        [Test]
        public async Task BeingRefusedBecauseItIsClosed_ReadsDifferentlyFromBeingRefusedForLackOfRights()
        {
            var seeded = await SeedArchivedDelivery();

            var refusedBecauseClosed = await AddNote(seeded.DeliveryId, "written after closing");

            client.AsPortfolioViewer(seeded.PortfolioId);
            var refusedForLackOfRights = await AddNote(seeded.DeliveryId, "written by a reader");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refusedBecauseClosed.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
                Assert.That(refusedForLackOfRights.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            }
        }

        [Test]
        public async Task Unarchive_IsNotRefusedOnTheGroundsThatTheDeliveryIsClosed()
        {
            var seeded = await SeedArchivedDelivery();

            var response = await Unarchive(seeded.DeliveryId);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
        }

        [Test]
        public async Task Delete_IsNotRefusedOnTheGroundsThatTheDeliveryIsClosed()
        {
            var seeded = await SeedArchivedDelivery();

            var response = await client.DeleteAsync($"/api/latest/deliveries/{seeded.DeliveryId}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent), await response.Content.ReadAsStringAsync());
        }

        [Test]
        public async Task TheSameChangeThatWasRefused_SucceedsOnceTheDeliveryIsBroughtBack()
        {
            var seeded = await SeedArchivedDelivery();
            Assert.That((await UpdateDelivery(seeded, "Renamed", Today.AddDays(90))).StatusCode, Is.EqualTo(HttpStatusCode.Conflict));

            await EnsureOk(Unarchive(seeded.DeliveryId));

            using (Assert.EnterMultipleScope())
            {
                Assert.That((await UpdateDelivery(seeded, "Renamed", Today.AddDays(90))).StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That((await AddNote(seeded.DeliveryId, "written after it came back")).StatusCode, Is.EqualTo(HttpStatusCode.OK));
            }
        }

        private sealed record SeededDelivery(int PortfolioId, int DeliveryId, int NoteId, List<int> FeatureIds);

        private async Task<SeededDelivery> SeedArchivedDelivery()
        {
            var portfolioId = await SeedPortfolioWithOneForecastableFeature();
            var featureIds = await FeatureIds();

            client.AsPortfolioAdmin(portfolioId);

            var create = await client.PostAsync(
                $"/api/latest/deliveries/portfolio/{portfolioId}",
                JsonContent.Create(new UpdateDeliveryRequest
                {
                    Name = NameAtClosure,
                    Date = Today.AddDays(30),
                    FeatureIds = featureIds,
                    SelectionMode = DeliverySelectionMode.Manual,
                }));
            create.EnsureSuccessStatusCode();

            int deliveryId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                deliveryId = await context.Deliveries.Select(delivery => delivery.Id).SingleAsync();
            }

            await EnsureOk(AddNote(deliveryId, "Vendor slipped a week"));
            var noteId = await OnlyNoteIdOf(deliveryId);

            await EnsureOk(client.PostAsync(
                $"/api/latest/deliveries/{deliveryId}/archive",
                JsonContent.Create(new ArchiveDeliveryRequest())));

            return new SeededDelivery(portfolioId, deliveryId, noteId, featureIds);
        }

        private Task<HttpResponseMessage> UpdateDelivery(SeededDelivery seeded, string name, DateTime date, List<int>? featureIds = null)
        {
            return client.PutAsync(
                $"/api/latest/deliveries/{seeded.DeliveryId}",
                JsonContent.Create(new UpdateDeliveryRequest
                {
                    Name = name,
                    Date = date,
                    FeatureIds = featureIds ?? seeded.FeatureIds,
                    SelectionMode = DeliverySelectionMode.Manual,
                }));
        }

        private Task<HttpResponseMessage> AddNote(int deliveryId, string text)
        {
            return client.PostAsync(
                $"/api/latest/deliveries/{deliveryId}/notes",
                JsonContent.Create(new DeliveryNoteRequest { Text = text }));
        }

        private Task<HttpResponseMessage> Unarchive(int deliveryId)
        {
            return client.PostAsync(
                $"/api/latest/deliveries/{deliveryId}/unarchive",
                JsonContent.Create(new ArchiveDeliveryRequest()));
        }

        private static async Task EnsureOk(Task<HttpResponseMessage> call)
        {
            var response = await call;
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
        }

        private async Task<int> SeedPortfolioWithOneForecastableFeature()
        {
            using var scope = factory.Services.CreateScope();
            var provider = scope.ServiceProvider;

            var connection = new WorkTrackingSystemConnection { Name = "Connection", WorkTrackingSystem = WorkTrackingSystems.Jira };
            var team = new Team { Name = "Team Alpha", WorkTrackingSystemConnection = connection };

            var teamRepository = provider.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(team);
            await teamRepository.Save();

            var feature = new Feature([(team, 3, 10)]) { Name = "Checkout", ReferenceId = "FTR-1", Order = "11" };
            var simulation = new SimulationResult();
            simulation.SimulationResults[10] = 9000;
            simulation.SimulationResults[20] = 1000;
            feature.SetFeatureForecasts([new WhenForecast(simulation) { HasSufficientData = true, TeamId = team.Id }]);

            var portfolio = new Portfolio { Name = "Test Portfolio", WorkTrackingSystemConnection = connection };
            portfolio.UpdateFeatures([feature]);

            var portfolioRepository = provider.GetRequiredService<IRepository<Portfolio>>();
            portfolioRepository.Add(portfolio);
            await portfolioRepository.Save();

            return portfolio.Id;
        }

        private DateTime Today
        {
            get
            {
                using var scope = factory.Services.CreateScope();
                return scope.ServiceProvider.GetRequiredService<ILighthouseClock>().TodayAsUtcMidnight;
            }
        }

        private async Task<List<int>> FeatureIds()
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            return await context.Features.Select(feature => feature.Id).ToListAsync();
        }

        private async Task<string> NameOf(int deliveryId)
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            return await context.Deliveries.Where(d => d.Id == deliveryId).Select(d => d.Name).SingleAsync();
        }

        private async Task<DeliverySelectionMode> SelectionModeOf(int deliveryId)
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            return await context.Deliveries.Where(d => d.Id == deliveryId).Select(d => d.SelectionMode).SingleAsync();
        }

        private async Task<int> FeatureCountOf(int deliveryId)
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var delivery = await context.Deliveries.Include(d => d.Features).SingleAsync(d => d.Id == deliveryId);
            return delivery.Features.Count;
        }

        private async Task<int> NoteCountOf(int deliveryId)
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            return await context.DeliveryNotes.CountAsync(note => note.DeliveryId == deliveryId);
        }

        private async Task<int> OnlyNoteIdOf(int deliveryId)
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            return await context.DeliveryNotes.Where(note => note.DeliveryId == deliveryId).Select(note => note.Id).SingleAsync();
        }

        private async Task<string> NoteTextOf(int noteId)
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            return await context.DeliveryNotes.Where(note => note.Id == noteId).Select(note => note.Text).SingleAsync();
        }
    }
}
