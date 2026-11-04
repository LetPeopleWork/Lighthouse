using System.Net;
using System.Net.Http.Json;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Validation;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Lighthouse.Backend.Tests.API.Security
{
    [TestFixture]
    public class S16_ValidateEndpointAccessTests
    {
        private const string ValidateTeamRoute = "/api/latest/teams/validate";
        private const string ValidatePortfolioRoute = "/api/latest/portfolios/validate";

        private const string NoGrant = "";
        private const string ViewerOfATeam = ClaimsDrivenRbacAdministrationService.ViewerTeamGrantPrefix + "5";
        private const string ViewerOfAPortfolio = ClaimsDrivenRbacAdministrationService.ViewerPortfolioGrantPrefix + "5";
        private const string TeamAdminOfAnotherTeam = ClaimsDrivenRbacAdministrationService.TeamAdminGrantPrefix + "5";
        private const string PortfolioAdminOfAnotherPortfolio = ClaimsDrivenRbacAdministrationService.PortfolioAdminGrantPrefix + "5";

        private const string TrackersAnswerOnTheTeam = "the-trackers-answer-on-the-team";
        private const string TrackersAnswerOnThePortfolio = "the-trackers-answer-on-the-portfolio";

        private const int MoreTeamsThanTheFreeLimit = 4;
        private const int MorePortfoliosThanTheFreeLimit = 2;

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> guardedHost = null!;
        private WebApplicationFactory<Program> plainHost = null!;
        private HttpClient guardedClient = null!;
        private HttpClient plainClient = null!;

        private Mock<ILicenseService> licenseService = null!;
        private Mock<IWorkTrackingConnectorFactory> connectorFactory = null!;
        private Mock<IWorkTrackingConnector> connector = null!;

        private int connectionId;

        [OneTimeSetUp]
        public void StartTheHosts()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.OneTimeSetUp);

            rootFactory = new TestWebApplicationFactory<Program> { UseSchemaTemplate = true };

            guardedHost = WithTheDoubles(TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory));
            plainHost = WithTheDoubles(rootFactory);

            guardedClient = guardedHost.CreateClient();
            plainClient = plainHost.CreateClient();
        }

        [SetUp]
        public async Task StartFromAnInstanceWithOneConnection()
        {
            rootFactory.Clock.Reset();
            rootFactory.UseFreshDatabase();

            licenseService = new Mock<ILicenseService>();
            licenseService.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            connector = new Mock<IWorkTrackingConnector>();
            connector
                .Setup(c => c.ValidateTeamSettings(It.IsAny<Team>()))
                .ReturnsAsync(new ConnectionValidationResult { IsValid = true, Code = TrackersAnswerOnTheTeam });
            connector
                .Setup(c => c.ValidatePortfolioSettings(It.IsAny<Portfolio>()))
                .ReturnsAsync(new ConnectionValidationResult { IsValid = true, Code = TrackersAnswerOnThePortfolio });

            connectorFactory = new Mock<IWorkTrackingConnectorFactory>();
            connectorFactory
                .Setup(f => f.GetWorkTrackingConnector(It.IsAny<WorkTrackingSystems>()))
                .Returns(connector.Object);

            using var scope = guardedHost.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.EnsureCreatedAsync();

            var connections = scope.ServiceProvider.GetRequiredService<IRepository<WorkTrackingSystemConnection>>();
            var connection = new WorkTrackingSystemConnection { Name = "Connection", WorkTrackingSystem = WorkTrackingSystems.Jira };
            connections.Add(connection);
            await connections.Save();
            connectionId = connection.Id;
        }

        [TearDown]
        public async Task LeaveTheHostsIdle()
        {
            await SharedHostTearDown.LeaveTheHostIdleOrFail(guardedHost.Services);
            await SharedHostTearDown.LeaveTheHostIdleOrFail(plainHost.Services);
        }

        [OneTimeTearDown]
        public void StopTheHosts()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.OneTimeTearDown);

            guardedClient?.Dispose();
            plainClient?.Dispose();
            guardedHost?.Dispose();
            plainHost?.Dispose();
            rootFactory?.Dispose();
        }

        [TestCase(NoGrant)]
        [TestCase(ViewerOfATeam)]
        [TestCase(ViewerOfAPortfolio)]
        public async Task S16_Viewer_ValidateTeamSettings_Returns403AndNeverAsksTheTracker(string grant)
        {
            using var response = await TheCallerValidates(ValidateTeamRoute, TeamSettings(), grant);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            connector.Verify(c => c.ValidateTeamSettings(It.IsAny<Team>()), Times.Never);
        }

        [TestCase(NoGrant)]
        [TestCase(ViewerOfATeam)]
        [TestCase(ViewerOfAPortfolio)]
        public async Task S16_Viewer_ValidatePortfolioSettings_Returns403AndNeverAsksTheTracker(string grant)
        {
            using var response = await TheCallerValidates(ValidatePortfolioRoute, PortfolioSettings(), grant);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            connector.Verify(c => c.ValidatePortfolioSettings(It.IsAny<Portfolio>()), Times.Never);
        }

        [Test]
        public async Task S16_TeamAdmin_ValidateTeamSettings_GetsTheConnectorsAnswer()
        {
            using var response = await TheCallerValidates(ValidateTeamRoute, TeamSettings(), TeamAdminOfAnotherTeam);

            await ThenTheAnswerIs(response, TrackersAnswerOnTheTeam);
        }

        [Test]
        public async Task S16_TeamAdmin_ValidatePortfolioSettings_GetsTheConnectorsAnswer()
        {
            using var response = await TheCallerValidates(ValidatePortfolioRoute, PortfolioSettings(), TeamAdminOfAnotherTeam);

            await ThenTheAnswerIs(response, TrackersAnswerOnThePortfolio);
        }

        [Test]
        public async Task S16_PortfolioAdmin_ValidateTeamSettings_GetsTheConnectorsAnswer()
        {
            using var response = await TheCallerValidates(ValidateTeamRoute, TeamSettings(), PortfolioAdminOfAnotherPortfolio);

            await ThenTheAnswerIs(response, TrackersAnswerOnTheTeam);
        }

        [Test]
        public async Task S16_PortfolioAdmin_ValidatePortfolioSettings_GetsTheConnectorsAnswer()
        {
            using var response = await TheCallerValidates(ValidatePortfolioRoute, PortfolioSettings(), PortfolioAdminOfAnotherPortfolio);

            await ThenTheAnswerIs(response, TrackersAnswerOnThePortfolio);
        }

        [Test]
        public async Task S16_SystemAdmin_ValidateTeamSettings_GetsTheConnectorsAnswer()
        {
            using var response = await TheCallerValidates(ValidateTeamRoute, TeamSettings(), ClaimsDrivenRbacAdministrationService.SystemAdminGrant);

            await ThenTheAnswerIs(response, TrackersAnswerOnTheTeam);
        }

        [Test]
        public async Task S16_SystemAdmin_ValidatePortfolioSettings_GetsTheConnectorsAnswer()
        {
            using var response = await TheCallerValidates(ValidatePortfolioRoute, PortfolioSettings(), ClaimsDrivenRbacAdministrationService.SystemAdminGrant);

            await ThenTheAnswerIs(response, TrackersAnswerOnThePortfolio);
        }

        [Test]
        public async Task S16_AuthOff_Validate_BehavesAsToday()
        {
            licenseService.Setup(s => s.CanUsePremiumFeatures()).Returns(false);

            Assert.That(await ThePlainHostEnforcesRbac(), Is.False);

            using var teamResponse = await plainClient.PostAsJsonAsync(ValidateTeamRoute, TeamSettings());
            using var portfolioResponse = await plainClient.PostAsJsonAsync(ValidatePortfolioRoute, PortfolioSettings());

            await ThenTheAnswerIs(teamResponse, TrackersAnswerOnTheTeam);
            await ThenTheAnswerIs(portfolioResponse, TrackersAnswerOnThePortfolio);
        }

        [Test]
        public async Task S16_AuthOff_ValidateAboveTheFreeLimit_Returns403AndNeverAsksTheTracker()
        {
            licenseService.Setup(s => s.CanUsePremiumFeatures()).Returns(false);
            await TheInstanceHoldsMoreThanTheFreeLimitAllows();

            using var teamResponse = await plainClient.PostAsJsonAsync(ValidateTeamRoute, TeamSettings());
            using var portfolioResponse = await plainClient.PostAsJsonAsync(ValidatePortfolioRoute, PortfolioSettings());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(teamResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
                Assert.That(await teamResponse.Content.ReadAsStringAsync(), Does.Contain("Free users"));
                Assert.That(portfolioResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
                Assert.That(await portfolioResponse.Content.ReadAsStringAsync(), Does.Contain("Free users"));
            }

            connector.Verify(c => c.ValidateTeamSettings(It.IsAny<Team>()), Times.Never);
            connector.Verify(c => c.ValidatePortfolioSettings(It.IsAny<Portfolio>()), Times.Never);
        }

        private WebApplicationFactory<Program> WithTheDoubles(WebApplicationFactory<Program> host)
        {
            return host.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<ILicenseService>();
                    services.AddScoped(_ => licenseService.Object);

                    services.RemoveAll<IWorkTrackingConnectorFactory>();
                    services.AddScoped(_ => connectorFactory.Object);
                });
            });
        }

        private TeamSettingDto TeamSettings() => new()
        {
            Name = "Some Team",
            DataRetrievalValue = "the settings query",
            WorkTrackingSystemConnectionId = connectionId,
        };

        private PortfolioSettingDto PortfolioSettings() => new()
        {
            Name = "Some Portfolio",
            DataRetrievalValue = "the settings query",
            WorkTrackingSystemConnectionId = connectionId,
        };

        private async Task<HttpResponseMessage> TheCallerValidates<TSettings>(string route, TSettings settings, string grant)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, route) { Content = JsonContent.Create(settings) };
            request.Headers.Add(TestAuthHandler.SubjectHeader, "the-caller");
            request.Headers.Add(TestAuthHandler.RolesHeader, grant);

            return await guardedClient.SendAsync(request);
        }

        private static async Task ThenTheAnswerIs(HttpResponseMessage response, string trackersAnswer)
        {
            var body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(body, Does.Contain(trackersAnswer));
            }
        }

        private async Task<bool> ThePlainHostEnforcesRbac()
        {
            using var scope = plainHost.Services.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<IRbacAdministrationService>().IsRbacEnforcedAsync();
        }

        private async Task TheInstanceHoldsMoreThanTheFreeLimitAllows()
        {
            using var scope = plainHost.Services.CreateScope();
            var teams = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();
            var portfolios = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
            var connection = scope.ServiceProvider.GetRequiredService<IRepository<WorkTrackingSystemConnection>>().GetById(connectionId)!;

            for (var number = 1; number <= MoreTeamsThanTheFreeLimit; number++)
            {
                teams.Add(new Team { Name = $"Team {number}", WorkTrackingSystemConnection = connection });
            }

            for (var number = 1; number <= MorePortfoliosThanTheFreeLimit; number++)
            {
                portfolios.Add(new Portfolio { Name = $"Portfolio {number}", WorkTrackingSystemConnection = connection });
            }

            await teams.Save();
            await portfolios.Save();
        }
    }
}
