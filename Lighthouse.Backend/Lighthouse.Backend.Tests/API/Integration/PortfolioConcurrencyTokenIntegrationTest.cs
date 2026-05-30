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
    public class PortfolioConcurrencyTokenIntegrationTest
    {
        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private Mock<ILicenseService> licenseServiceMock = null!;
        private int seededPortfolioId;
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

            SeedPortfolio();
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
            client.AsPortfolioAdmin(seededPortfolioId);

            var initialSettings = await GetPortfolioSettings();
            var staleToken = ConcurrencyTokenTestHelpers.GetToken(initialSettings);

            var adminAResponse = await PutPortfolioWithNameAndToken(seededPortfolioId, "Renamed By Admin A", staleToken);
            Assert.That(adminAResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), await adminAResponse.Content.ReadAsStringAsync());

            var adminBResponse = await PutPortfolioWithNameAndToken(seededPortfolioId, "Renamed By Admin B", staleToken);
            var adminBBody = await adminBResponse.Content.ReadAsStringAsync();

            var settingsAfterConflict = await GetPortfolioSettings();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(adminBResponse.StatusCode, Is.EqualTo(HttpStatusCode.Conflict), adminBBody);
                Assert.That(adminBBody, Does.Contain(ConcurrencyTokenTestHelpers.ConcurrencyConflictCode),
                    $"409 body must carry the distinguishable concurrency code so clients can render a reload affordance. Body: {adminBBody}");
                Assert.That(ConcurrencyTokenTestHelpers.GetString(settingsAfterConflict, "name"), Is.EqualTo("Renamed By Admin A"),
                    "Admin A's write must be preserved; Admin B's stale write must not overwrite it.");
            }
        }

        [Test]
        public async Task SingleAdminHappyPath_PutWithJustReadToken_Returns200_NoFalseConflict()
        {
            client.AsPortfolioAdmin(seededPortfolioId);

            var settings = await GetPortfolioSettings();
            var token = ConcurrencyTokenTestHelpers.GetToken(settings);

            var putResponse = await PutPortfolioWithNameAndToken(seededPortfolioId, "Lone Editor Portfolio", token);

            var body = await putResponse.Content.ReadAsStringAsync();
            Assert.That(putResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"A lone editor saving with the token they just read must not see a false 409. Body: {body}");
        }

        private async Task<JsonObject> GetPortfolioSettings()
        {
            return await ConcurrencyTokenTestHelpers.GetJsonObject(client, $"/api/latest/portfolios/{seededPortfolioId}/settings");
        }

        private async Task<HttpResponseMessage> PutPortfolioWithNameAndToken(int portfolioId, string name, Guid token)
        {
            var payload = BuildPortfolioSettingJson(portfolioId);
            payload["name"] = name;
            payload["concurrencyToken"] = token.ToString();

            var content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");
            return await client.PutAsync($"/api/latest/portfolios/{portfolioId}", content);
        }

        private JsonObject BuildPortfolioSettingJson(int portfolioId)
        {
            var dto = new PortfolioSettingDto
            {
                Id = portfolioId,
                Name = $"Portfolio {portfolioId}",
                DataRetrievalValue = "project = TEST",
                WorkTrackingSystemConnectionId = seededConnectionId,
                WorkItemTypes = ["Epic"],
                ToDoStates = ["New"],
                DoingStates = ["Active"],
                DoneStates = ["Done"],
                DoneItemsCutoffDays = 365,
                StateMappings = [],
            };

            var serialized = JsonSerializer.Serialize(dto);
            return JsonNode.Parse(serialized)!.AsObject();
        }

        private void SeedPortfolio()
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var connection = new WorkTrackingSystemConnection
            {
                Name = $"Connection {Guid.NewGuid():N}",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
            };

            var portfolio = new Portfolio
            {
                Name = $"Portfolio {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = connection,
            };

            var portfolioRepository = sp.GetRequiredService<IRepository<Portfolio>>();
            portfolioRepository.Add(portfolio);
            portfolioRepository.Save().GetAwaiter().GetResult();

            seededPortfolioId = portfolio.Id;
            seededConnectionId = connection.Id;
        }
    }
}
