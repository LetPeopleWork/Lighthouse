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

namespace Lighthouse.Backend.Tests.API.Integration.BlockedItems
{
    /// <summary>
    /// Step 04-01 — range-validation tests for blockedStalenessThresholdDays on the
    /// TeamController.UpdateTeam and PortfolioController.UpdatePortfolio endpoints.
    /// Reuses the existing MinStalenessThresholdDays (0) and MaxStalenessThresholdDays (365)
    /// constants. These tests fail RED because the validation is not yet wired.
    /// </summary>
    [TestFixture]
    [Category("epic-5074-blocked-items")]
    public class BlockedStalenessThresholdValidationTests
    {
        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private Mock<ILicenseService> licenseServiceMock = null!;
        private int seededTeamId;
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

            SeedTeamAndPortfolio();
        }

        [TearDown]
        public void Cleanup()
        {
            using var teardownScope = factory.Services.CreateScope();
            var dbContext = teardownScope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();

            client.Dispose();
            factory.Dispose();
            rootFactory.Dispose();
        }

        // --- Team validation ---

        [Test]
        public async Task PutTeam_BlockedStalenessThresholdBelowZero_ReturnsBadRequest()
        {
            client.AsTeamAdmin(seededTeamId);

            var response = await PutTeamWithBlockedStalenessThreshold(seededTeamId, -1);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task PutTeam_BlockedStalenessThresholdAbove365_ReturnsBadRequest()
        {
            client.AsTeamAdmin(seededTeamId);

            var response = await PutTeamWithBlockedStalenessThreshold(seededTeamId, 366);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task PutTeam_BlockedStalenessThresholdValidValue_ReturnsOk()
        {
            client.AsTeamAdmin(seededTeamId);

            var response = await PutTeamWithBlockedStalenessThreshold(seededTeamId, 10);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        // --- Portfolio validation ---

        [Test]
        public async Task PutPortfolio_BlockedStalenessThresholdBelowZero_ReturnsBadRequest()
        {
            client.AsPortfolioAdmin(seededPortfolioId);

            var response = await PutPortfolioWithBlockedStalenessThreshold(seededPortfolioId, -1);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task PutPortfolio_BlockedStalenessThresholdAbove365_ReturnsBadRequest()
        {
            client.AsPortfolioAdmin(seededPortfolioId);

            var response = await PutPortfolioWithBlockedStalenessThreshold(seededPortfolioId, 366);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task PutPortfolio_BlockedStalenessThresholdValidValue_ReturnsOk()
        {
            client.AsPortfolioAdmin(seededPortfolioId);

            var response = await PutPortfolioWithBlockedStalenessThreshold(seededPortfolioId, 10);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        // --- helpers ---

        private async Task<HttpResponseMessage> PutTeamWithBlockedStalenessThreshold(int teamId, int threshold)
        {
            var payload = BuildTeamSettingJson(teamId);
            payload["blockedStalenessThresholdDays"] = threshold;

            var content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");
            return await client.PutAsync($"/api/latest/teams/{teamId}", content);
        }

        private async Task<HttpResponseMessage> PutPortfolioWithBlockedStalenessThreshold(int portfolioId, int threshold)
        {
            var payload = BuildPortfolioSettingJson(portfolioId);
            payload["blockedStalenessThresholdDays"] = threshold;

            var content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");
            return await client.PutAsync($"/api/latest/portfolios/{portfolioId}", content);
        }

        private void SeedTeamAndPortfolio()
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var connection = new WorkTrackingSystemConnection
            {
                Name = $"Connection {Guid.NewGuid():N}",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
            };

            var team = new Team
            {
                Name = $"Team {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = connection,
            };

            var portfolio = new Portfolio
            {
                Name = $"Portfolio {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = connection,
            };

            var teamRepository = sp.GetRequiredService<IRepository<Team>>();
            var portfolioRepository = sp.GetRequiredService<IRepository<Portfolio>>();
            teamRepository.Add(team);
            portfolioRepository.Add(portfolio);
            teamRepository.Save().GetAwaiter().GetResult();

            seededTeamId = team.Id;
            seededPortfolioId = portfolio.Id;
            seededConnectionId = connection.Id;
        }

        private JsonObject BuildTeamSettingJson(int teamId)
        {
            var dto = new TeamSettingDto
            {
                Id = teamId,
                Name = $"Team {teamId}",
                DataRetrievalValue = "project = TEST",
                WorkTrackingSystemConnectionId = seededConnectionId,
                WorkItemTypes = ["User Story", "Bug"],
                ToDoStates = ["New"],
                DoingStates = ["Active"],
                DoneStates = ["Done"],
                ThroughputHistory = 30,
                UseFixedDatesForThroughput = false,
                FeatureWIP = 1,
                AutomaticallyAdjustFeatureWIP = false,
                DoneItemsCutoffDays = 365,
                StateMappings = [],
            };

            var serialized = JsonSerializer.Serialize(dto);
            return JsonNode.Parse(serialized)!.AsObject();
        }

        private JsonObject BuildPortfolioSettingJson(int portfolioId)
        {
            var dto = new PortfolioSettingDto
            {
                Id = portfolioId,
                Name = $"Portfolio {portfolioId}",
                DataRetrievalValue = string.Empty,
                WorkTrackingSystemConnectionId = seededConnectionId,
                WorkItemTypes = ["Epic"],
                ToDoStates = ["New"],
                DoingStates = ["Active"],
                DoneStates = ["Done"],
                DoneItemsCutoffDays = 365,
                DefaultAmountOfWorkItemsPerFeature = 25,
                StateMappings = [],
            };

            var serialized = JsonSerializer.Serialize(dto);
            return JsonNode.Parse(serialized)!.AsObject();
        }
    }
}
