using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Validation;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Services.Interfaces.Encryption;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    [TestFixture]
    public class ConnectionValidationUnreadableSecretTests
    {
        private const string ValidateRoute = "/api/latest/worktrackingsystemconnections/validate";

        private const string StoredCredential = "the-token-that-was-stored";

        private const string MissingKeyId = "k-not-on-this-ring";

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private Mock<IWorkTrackingConnector> connectorMock = null!;
        private int connectionWithLostCredentialId;
        private int healthyConnectionId;

        [SetUp]
        public void Init()
        {
            rootFactory = new TestWebApplicationFactory<Program>();

            connectorMock = new Mock<IWorkTrackingConnector>();
            connectorMock
                .Setup(c => c.ValidateConnection(It.IsAny<WorkTrackingSystemConnection>()))
                .ReturnsAsync(ConnectionValidationResult.Success);

            var connectorFactoryMock = new Mock<IWorkTrackingConnectorFactory>();
            connectorFactoryMock
                .Setup(f => f.GetWorkTrackingConnector(It.IsAny<WorkTrackingSystems>()))
                .Returns(connectorMock.Object);

            factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory)
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<IWorkTrackingConnectorFactory>();
                        services.AddScoped(_ => connectorFactoryMock.Object);
                    });
                });

            client = factory.CreateClient();

            using (var setupScope = factory.Services.CreateScope())
            {
                var dbContext = setupScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
            }

            connectionWithLostCredentialId = SeedConnection("Connection With A Lost Credential");
            healthyConnectionId = SeedConnection("Connection Whose Credentials All Read");

            MakeStoredCredentialUnreadable(connectionWithLostCredentialId, JiraWorkTrackingOptionNames.ApiToken);
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

        [Test]
        public async Task Validate_StoredCredentialCannotBeRead_SaysItCannotBeReadWithTheCurrentEncryptionKey()
        {
            client.AsSystemAdmin();

            var body = await ValidateConnection(connectionWithLostCredentialId, HttpStatusCode.BadRequest);
            var result = ResultOf(body);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.Message, Does.Contain("cannot be read with the current encryption key"));
                Assert.That(result.Message, Does.Contain("Enter it again"));
                Assert.That(result.FieldName, Is.EqualTo(JiraWorkTrackingOptionNames.ApiToken));
            }
        }

        [Test]
        public async Task Validate_StoredCredentialCannotBeRead_DoesNotSayTheWorkTrackingSystemRejectedIt()
        {
            client.AsSystemAdmin();

            var body = await ValidateConnection(connectionWithLostCredentialId, HttpStatusCode.BadRequest);
            var result = ResultOf(body);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Code, Is.Not.EqualTo("authentication_failed"));
                Assert.That(body, Does.Not.Contain("authentication failed").IgnoreCase);
                Assert.That(body, Does.Not.Contain("reject").IgnoreCase);
                Assert.That(body, Does.Not.Contain("expired").IgnoreCase);
                Assert.That(body, Does.Not.Contain("permission").IgnoreCase);
            }

            connectorMock.Verify(c => c.ValidateConnection(It.IsAny<WorkTrackingSystemConnection>()), Times.Never);
        }

        [Test]
        public async Task Validate_AllCredentialsRead_IsUnchanged()
        {
            client.AsSystemAdmin();

            var body = await ValidateConnection(healthyConnectionId, HttpStatusCode.OK);
            var result = ResultOf(body);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsValid, Is.True);
                Assert.That(result.Code, Is.EqualTo("valid"));
                Assert.That(result.Message, Is.EqualTo("Connection validated successfully."));
                Assert.That(body, Does.Not.Contain("encryption").IgnoreCase);
            }

            connectorMock.Verify(c => c.ValidateConnection(It.IsAny<WorkTrackingSystemConnection>()), Times.Once);
        }

        [Test]
        public async Task Validate_StoredCredentialCannotBeRead_CarriesNoPartOfTheStoredValueOrTheKey()
        {
            client.AsSystemAdmin();

            var body = await ValidateConnection(connectionWithLostCredentialId, HttpStatusCode.BadRequest);
            var mustNeverAppear = StoredValueAndKeyFragments();

            var leaked = mustNeverAppear
                .Where(fragment => body.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(mustNeverAppear, Is.Not.Empty, "The fixture must offer something to leak, or this test proves nothing.");
                Assert.That(leaked, Is.Empty);
            }
        }

        private async Task<string> ValidateConnection(int connectionId, HttpStatusCode expectedStatus)
        {
            var connection = await GetConnectionAsTheOperatorSeesIt(connectionId);

            using var payload = new StringContent(connection.ToJsonString(), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(ValidateRoute, payload);
            var body = await response.Content.ReadAsStringAsync();

            Assert.That(response.StatusCode, Is.EqualTo(expectedStatus), body);

            return body;
        }

        private async Task<JsonObject> GetConnectionAsTheOperatorSeesIt(int connectionId)
        {
            var response = await client.GetAsync("/api/latest/worktrackingsystemconnections");
            var body = await response.Content.ReadAsStringAsync();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            return JsonNode.Parse(body)!
                .AsArray()
                .Select(c => c!.AsObject())
                .Single(c => (int)c["id"]! == connectionId);
        }

        private static ConnectionValidationResult ResultOf(string body)
        {
            return JsonSerializer.Deserialize<ConnectionValidationResult>(body)!;
        }

        private List<string> StoredValueAndKeyFragments()
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var storedSecrets = dbContext.WorkTrackingSystemConnections
                .Include(c => c.Options)
                .Single(c => c.Id == connectionWithLostCredentialId)
                .Options
                .Where(o => o.IsSecret)
                .Select(o => o.Value)
                .ToList();

            var fragments = storedSecrets
                .SelectMany(storedSecret => storedSecret.Split('.'))
                .Where(field => field.Length > 4)
                .ToList();

            fragments.Add(StoredCredential);
            fragments.Add(MissingKeyId);
            fragments.Add(scope.ServiceProvider.GetRequiredService<IEncryptionKeyRingHolder>().Current.ActiveKey.Id);

            return fragments;
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

            connection.Options.Add(new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Url, Value = "https://example.test", IsSecret = false });
            connection.Options.Add(new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Username, Value = "operator@example.test", IsSecret = false });
            connection.Options.Add(new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.ApiToken, Value = StoredCredential, IsSecret = true });

            var repository = scope.ServiceProvider.GetRequiredService<IRepository<WorkTrackingSystemConnection>>();
            repository.Add(connection);
            repository.Save().GetAwaiter().GetResult();

            return connection.Id;
        }

        // Relabelling the stored envelope with a key id the instance does not hold is how an operator loses a
        // credential in real life - the key that wrote it is gone. It is done in SQL because saving the value
        // through the context would simply encrypt it again under the key that is present.
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
