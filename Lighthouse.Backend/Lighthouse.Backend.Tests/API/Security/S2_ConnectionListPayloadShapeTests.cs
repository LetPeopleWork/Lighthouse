using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
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
        private const int TeamScopeId = 11;
        private const int PortfolioScopeId = 22;

        private readonly TestWebApplicationFactory<Program> rootFactory;
        private readonly WebApplicationFactory<Program> factory;
        private readonly HttpClient client;

        public S2_ConnectionListPayloadShapeTests()
        {
            rootFactory = new TestWebApplicationFactory<Program>();
            factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory);
            client = factory.CreateClient();
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
        public async Task S2_TeamAdmin_GetSummary_Returns200WithIdNameAndWorkTrackingSystemOnly()
        {
            client.AsTeamAdmin(TeamScopeId);

            var response = await client.GetAsync(SummaryPath);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            await AssertSummaryShape(response);
        }

        [Test]
        public async Task S2_PortfolioAdmin_GetSummary_Returns200WithIdNameAndWorkTrackingSystemOnly()
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

        private async Task<string> GetWithIdentity(Func<HttpClient, HttpClient> applyIdentity, string path)
        {
            using var scopedClient = factory.CreateClient();
            applyIdentity(scopedClient);
            var response = await scopedClient.GetAsync(path);
            return await response.Content.ReadAsStringAsync();
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
                Name = "Primary Jira",
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

            dbContext.WorkTrackingSystemConnections.Add(connection);
            dbContext.SaveChanges();
        }
    }
}
