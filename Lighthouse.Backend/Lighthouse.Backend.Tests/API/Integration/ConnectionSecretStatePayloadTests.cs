using System.Net;
using System.Text.Json.Nodes;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    [TestFixture]
    public class ConnectionSecretStatePayloadTests
    {
        private const string LostCredentialKey = "Personal Access Token";

        private const string IntactCredentialKey = "Api Token";

        private const string PlainOptionKey = "Jira Url";

        private const string MissingKeyId = "k-not-on-this-ring";

        private static readonly string[] ConnectionPropertiesToday =
        [
            "id",
            "name",
            "workTrackingSystem",
            "authenticationMethodKey",
            "authenticationMethodDisplayName",
            "availableAuthenticationMethods",
            "options",
            "additionalFieldDefinitions",
            "writeBackMappingDefinitions",
            "requiresReconnect",
            "concurrencyToken",
        ];

        private static readonly string[] OptionPropertiesToday =
        [
            "key",
            "value",
            "isSecret",
            "isOptional",
        ];

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private int connectionWithLostCredentialId;
        private int healthyConnectionId;

        [SetUp]
        public void Init()
        {
            rootFactory = new TestWebApplicationFactory<Program>();
            factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory);
            client = factory.CreateClient();

            using (var setupScope = factory.Services.CreateScope())
            {
                var dbContext = setupScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
            }

            connectionWithLostCredentialId = SeedConnection("Connection With A Lost Credential");
            healthyConnectionId = SeedConnection("Connection Whose Credentials All Read");

            MakeStoredCredentialUnreadable(connectionWithLostCredentialId, LostCredentialKey);
        }

        [TearDown]
        public void Cleanup()
        {
            using (var teardownScope = factory.Services.CreateScope())
            {
                var dbContext = teardownScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                dbContext.Database.EnsureDeleted();
            }

            client.Dispose();
            factory.Dispose();
            rootFactory.Dispose();
        }

        [TestCase("v1")]
        [TestCase("latest")]
        public async Task GetConnections_StoredCredentialCannotBeRead_NamesTheOptionThatHoldsIt(string apiVersion)
        {
            client.AsSystemAdmin();

            var connections = await GetConnections(apiVersion);
            var connection = ConnectionWithId(connections, connectionWithLostCredentialId);

            Assert.That(StateOf(connection, LostCredentialKey), Is.EqualTo("Unreadable"));
        }

        [TestCase("v1")]
        [TestCase("latest")]
        public async Task GetConnections_StoredCredentialCannotBeRead_LeavesTheOtherOptionsAlone(string apiVersion)
        {
            client.AsSystemAdmin();

            var connections = await GetConnections(apiVersion);
            var connection = ConnectionWithId(connections, connectionWithLostCredentialId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(StateOf(connection, IntactCredentialKey), Is.EqualTo("Envelope"));
                Assert.That(StateOf(connection, PlainOptionKey), Is.Null);
            }
        }

        [TestCase("v1")]
        [TestCase("latest")]
        public async Task GetConnections_AllCredentialsRead_ReportsNothingUnreadable(string apiVersion)
        {
            client.AsSystemAdmin();

            var connections = await GetConnections(apiVersion);
            var connection = ConnectionWithId(connections, healthyConnectionId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(StateOf(connection, LostCredentialKey), Is.EqualTo("Envelope"));
                Assert.That(StateOf(connection, IntactCredentialKey), Is.EqualTo("Envelope"));
            }
        }

        [Test]
        public async Task GetConnections_OptionHoldsNoSecret_CarriesNoReadabilityState()
        {
            client.AsSystemAdmin();

            var connections = await GetConnections("latest");
            var connection = ConnectionWithId(connections, healthyConnectionId);
            var plainOption = OptionWithKey(connection, PlainOptionKey);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(plainOption.ContainsKey("secretState"), Is.True, "The property must be present so the shape is the same on every option.");
                Assert.That((string?)plainOption["secretState"], Is.Null);
            }
        }

        [TestCase("v1")]
        [TestCase("latest")]
        public async Task GetConnections_SecretOption_HasNoValueForAnyRoleThatCanReachThePayload(string apiVersion)
        {
            client.AsSystemAdmin();

            var connections = await GetConnections(apiVersion);

            var secretValues = connections
                .Select(c => c!.AsObject())
                .SelectMany(c => c["options"]!.AsArray())
                .Select(o => o!.AsObject())
                .Where(o => (bool)o["isSecret"]!)
                .Select(o => (string?)o["value"])
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(secretValues, Has.Count.EqualTo(4));
                Assert.That(secretValues, Is.All.EqualTo(string.Empty));
            }
        }

        [Test]
        public async Task GetConnections_CallerIsNotASystemAdmin_PayloadIsNotServedAtAll()
        {
            var refusedForRoles = new List<HttpStatusCode>();

            foreach (var authenticateAs in RolesBelowSystemAdmin)
            {
                authenticateAs(client);
                var response = await client.GetAsync("/api/latest/worktrackingsystemconnections");
                refusedForRoles.Add(response.StatusCode);
            }

            Assert.That(refusedForRoles, Is.All.EqualTo(HttpStatusCode.Forbidden));
        }

        [Test]
        public async Task GetConnections_AllCredentialsRead_PayloadIsOtherwiseUnchanged()
        {
            client.AsSystemAdmin();

            var connections = await GetConnections("latest");
            var connection = ConnectionWithId(connections, healthyConnectionId);
            var option = OptionWithKey(connection, PlainOptionKey);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(PropertyNames(connection), Is.EquivalentTo(ConnectionPropertiesToday));
                Assert.That(PropertyNames(option), Is.EquivalentTo(OptionPropertiesToday.Append("secretState")));
            }
        }

        private static IEnumerable<Action<HttpClient>> RolesBelowSystemAdmin =>
        [
            c => c.AsViewer(),
            c => c.AsTeamAdmin(1),
            c => c.AsPortfolioAdmin(1),
        ];

        private async Task<JsonArray> GetConnections(string apiVersion)
        {
            var response = await client.GetAsync($"/api/{apiVersion}/worktrackingsystemconnections");
            var body = await response.Content.ReadAsStringAsync();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            return JsonNode.Parse(body)!.AsArray();
        }

        private static IEnumerable<string> PropertyNames(JsonObject jsonObject)
        {
            return jsonObject.Select(property => property.Key);
        }

        private static JsonObject ConnectionWithId(JsonArray connections, int connectionId)
        {
            return connections.Select(c => c!.AsObject()).Single(c => (int)c["id"]! == connectionId);
        }

        private static JsonObject OptionWithKey(JsonObject connection, string optionKey)
        {
            return connection["options"]!
                .AsArray()
                .Select(o => o!.AsObject())
                .Single(o => (string?)o["key"] == optionKey);
        }

        private static string? StateOf(JsonObject connection, string optionKey)
        {
            return (string?)OptionWithKey(connection, optionKey)["secretState"];
        }

        private int SeedConnection(string name)
        {
            using var scope = factory.Services.CreateScope();

            var connection = new WorkTrackingSystemConnection
            {
                Name = name,
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                AuthenticationMethodKey = AuthenticationMethodKeys.JiraCloud,
            };

            connection.Options.Add(new WorkTrackingSystemConnectionOption { Key = PlainOptionKey, Value = "https://example.test", IsSecret = false });
            connection.Options.Add(new WorkTrackingSystemConnectionOption { Key = LostCredentialKey, Value = "the-token-nobody-can-read", IsSecret = true });
            connection.Options.Add(new WorkTrackingSystemConnectionOption { Key = IntactCredentialKey, Value = "the-token-that-still-reads", IsSecret = true });

            var repository = scope.ServiceProvider.GetRequiredService<IRepository<WorkTrackingSystemConnection>>();
            repository.Add(connection);
            repository.Save().GetAwaiter().GetResult();

            return connection.Id;
        }

        // Relabelling the stored envelope with a key id the instance does not hold is how an operator loses
        // a credential in real life — the key that wrote it is gone. It is done in SQL because saving the
        // value through the context would simply encrypt it again under the key that is present.
        private void MakeStoredCredentialUnreadable(int connectionId, string optionKey)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var storedValue = dbContext.WorkTrackingSystemConnections
                .Include(c => c.Options)
                .Single(c => c.Id == connectionId)
                .Options
                .Single(o => o.Key == optionKey)
                .Value;

            var fields = storedValue.Split('.');
            fields[1] = MissingKeyId;

            dbContext.Database.ExecuteSqlRaw(
                "UPDATE WorkTrackingSystemConnectionOption SET Value = {0} WHERE WorkTrackingSystemConnectionId = {1} AND Key = {2}",
                string.Join('.', fields),
                connectionId,
                optionKey);
        }
    }
}
