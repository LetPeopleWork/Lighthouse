using System.Net;
using System.Text.Json.Nodes;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Lighthouse.Backend.Tests.API.Security
{
    [TestFixture]
    public class S17_LicenseDetailsTests
    {
        private const string LicenseRoute = "/api/latest/license";
        private const string ImportRoute = "/api/latest/license/import";

        private const string NoGrant = "";
        private const string ViewerOfATeam = ClaimsDrivenRbacAdministrationService.ViewerTeamGrantPrefix + "5";
        private const string ViewerOfAPortfolio = ClaimsDrivenRbacAdministrationService.ViewerPortfolioGrantPrefix + "5";
        private const string TeamAdmin = ClaimsDrivenRbacAdministrationService.TeamAdminGrantPrefix + "5";
        private const string PortfolioAdmin = ClaimsDrivenRbacAdministrationService.PortfolioAdminGrantPrefix + "5";

        private const string Licensee = "Jane Licensee";
        private const string LicenseeEmail = "jane@licensee.example";
        private const string LicenseeOrganization = "Licensee Ltd";
        private const string LicenseNumber = "LN-2026-0042";

        private static readonly DateTime ExpiryDate = new(2031, 6, 30, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime ValidFrom = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);

        private static readonly string[] WhatEveryoneMaySee =
            ["hasLicense", "isValid", "expiryDate", "validFrom", "canUsePremiumFeatures"];

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> guardedHost = null!;
        private WebApplicationFactory<Program> rbacOffHost = null!;
        private WebApplicationFactory<Program> plainHost = null!;
        private HttpClient guardedClient = null!;
        private HttpClient rbacOffClient = null!;
        private HttpClient plainClient = null!;

        private Mock<ILicenseService> licenseService = null!;

        [OneTimeSetUp]
        public async Task StartTheHosts()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.OneTimeSetUp);

            rootFactory = new TestWebApplicationFactory<Program> { UseSchemaTemplate = true };

            var withAuthentication = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory);

            guardedHost = WithTheDoubles(withAuthentication);
            rbacOffHost = WithTheDoubles(WithoutRbac(withAuthentication));
            plainHost = WithTheDoubles(rootFactory);

            guardedClient = guardedHost.CreateClient();
            rbacOffClient = rbacOffHost.CreateClient();
            plainClient = plainHost.CreateClient();

            using var scope = guardedHost.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.EnsureCreatedAsync();
        }

        [SetUp]
        public void StartFromAValidLicense()
        {
            var license = new LicenseInformation
            {
                Name = Licensee,
                Email = LicenseeEmail,
                Organization = LicenseeOrganization,
                LicenseNumber = LicenseNumber,
                ExpiryDate = ExpiryDate,
                ValidFrom = ValidFrom,
                Signature = "signature",
            };

            licenseService = new Mock<ILicenseService>();
            licenseService.Setup(s => s.GetLicenseData()).Returns((license, true));
            licenseService.Setup(s => s.CanUsePremiumFeatures()).Returns(true);
            licenseService.Setup(s => s.ImportLicense(It.IsAny<string>())).ReturnsAsync(license);
        }

        [TearDown]
        public async Task LeaveTheHostsIdle()
        {
            await SharedHostTearDown.LeaveTheHostIdleOrFail(guardedHost.Services);
            await SharedHostTearDown.LeaveTheHostIdleOrFail(rbacOffHost.Services);
            await SharedHostTearDown.LeaveTheHostIdleOrFail(plainHost.Services);
        }

        [OneTimeTearDown]
        public void StopTheHosts()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.OneTimeTearDown);

            guardedClient?.Dispose();
            rbacOffClient?.Dispose();
            plainClient?.Dispose();
            guardedHost?.Dispose();
            rbacOffHost?.Dispose();
            plainHost?.Dispose();
            rootFactory?.Dispose();
        }

        [TestCase(NoGrant)]
        [TestCase(ViewerOfATeam)]
        [TestCase(ViewerOfAPortfolio)]
        [TestCase(TeamAdmin)]
        [TestCase(PortfolioAdmin)]
        public async Task S17_Viewer_GetLicense_IsToldNothingAboutTheLicensee(string grant)
        {
            var answer = await AnswerTo(guardedClient, Reading(grant));

            Assert.That(answer.Select(field => field.Key), Is.EquivalentTo(WhatEveryoneMaySee));
        }

        [TestCase(NoGrant)]
        [TestCase(ViewerOfATeam)]
        [TestCase(ViewerOfAPortfolio)]
        [TestCase(TeamAdmin)]
        [TestCase(PortfolioAdmin)]
        public async Task S17_Viewer_GetLicense_StillGetsTheStateOfTheLicense(string grant)
        {
            var answer = await AnswerTo(guardedClient, Reading(grant));

            ThenTheStateOfTheLicenseIsShown(answer);
        }

        [Test]
        public async Task S17_SystemAdmin_GetLicense_SeesEveryField()
        {
            var answer = await AnswerTo(guardedClient, Reading(ClaimsDrivenRbacAdministrationService.SystemAdminGrant));

            ThenEveryFieldIsShown(answer);
        }

        [Test]
        public async Task S17_AuthOff_GetLicense_SeesEveryField()
        {
            Assert.That(await EnforcesRbac(plainHost), Is.False);

            var answer = await AnswerTo(plainClient, new HttpRequestMessage(HttpMethod.Get, LicenseRoute));

            ThenEveryFieldIsShown(answer);
        }

        [Test]
        public async Task S17_RbacOff_GetLicense_SeesEveryField()
        {
            Assert.That(await EnforcesRbac(rbacOffHost), Is.False);

            var answer = await AnswerTo(rbacOffClient, Reading(NoGrant));

            ThenEveryFieldIsShown(answer);
        }

        [Test]
        public async Task S17_SystemAdmin_ImportLicense_SeesEveryField()
        {
            var answer = await AnswerTo(guardedClient, Importing(ClaimsDrivenRbacAdministrationService.SystemAdminGrant));

            ThenEveryFieldIsShown(answer);
        }

        [Test]
        public async Task S17_AuthOff_ImportLicense_SeesEveryField()
        {
            var answer = await AnswerTo(plainClient, Importing(NoGrant));

            ThenEveryFieldIsShown(answer);
        }

        [Test]
        public async Task S17_RbacOff_ImportLicense_SeesEveryField()
        {
            var answer = await AnswerTo(rbacOffClient, Importing(NoGrant));

            ThenEveryFieldIsShown(answer);
        }

        private WebApplicationFactory<Program> WithTheDoubles(WebApplicationFactory<Program> host)
        {
            return host.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<ILicenseService>();
                    services.AddScoped(_ => licenseService.Object);
                });
            });
        }

        // Signed-in callers on an instance that does not enforce RBAC. The double the authenticated host
        // installs always enforces, so the real service goes back in to give the real answer.
        private static WebApplicationFactory<Program> WithoutRbac(WebApplicationFactory<Program> host)
        {
            return host.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Authorization:Enabled"] = "false",
                    });
                });

                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IRbacAdministrationService>();
                    services.AddScoped<IRbacAdministrationService, RbacAdministrationService>();
                });
            });
        }

        private static HttpRequestMessage Reading(string grant)
        {
            return AsTheCaller(new HttpRequestMessage(HttpMethod.Get, LicenseRoute), grant);
        }

        private static HttpRequestMessage Importing(string grant)
        {
            var file = new MultipartFormDataContent { { new StringContent("{}"), "file", "license.json" } };

            return AsTheCaller(new HttpRequestMessage(HttpMethod.Post, ImportRoute) { Content = file }, grant);
        }

        private static HttpRequestMessage AsTheCaller(HttpRequestMessage request, string grant)
        {
            request.Headers.Add(TestAuthHandler.SubjectHeader, "the-caller");
            request.Headers.Add(TestAuthHandler.RolesHeader, grant);

            return request;
        }

        private static async Task<JsonObject> AnswerTo(HttpClient client, HttpRequestMessage request)
        {
            using (request)
            {
                using var response = await client.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

                return JsonNode.Parse(body)!.AsObject();
            }
        }

        private static async Task<bool> EnforcesRbac(WebApplicationFactory<Program> host)
        {
            using var scope = host.Services.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<IRbacAdministrationService>().IsRbacEnforcedAsync();
        }

        private static void ThenEveryFieldIsShown(JsonObject answer)
        {
            ThenTheStateOfTheLicenseIsShown(answer);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer["name"]?.GetValue<string>(), Is.EqualTo(Licensee));
                Assert.That(answer["email"]?.GetValue<string>(), Is.EqualTo(LicenseeEmail));
                Assert.That(answer["organization"]?.GetValue<string>(), Is.EqualTo(LicenseeOrganization));
                Assert.That(answer["licenseNumber"]?.GetValue<string>(), Is.EqualTo(LicenseNumber));
            }
        }

        private static void ThenTheStateOfTheLicenseIsShown(JsonObject answer)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer["hasLicense"]?.GetValue<bool>(), Is.True);
                Assert.That(answer["isValid"]?.GetValue<bool>(), Is.True);
                Assert.That(answer["canUsePremiumFeatures"]?.GetValue<bool>(), Is.True);
                Assert.That(answer["expiryDate"]?.GetValue<DateTime>(), Is.EqualTo(ExpiryDate));
                Assert.That(answer["validFrom"]?.GetValue<DateTime>(), Is.EqualTo(ValidFrom));
            }
        }
    }
}
