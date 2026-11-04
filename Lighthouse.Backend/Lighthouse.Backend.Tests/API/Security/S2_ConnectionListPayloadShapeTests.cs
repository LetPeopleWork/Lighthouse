using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.API.Security
{
    public class S2_ConnectionListPayloadShapeTests
    {
        private const string FullListPath = "/api/v1/worktrackingsystemconnections";
        private const string SummaryPath = "/api/v1/worktrackingsystemconnections/summary";
        private const string SecretValue = "encrypted-blob-here";
        private const string NonSecretValue = "https://example.test/project";
        private const string SecretKey = "PersonalAccessToken";
        private const string NonSecretKey = "Url";
        private const string AdditionalFieldName = "Story Points";
        private const string AdditionalFieldReference = "customfield_10016";
        private const string PredefinedFieldName = "Flagged";
        private const string DriftedPredefinedFieldReference = "customfield_stale";
        private const string ConnectionName = "Primary Jira";
        private const int TeamScopeId = 11;
        private const int PortfolioScopeId = 22;
        private const int ConnectionIdNobodyHolds = 999999;

        private readonly TestWebApplicationFactory<Program> rootFactory;
        private readonly WebApplicationFactory<Program> factory;
        private readonly HttpClient client;
        private int seededConnectionId;

        public S2_ConnectionListPayloadShapeTests()
        {
            rootFactory = new TestWebApplicationFactory<Program>();
            factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory);
            client = factory.CreateClient();
        }

        [SetUp]
        public void StartFromOneSeededConnection()
        {
            rootFactory.UseFreshDatabase();
            SeedConnection();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            client.Dispose();
            factory.Dispose();
            rootFactory.Dispose();
        }

        [Test]
        public async Task S2_SystemAdmin_GetFullList_Returns200WithFullDtoAndSecretsBlanked()
        {
            client.AsSystemAdmin();

            var response = await client.GetAsync(FullListPath);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var body = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(body);

            var first = document.RootElement.EnumerateArray().First();
            Assert.That(first.TryGetProperty("options", out var options), Is.True);

            var secretOption = options.EnumerateArray()
                .Single(o => o.GetProperty("key").GetString() == SecretKey);
            var nonSecretOption = options.EnumerateArray()
                .Single(o => o.GetProperty("key").GetString() == NonSecretKey);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(secretOption.GetProperty("value").GetString(), Is.Empty);
                Assert.That(nonSecretOption.GetProperty("value").GetString(), Is.EqualTo(NonSecretValue));
            }
        }

        [Test]
        public async Task S2_TeamAdmin_GetFullList_Returns403()
        {
            client.AsTeamAdmin(TeamScopeId);

            var response = await client.GetAsync(FullListPath);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        }

        [Test]
        public async Task S2_PortfolioAdmin_GetFullList_Returns403()
        {
            client.AsPortfolioAdmin(PortfolioScopeId);

            var response = await client.GetAsync(FullListPath);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        }

        [Test]
        public async Task S2_TeamAdmin_GetSummary_CarriesTheFieldsTheSettingsFormReads()
        {
            client.AsTeamAdmin(TeamScopeId);

            var response = await client.GetAsync(SummaryPath);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            await AssertSummaryShape(response);
        }

        [Test]
        public async Task S2_TeamAdmin_GetSummary_CarriesTheConnectionsAdditionalFieldsAndSupportedDefaults()
        {
            client.AsTeamAdmin(TeamScopeId);

            var response = await client.GetAsync(SummaryPath);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var summary = document.RootElement.EnumerateArray().Single();
            var field = summary.GetProperty("additionalFieldDefinitions").EnumerateArray().Single();
            var supportedDefaults = summary.GetProperty("supportedDefaults").EnumerateArray()
                .Select(d => d.GetString()).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(field.GetProperty("id").GetInt32(), Is.GreaterThan(0));
                Assert.That(field.GetProperty("displayName").GetString(), Is.EqualTo(AdditionalFieldName));
                Assert.That(supportedDefaults, Does.Contain("estimationForTeams"));
                Assert.That(supportedDefaults, Has.Count.EqualTo(SupportedConnectionDefaults.For(WorkTrackingSystems.Jira).Count));
            }
        }

        [Test]
        public async Task S2_PortfolioAdmin_GetSummary_CarriesTheFieldsTheSettingsFormReads()
        {
            client.AsPortfolioAdmin(PortfolioScopeId);

            var response = await client.GetAsync(SummaryPath);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            await AssertSummaryShape(response);
        }

        [Test]
        public async Task S2_Viewer_GetFullList_Returns403()
        {
            client.AsViewer();

            var response = await client.GetAsync(FullListPath);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        }

        [Test]
        public async Task S2_Viewer_GetSummary_Returns403()
        {
            client.AsViewer();

            var response = await client.GetAsync(SummaryPath);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        }

        [Test]
        public async Task S2_AnyCaller_ResponseBodyNeverContainsSecretValue_Regression()
        {
            var systemAdminFullList = await GetWithIdentity(c => c.AsSystemAdmin(), FullListPath);
            var teamAdminSummary = await GetWithIdentity(c => c.AsTeamAdmin(TeamScopeId), SummaryPath);
            var portfolioAdminSummary = await GetWithIdentity(c => c.AsPortfolioAdmin(PortfolioScopeId), SummaryPath);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(systemAdminFullList, Does.Not.Contain(SecretValue));
                Assert.That(teamAdminSummary, Does.Not.Contain(SecretValue));
                Assert.That(portfolioAdminSummary, Does.Not.Contain(SecretValue));
            }
        }

        [Test]
        public async Task S2_Viewer_GetSingleConnection_Returns403()
        {
            client.AsViewer();

            var response = await client.GetAsync(SeededConnectionPath);

            await AssertRefused(response);
        }

        [Test]
        public async Task S2_TeamAdmin_GetSingleConnection_Returns403()
        {
            client.AsTeamAdmin(TeamScopeId);

            var response = await client.GetAsync(SeededConnectionPath);

            await AssertRefused(response);
        }

        [Test]
        public async Task S2_PortfolioAdmin_GetSingleConnection_Returns403()
        {
            client.AsPortfolioAdmin(PortfolioScopeId);

            var response = await client.GetAsync(SeededConnectionPath);

            await AssertRefused(response);
        }

        [Test]
        public async Task S2_NonAdmins_GetSingleConnectionThatDoesNotExist_Returns403SoTheAnswerRevealsNoIds()
        {
            var path = ConnectionPath(ConnectionIdNobodyHolds);

            var viewer = await StatusWithIdentity(c => c.AsViewer(), path);
            var teamAdmin = await StatusWithIdentity(c => c.AsTeamAdmin(TeamScopeId), path);
            var portfolioAdmin = await StatusWithIdentity(c => c.AsPortfolioAdmin(PortfolioScopeId), path);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewer, Is.EqualTo(HttpStatusCode.Forbidden));
                Assert.That(teamAdmin, Is.EqualTo(HttpStatusCode.Forbidden));
                Assert.That(portfolioAdmin, Is.EqualTo(HttpStatusCode.Forbidden));
            }
        }

        [Test]
        public async Task S2_SystemAdmin_GetSingleConnection_Returns200WithSecretsBlanked()
        {
            client.AsSystemAdmin();

            var response = await client.GetAsync(SeededConnectionPath);

            await AssertFullConnectionWithSecretsBlanked(response);
        }

        [Test]
        public async Task S2_SystemAdmin_GetSingleConnectionThatDoesNotExist_Returns404()
        {
            client.AsSystemAdmin();

            var response = await client.GetAsync(ConnectionPath(ConnectionIdNobodyHolds));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task S2_SystemAdmin_GetSingleConnection_ReconcilesADriftedPredefinedField()
        {
            await GivenThePredefinedFieldHasDrifted();
            client.AsSystemAdmin();

            var response = await client.GetAsync(SeededConnectionPath);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var predefinedFields = StoredPredefinedFields();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(predefinedFields, Has.Count.EqualTo(1));
                Assert.That(predefinedFields[0].Reference, Is.Not.EqualTo(DriftedPredefinedFieldReference));
            }
        }

        [Test]
        public async Task S2_NonAdmins_GetSingleConnection_LeaveADriftedPredefinedFieldAsItIs()
        {
            await GivenThePredefinedFieldHasDrifted();

            await ReadTheConnectionAsEveryNonAdmin();

            var predefinedFields = StoredPredefinedFields();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(predefinedFields, Has.Count.EqualTo(1));
                Assert.That(predefinedFields[0].Reference, Is.EqualTo(DriftedPredefinedFieldReference));
            }
        }

        [Test]
        public async Task S2_NonAdmins_GetSingleConnection_RegisterNoMissingPredefinedField()
        {
            await ReadTheConnectionAsEveryNonAdmin();

            Assert.That(StoredPredefinedFields(), Is.Empty);
        }

        [Test]
        public async Task S2_AuthOff_GetSingleConnection_AnswersAsForASystemAdmin()
        {
            using var authOffClient = rootFactory.CreateClient();

            var response = await authOffClient.GetAsync(SeededConnectionPath);
            var unknownConnection = await authOffClient.GetAsync(ConnectionPath(ConnectionIdNobodyHolds));

            await AssertFullConnectionWithSecretsBlanked(response);
            Assert.That(unknownConnection.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        private string SeededConnectionPath => ConnectionPath(seededConnectionId);

        private static string ConnectionPath(int connectionId) => $"{FullListPath}/{connectionId}";

        private async Task<string> GetWithIdentity(Func<HttpClient, HttpClient> applyIdentity, string path)
        {
            var response = await ResponseWithIdentity(applyIdentity, path);
            return await response.Content.ReadAsStringAsync();
        }

        private async Task<HttpStatusCode> StatusWithIdentity(Func<HttpClient, HttpClient> applyIdentity, string path)
        {
            var response = await ResponseWithIdentity(applyIdentity, path);
            return response.StatusCode;
        }

        private async Task<HttpResponseMessage> ResponseWithIdentity(Func<HttpClient, HttpClient> applyIdentity, string path)
        {
            using var scopedClient = factory.CreateClient();
            applyIdentity(scopedClient);
            return await scopedClient.GetAsync(path);
        }

        private async Task ReadTheConnectionAsEveryNonAdmin()
        {
            await StatusWithIdentity(c => c.AsViewer(), SeededConnectionPath);
            await StatusWithIdentity(c => c.AsTeamAdmin(TeamScopeId), SeededConnectionPath);
            await StatusWithIdentity(c => c.AsPortfolioAdmin(PortfolioScopeId), SeededConnectionPath);
        }

        private static async Task AssertRefused(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
                Assert.That(body, Does.Not.Contain(ConnectionName));
                Assert.That(body, Does.Not.Contain(NonSecretValue));
            }
        }

        private async Task AssertFullConnectionWithSecretsBlanked(HttpResponseMessage response)
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var body = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(body);
            var connection = document.RootElement;
            var options = connection.GetProperty("options").EnumerateArray().ToList();
            var fieldNames = connection.GetProperty("additionalFieldDefinitions").EnumerateArray()
                .Select(field => field.GetProperty("displayName").GetString());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(connection.GetProperty("id").GetInt32(), Is.EqualTo(seededConnectionId));
                Assert.That(connection.GetProperty("name").GetString(), Is.EqualTo(ConnectionName));
                Assert.That(fieldNames, Does.Contain(AdditionalFieldName));
                Assert.That(options.Single(o => o.GetProperty("key").GetString() == NonSecretKey).GetProperty("value").GetString(), Is.EqualTo(NonSecretValue));
                Assert.That(options.Single(o => o.GetProperty("key").GetString() == SecretKey).GetProperty("value").GetString(), Is.Empty);
                Assert.That(body, Does.Not.Contain(SecretValue));
            }
        }

        private async Task GivenThePredefinedFieldHasDrifted()
        {
            using var scope = rootFactory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<WorkTrackingSystemConnection>>();
            var connection = repository.GetById(seededConnectionId)!;
            connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition
            {
                DisplayName = PredefinedFieldName,
                Reference = DriftedPredefinedFieldReference,
                IsPredefined = true,
            });
            await repository.Save();
        }

        private List<AdditionalFieldDefinition> StoredPredefinedFields()
        {
            using var scope = rootFactory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<WorkTrackingSystemConnection>>();
            return repository.GetById(seededConnectionId)!.AdditionalFieldDefinitions
                .Where(field => field.IsPredefined)
                .ToList();
        }

        private static async Task AssertSummaryShape(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(body);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(document.RootElement.ValueKind, Is.EqualTo(JsonValueKind.Array));
                Assert.That(document.RootElement.GetArrayLength(), Is.GreaterThan(0));
            }

            var allowedProperties = new HashSet<string>(StringComparer.Ordinal)
            {
                "id",
                "name",
                "workTrackingSystem",
                "additionalFieldDefinitions",
                "defaults",
                "supportedDefaults",
            };

            foreach (var item in document.RootElement.EnumerateArray())
            {
                var propertyNames = item.EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);

                Assert.That(propertyNames, Is.EquivalentTo(allowedProperties));
            }
        }

        private void SeedConnection()
        {
            using var scope = rootFactory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            var connection = new WorkTrackingSystemConnection
            {
                Name = ConnectionName,
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                AuthenticationMethodKey = "pat",
            };
            connection.Options.Add(new WorkTrackingSystemConnectionOption
            {
                Key = NonSecretKey,
                Value = NonSecretValue,
                IsSecret = false,
                IsOptional = false,
            });
            connection.Options.Add(new WorkTrackingSystemConnectionOption
            {
                Key = SecretKey,
                Value = SecretValue,
                IsSecret = true,
                IsOptional = false,
            });

            connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition
            {
                DisplayName = AdditionalFieldName,
                Reference = AdditionalFieldReference,
            });

            dbContext.WorkTrackingSystemConnections.Add(connection);
            dbContext.SaveChanges();
            seededConnectionId = connection.Id;
        }
    }
}
