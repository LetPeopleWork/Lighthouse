using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models.Validation;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Lighthouse.Backend.Tests.API.Integration
{
    // Story #5574, US-01 — the walking skeleton for the ServiceNow connection.
    //
    // Everything the administrator's click actually traverses is real here: the HTTP endpoint,
    // the enum, both factories, the DI container, the ServiceNow connector, the auth strategy,
    // the HTTP client, the verdict ladder and the persisted connection. Nothing about ServiceNow
    // is faked. The instance is simply pointed at a closed local port, which is a real
    // unreachable host and needs no external system to be deterministic.
    //
    // Layer 5 (real stack): one representative example, traditional assertions.
    [TestFixture]
    [Category("epic-5513-servicenow")]
    public class ServiceNowConnectionAcceptanceTest
    {
        private const string UnreachableInstance = "http://127.0.0.1:1/";
        private const string Password = "the-platform-teams-password";

        private TestWebApplicationFactory<Program> rootFactory;
        private WebApplicationFactory<Program> factory;
        private HttpClient client;

        [SetUp]
        public void Init()
        {
            rootFactory = new TestWebApplicationFactory<Program>();

            var licenseService = new Mock<ILicenseService>();
            licenseService.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory)
                .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
                {
                    services.RemoveAll<ILicenseService>();
                    services.AddScoped(_ => licenseService.Object);
                }));

            client = factory.CreateClient();

            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
        }

        [TearDown]
        public void Cleanup()
        {
            using (var scope = factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
                dbContext.Database.EnsureDeleted();
            }

            client.Dispose();
            factory.Dispose();
            rootFactory.Dispose();
        }

        // AC1. Slice 01's whole promise starts here: a ServiceNow shop can find their system in
        // the list at all.
        [Test]
        public async Task AnAdministratorOpeningTheConnectionWizard_CanChooseServiceNow()
        {
            var offered = await GetConnectionsFrom("/api/latest/worktrackingsystemconnections/supported");

            Assert.That(offered, Is.Not.Null);
            Assert.That(
                offered.Select(system => system.WorkTrackingSystem),
                Contains.Item(WorkTrackingSystems.ServiceNow));
        }

        // AC2. The form is rendered from what this endpoint returns, so what it returns is the
        // form. No bespoke React screen exists or is wanted.
        [Test]
        public async Task TheServiceNowEntryInTheWizard_CarriesTheFieldsTheFormNeedsToRender()
        {
            var offered = await GetConnectionsFrom("/api/latest/worktrackingsystemconnections/supported");

            Assert.That(offered, Is.Not.Null);

            var serviceNow = offered.Single(system => system.WorkTrackingSystem == WorkTrackingSystems.ServiceNow);
            var method = serviceNow.AvailableAuthenticationMethods.Single();
            var fields = method.Options.Select(option => option.Key).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(serviceNow.AuthenticationMethodKey, Is.EqualTo(AuthenticationMethodKeys.ServiceNowBasic));
                Assert.That(fields, Contains.Item(ServiceNowWorkTrackingOptionNames.InstanceUrl));
                Assert.That(fields, Contains.Item(ServiceNowWorkTrackingOptionNames.Username));
                Assert.That(fields, Contains.Item(ServiceNowWorkTrackingOptionNames.Password));
            }
        }

        // The walking skeleton. AC4's first failure mode, driven the way the administrator drives
        // it, all the way through the production wiring.
        [Test]
        public async Task AnAdministratorValidatingAConnectionToAnInstanceThatIsNotThere_IsToldTheInstanceIsNotThere()
        {
            client.AsSystemAdmin();

            var response = await PostConnectionTo(
                "/api/latest/worktrackingsystemconnections/validate", NewServiceNowConnection());

            var verdict = await ReadVerdict(response);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(verdict.IsValid, Is.False);
                Assert.That(verdict.Code, Is.EqualTo("connection_failed"));
                Assert.That(verdict.Message, Is.Not.Empty);
            }
        }

        // AC5. The credential the platform team handed over never comes back out of Lighthouse.
        [Test]
        public async Task TheCredentialAnAdministratorEnters_IsNeverHandedBackToTheBrowser()
        {
            client.AsSystemAdmin();

            var created = await PostConnectionTo(
                "/api/latest/worktrackingsystemconnections", NewServiceNowConnection());

            Assert.That(created.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var reloaded = await GetConnectionsFrom("/api/latest/worktrackingsystemconnections");

            Assert.That(reloaded, Is.Not.Null);

            var connection = reloaded.Single();
            var password = connection.Options.Single(o => o.Key == ServiceNowWorkTrackingOptionNames.Password);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(password.IsSecret, Is.True);
                Assert.That(password.Value, Is.Not.EqualTo(Password),
                    "The password came back in plaintext. AC5 is the reason the option is marked secret.");
            }
        }

        // Story #5610 slice 02, AC-B1 / AC-B3 / DD-1 / DD-6, driven the way the administrator drives
        // it. Today ServiceNow falls through the wizard's switch to a NotImplementedException, so
        // asking a ServiceNow connection for its boards is a fault rather than an answer. It must be
        // an answer: the instance here is a closed local port, which is a real unreachable host, and
        // the administrator is told that rather than told Lighthouse broke.
        [Test]
        public async Task AnAdministratorAskingAServiceNowConnectionForItsBoards_IsToldWhyRatherThanShownAFault()
        {
            var connectionId = await AServiceNowConnectionIsConfigured();

            var response = await client.GetAsync($"/api/latest/wizards/{connectionId}/boards");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
                "An instance that cannot be reached is a refusal the administrator can act on, not a fault and not an empty picker.");

            var verdict = await ReadVerdict(response);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsValid, Is.False);
                Assert.That(verdict.Message, Is.Not.Empty,
                    "The reason travels with the refusal — this is the whole difference between a picker that can be acted on and 'Failed to load boards. Please try again.'");
            }
        }

        // AC-B3 / ADR-126 decision 2. A board that could not be read is a refusal, not a pre-fill.
        // The dialog's fallback used to turn one into an all-empty board that could still be
        // confirmed — Confirm succeeded and silently wrote nothing.
        [Test]
        public async Task AnAdministratorAskingForOneBoardOfAnUnreachableInstance_IsToldWhyRatherThanOfferedABlankPreFill()
        {
            var connectionId = await AServiceNowConnectionIsConfigured();

            var response = await client.GetAsync($"/api/latest/wizards/{connectionId}/boards/any-board");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
                "A board that could not be read is a refusal. Handing back an all-empty board is what let Confirm succeed and write nothing.");

            var verdict = await ReadVerdict(response);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsValid, Is.False);
                Assert.That(verdict.Message, Is.Not.Empty);
            }
        }

        private async Task<int> AServiceNowConnectionIsConfigured()
        {
            client.AsSystemAdmin();

            var created = await PostConnectionTo(
                "/api/latest/worktrackingsystemconnections", NewServiceNowConnection());

            Assert.That(created.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var body = await created.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<WorkTrackingSystemConnectionDto>(body, JsonOptions)?.Id ?? 0;
        }

        private static WorkTrackingSystemConnectionDto NewServiceNowConnection()
        {
            var connection = new WorkTrackingSystemConnectionDto
            {
                Id = 0,
                Name = "Acme ServiceNow",
                WorkTrackingSystem = WorkTrackingSystems.ServiceNow,
                AuthenticationMethodKey = AuthenticationMethodKeys.ServiceNowBasic,
            };

            connection.Options.AddRange([
                new WorkTrackingSystemConnectionOptionDto { Key = ServiceNowWorkTrackingOptionNames.InstanceUrl, Value = UnreachableInstance },
                new WorkTrackingSystemConnectionOptionDto { Key = ServiceNowWorkTrackingOptionNames.Username, Value = "lighthouse.integration" },
                new WorkTrackingSystemConnectionOptionDto { Key = ServiceNowWorkTrackingOptionNames.Password, Value = Password, IsSecret = true },
            ]);

            return connection;
        }

        private static async Task<ConnectionValidationResult> ReadVerdict(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<ConnectionValidationResult>(body, JsonOptions)
                ?? new ConnectionValidationResult();
        }

        // The API serialises the work tracking system as its name, so the test client has to read
        // it the same way the browser does.
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        };

        private Task<List<WorkTrackingSystemConnectionDto>?> GetConnectionsFrom(string route)
        {
            return client.GetFromJsonAsync<List<WorkTrackingSystemConnectionDto>>(route, JsonOptions);
        }

        private Task<HttpResponseMessage> PostConnectionTo(string route, WorkTrackingSystemConnectionDto connection)
        {
            return client.PostAsJsonAsync(route, connection, JsonOptions);
        }
    }
}
