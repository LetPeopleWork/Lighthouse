using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    [TestFixture]
    [NonParallelizable]
    public class WorkTrackingSystemConnectionConcurrencyTokenIntegrationTest
    {
        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private Mock<ILicenseService> licenseServiceMock = null!;
        private int seededConnectionId;

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

            using var setupScope = factory.Services.CreateScope();
            var dbContext = setupScope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            SeedConnection();
        }

        [TearDown]
        public void Cleanup()
        {
            using (var teardownScope = factory.Services.CreateScope())
            {
                var dbContext = teardownScope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
                dbContext.Database.EnsureDeleted();
            }

            client.Dispose();
            factory.Dispose();
            rootFactory.Dispose();
        }

        [Test]
        public async Task TwoStaleWrites_SecondWriterCarriesStaleToken_Returns409_NoWrite_FirstWriterValuePreserved()
        {
            client.AsSystemAdmin();

            var initialConnection = await GetConnection();
            var staleToken = ConcurrencyTokenTestHelpers.GetToken(initialConnection);

            var adminAResponse = await PutConnectionWithNameAndToken(seededConnectionId, "Renamed By Admin A", staleToken);
            Assert.That(adminAResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), await adminAResponse.Content.ReadAsStringAsync());

            var adminBResponse = await PutConnectionWithNameAndToken(seededConnectionId, "Renamed By Admin B", staleToken);
            var adminBBody = await adminBResponse.Content.ReadAsStringAsync();

            var connectionAfterConflict = await GetConnection();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(adminBResponse.StatusCode, Is.EqualTo(HttpStatusCode.Conflict), adminBBody);
                Assert.That(adminBBody, Does.Contain(ConcurrencyTokenTestHelpers.ConcurrencyConflictCode),
                    $"409 body must carry the distinguishable concurrency code so clients can render a reload affordance. Body: {adminBBody}");
                Assert.That(ConcurrencyTokenTestHelpers.GetString(connectionAfterConflict, "name"), Is.EqualTo("Renamed By Admin A"),
                    "Admin A's write must be preserved; Admin B's stale write must not overwrite it.");
            }
        }

        [Test]
        public async Task SingleAdminHappyPath_PutWithJustReadToken_Returns200_NoFalseConflict()
        {
            client.AsSystemAdmin();

            var connection = await GetConnection();
            var token = ConcurrencyTokenTestHelpers.GetToken(connection);

            var putResponse = await PutConnectionWithNameAndToken(seededConnectionId, "Lone Editor Connection", token);

            var body = await putResponse.Content.ReadAsStringAsync();
            Assert.That(putResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"A lone editor saving with the token they just read must not see a false 409. Body: {body}");
        }

        private async Task<JsonObject> GetConnection()
        {
            return await ConcurrencyTokenTestHelpers.GetJsonObject(client, $"/api/latest/worktrackingsystemconnections/{seededConnectionId}");
        }

        private async Task<HttpResponseMessage> PutConnectionWithNameAndToken(int connectionId, string name, Guid token)
        {
            var dto = new WorkTrackingSystemConnectionDto
            {
                Id = connectionId,
                Name = name,
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                AuthenticationMethodKey = AuthenticationMethodKeys.JiraCloud,
                ConcurrencyToken = token,
            };

            var content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
            return await client.PutAsync($"/api/latest/worktrackingsystemconnections/{connectionId}", content);
        }

        private void SeedConnection()
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var connection = new WorkTrackingSystemConnection
            {
                Name = $"Connection {Guid.NewGuid():N}",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                AuthenticationMethodKey = AuthenticationMethodKeys.JiraCloud,
            };

            var repository = sp.GetRequiredService<IRepository<WorkTrackingSystemConnection>>();
            repository.Add(connection);
            repository.Save().GetAwaiter().GetResult();

            seededConnectionId = connection.Id;
        }
    }
}
