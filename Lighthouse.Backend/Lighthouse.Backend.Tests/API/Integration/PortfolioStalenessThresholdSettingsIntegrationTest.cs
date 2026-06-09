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
    public class PortfolioStalenessThresholdSettingsIntegrationTest
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
        public async Task GetPortfolioSettings_NewlyCreatedPortfolio_ExposesDefaultStalenessThresholdOfZero()
        {
            client.AsPortfolioAdmin(seededPortfolioId);

            var response = await client.GetAsync($"/api/latest/portfolios/{seededPortfolioId}/settings");

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(TryGetInt(body, "stalenessThresholdDays", out var threshold), Is.True,
                    $"Portfolio settings payload must carry stalenessThresholdDays. Body: {body}");
                Assert.That(threshold, Is.Zero,
                    $"A newly-created portfolio must default to 0 days (staleness opt-in, US-05/DDD-12). Body: {body}");
            }
        }

        [Test]
        public async Task PutPortfolio_PortfolioAdminSetsThresholdToThirty_SettingsRoundTripsTheNewThreshold()
        {
            client.AsPortfolioAdmin(seededPortfolioId);

            var putResponse = await PutPortfolioWithThreshold(seededPortfolioId, 30);
            var putBody = await putResponse.Content.ReadAsStringAsync();
            Assert.That(putResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), putBody);

            var settingsResponse = await client.GetAsync($"/api/latest/portfolios/{seededPortfolioId}/settings");
            var settingsBody = await settingsResponse.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(settingsResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), settingsBody);
                Assert.That(TryGetInt(settingsBody, "stalenessThresholdDays", out var threshold), Is.True, settingsBody);
                Assert.That(threshold, Is.EqualTo(30),
                    $"Saved staleness threshold must round-trip through the settings payload. Body: {settingsBody}");
            }
        }

        [Test]
        public async Task PutPortfolio_PortfolioAdminSetsThresholdToZero_PersistsZeroToDisableHighlighting()
        {
            client.AsPortfolioAdmin(seededPortfolioId);

            var putResponse = await PutPortfolioWithThreshold(seededPortfolioId, 0);
            Assert.That(putResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), await putResponse.Content.ReadAsStringAsync());

            var settingsBody = await (await client.GetAsync($"/api/latest/portfolios/{seededPortfolioId}/settings")).Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TryGetInt(settingsBody, "stalenessThresholdDays", out var threshold), Is.True, settingsBody);
                Assert.That(threshold, Is.Zero, settingsBody);
            }
        }

        [Test]
        public async Task PutPortfolio_ThresholdBelowRange_ReturnsBadRequest()
        {
            client.AsPortfolioAdmin(seededPortfolioId);

            var response = await PutPortfolioWithThreshold(seededPortfolioId, -1);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), body);
        }

        [Test]
        public async Task PutPortfolio_ThresholdAboveRange_ReturnsBadRequest()
        {
            client.AsPortfolioAdmin(seededPortfolioId);

            var response = await PutPortfolioWithThreshold(seededPortfolioId, 366);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), body);
        }

        [Test]
        public async Task PutPortfolio_NonPortfolioAdminSetsThreshold_ReturnsForbidden()
        {
            client.AsViewer();

            var response = await PutPortfolioWithThreshold(seededPortfolioId, 30);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), body);
        }

        [Test]
        public async Task PutPortfolio_PortfolioAdminAddsNamedCycleTime_DefinitionPersistsAndIsStampedValid()
        {
            client.AsPortfolioAdmin(seededPortfolioId);

            var payload = BuildPortfolioSettingJson(seededPortfolioId);
            payload["cycleTimeDefinitions"] = new JsonArray
            {
                new JsonObject
                {
                    ["id"] = 0,
                    ["name"] = "Lead Time",
                    ["startState"] = "New",
                    ["endState"] = "Done",
                },
            };

            var content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");
            var putResponse = await client.PutAsync($"/api/latest/portfolios/{seededPortfolioId}", content);
            Assert.That(putResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), await putResponse.Content.ReadAsStringAsync());

            var settingsBody = await (await client.GetAsync($"/api/latest/portfolios/{seededPortfolioId}/settings")).Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(settingsBody);
            var definitions = document.RootElement.GetProperty("cycleTimeDefinitions").EnumerateArray().ToList();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(definitions, Has.Count.EqualTo(1),
                    $"A named cycle time saved through the portfolio PUT must persist — PortfolioExtensions previously dropped CycleTimeDefinitions entirely. Body: {settingsBody}");
                Assert.That(definitions[0].GetProperty("name").GetString(), Is.EqualTo("Lead Time"), settingsBody);
                Assert.That(definitions[0].GetProperty("isValid").GetBoolean(), Is.True, settingsBody);
            }
        }

        private async Task<HttpResponseMessage> PutPortfolioWithThreshold(int portfolioId, int threshold)
        {
            var payload = BuildPortfolioSettingJson(portfolioId);
            payload["stalenessThresholdDays"] = threshold;

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
                StateMappings = [],
                UsePercentileToCalculateDefaultAmountOfWorkItems = false,
                DefaultWorkItemPercentile = 85,
                PercentileHistoryInDays = 90,
                DefaultAmountOfWorkItemsPerFeature = 25,
                DoneItemsCutoffDays = 365,
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

        private static bool TryGetInt(string body, string propertyName, out int value)
        {
            value = default;
            using var document = JsonDocument.Parse(body);
            if (!document.RootElement.TryGetProperty(propertyName, out var prop) || prop.ValueKind != JsonValueKind.Number)
            {
                return false;
            }

            return prop.TryGetInt32(out value);
        }
    }
}
