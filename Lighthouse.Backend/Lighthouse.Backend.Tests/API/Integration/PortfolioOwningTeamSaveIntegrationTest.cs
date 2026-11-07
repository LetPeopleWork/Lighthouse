using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Licensing;
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
    public class PortfolioOwningTeamSaveIntegrationTest : IntegrationFixture
    {
        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private int portfolioId;
        private int owningTeamId;
        private int otherTeamId;

        [OneTimeSetUp]
        public void BuildHost()
        {
            rootFactory = new TestWebApplicationFactory<Program>();

            var licenseService = new Mock<ILicenseService>();
            licenseService.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory)
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<ILicenseService>();
                        services.AddScoped(_ => licenseService.Object);
                    });
                });

            client = factory.CreateClient();
        }

        [SetUp]
        public void ResetDatabase()
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
        }

        [TearDown]
        public async Task LeaveTheHostIdle()
        {
            await SharedHostCleanup.WaitUntilTheQueueIsIdleOrFail(factory.Services, TimeSpan.FromSeconds(30));
        }

        [OneTimeTearDown]
        public void DisposeHost()
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
        public async Task AdminWhoCannotReadTheOwningTeam_SavesTheSettingsItGot_TheOwningTeamIsKept(
            [Values] bool owningTeamWorksAFeature)
        {
            SeedPortfolio(owningTeamWorksAFeature);
            ActAs($"{ClaimsDrivenRbacAdministrationService.PortfolioAdminGrantPrefix}{portfolioId}");

            var settings = await ReadSettings();
            var response = await Save(settings);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(settings["owningTeam"], Is.Null, "The owner is left out of what a caller who cannot read it is shown.");
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
                Assert.That(StoredOwningTeamId(), Is.EqualTo(owningTeamId));
            }
        }

        [Test]
        public async Task AdminWhoCanReadTheOwningTeam_SavesNoOwner_TheOwningTeamIsCleared(
            [Values] bool owningTeamWorksAFeature)
        {
            SeedPortfolio(owningTeamWorksAFeature);
            ActAs(ClaimsDrivenRbacAdministrationService.SystemAdminGrant);

            var settings = await ReadSettings();
            settings["owningTeam"] = null;
            var response = await Save(settings);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
                Assert.That(StoredOwningTeamId(), Is.Null);
            }
        }

        [Test]
        public async Task AdminWhoCanReadTheOwningTeam_ChangesIt_TheNewOwnerIsStored(
            [Values] bool owningTeamWorksAFeature)
        {
            SeedPortfolio(owningTeamWorksAFeature);
            ActAs(ClaimsDrivenRbacAdministrationService.SystemAdminGrant);

            var settings = await ReadSettings();
            settings["owningTeam"] = new JsonObject { ["id"] = otherTeamId, ["name"] = "Other Team" };
            var response = await Save(settings);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
                Assert.That(StoredOwningTeamId(), Is.EqualTo(otherTeamId));
            }
        }

        private void ActAs(string grant)
        {
            client.DefaultRequestHeaders.Remove(TestAuthHandler.SubjectHeader);
            client.DefaultRequestHeaders.Remove(TestAuthHandler.RolesHeader);
            client.DefaultRequestHeaders.Add(TestAuthHandler.SubjectHeader, "test-caller");
            client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, grant);
        }

        private async Task<JsonObject> ReadSettings()
        {
            return await ConcurrencyTokenTestHelpers.GetJsonObject(client, $"/api/latest/portfolios/{portfolioId}/settings");
        }

        private async Task<HttpResponseMessage> Save(JsonObject settings)
        {
            using var content = new StringContent(settings.ToJsonString(), Encoding.UTF8, "application/json");
            return await client.PutAsync($"/api/latest/portfolios/{portfolioId}", content);
        }

        private int? StoredOwningTeamId()
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            return dbContext.Portfolios.AsNoTracking().Single(p => p.Id == portfolioId).OwningTeamId;
        }

        private void SeedPortfolio(bool owningTeamWorksAFeature)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var connection = new WorkTrackingSystemConnection { Name = "Connection", WorkTrackingSystem = WorkTrackingSystems.Jira };
            var owningTeam = new Team { Name = "Owning Team", WorkTrackingSystemConnection = connection };
            var otherTeam = new Team { Name = "Other Team", WorkTrackingSystemConnection = connection };
            var portfolio = new Portfolio
            {
                Name = "Portfolio",
                WorkTrackingSystemConnection = connection,
                OwningTeam = owningTeam,
            };

            if (owningTeamWorksAFeature)
            {
                portfolio.UpdateFeatures([new Feature(owningTeam, 5) { Name = "Feature", Order = "1" }]);
            }

            dbContext.Teams.Add(otherTeam);
            dbContext.Portfolios.Add(portfolio);
            dbContext.SaveChanges();

            portfolioId = portfolio.Id;
            owningTeamId = owningTeam.Id;
            otherTeamId = otherTeam.Id;
        }
    }
}
