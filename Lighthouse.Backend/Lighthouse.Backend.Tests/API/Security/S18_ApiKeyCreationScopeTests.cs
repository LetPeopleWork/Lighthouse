using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Auth;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Lighthouse.Backend.Tests.API.Security
{
    public class S18_ApiKeyCreationScopeTests : IntegrationFixture
    {
        private const string ApiKeysRoute = "/api/latest/apikeys";
        private const string TheOwner = "the-owner";
        private const string BroaderScopeAnswer = "Cannot issue API key with broader scope than your own permissions.";
        private const string TwoWaysInScheme = "S18TwoWaysIn";
        private const int TheKeysTeam = 10;
        private const int AnotherTeam = 11;

        private static readonly ApiKeyScopeDto ViewerOfTheKeysTeam = new()
        {
            Role = UserRole.Viewer,
            ScopeType = PermissionScopeType.Team,
            ScopeId = TheKeysTeam,
        };

        private static readonly ApiKeyScopeDto[] NoEntries = [];

        private static readonly ApiKeyScopeDto[]?[] WaysToNameNoScope = [null, NoEntries];

        private static readonly ApiKeyScopeDto[] BroaderThanTheKey =
        [
            new() { Role = UserRole.Viewer, ScopeType = PermissionScopeType.Team, ScopeId = AnotherTeam },
            new() { Role = UserRole.TeamAdmin, ScopeType = PermissionScopeType.Team, ScopeId = TheKeysTeam },
            new() { Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System, ScopeId = null },
        ];

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> authenticatedHost = null!;
        private WebApplicationFactory<Program> authOffHost = null!;
        private HttpClient authenticatedClient = null!;
        private HttpClient authOffClient = null!;

        [OneTimeSetUp]
        public void StartTheHosts()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.OneTimeSetUp);

            rootFactory = new TestWebApplicationFactory<Program> { UseSchemaTemplate = true };
            authenticatedHost = WithRealKeysAndTheRealRbacService(rootFactory);
            authOffHost = rootFactory.WithWebHostBuilder(_ => { });

            authenticatedClient = authenticatedHost.CreateClient();
            authOffClient = authOffHost.CreateClient();
        }

        [SetUp]
        public void StartFromAnOwnerWhoIsASystemAdmin()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.SetUp);

            rootFactory.UseFreshDatabase();

            using var scope = authenticatedHost.Services.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var owner = new UserProfile
            {
                Subject = TheOwner,
                SubjectClaimType = "sub",
                DisplayName = "The Owner",
                Email = "owner@example.test",
            };
            database.UserProfiles.Add(owner);
            database.SaveChanges();

            database.UserPermissions.Add(new UserPermission
            {
                UserProfileId = owner.Id,
                Role = UserRole.SystemAdmin,
                ScopeType = PermissionScopeType.System,
                ScopeId = null,
            });
            database.SaveChanges();
        }

        [TearDown]
        public async Task LeaveTheHostsIdle()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.TearDown);

            await SharedHostTearDown.LeaveTheHostIdleOrFail(authenticatedHost.Services);
            await SharedHostTearDown.LeaveTheHostIdleOrFail(authOffHost.Services);
        }

        [OneTimeTearDown]
        public void StopTheHosts()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.OneTimeTearDown);

            authenticatedClient?.Dispose();
            authOffClient?.Dispose();
            authenticatedHost?.Dispose();
            authOffHost?.Dispose();
            rootFactory?.Dispose();
        }

        [TestCaseSource(nameof(WaysToNameNoScope))]
        public async Task S18_RestrictedKey_AsksForNoScope_IsRefusedAndNothingIsCreated(ApiKeyScopeDto[]? noScope)
        {
            var restrictedKey = await IssueAKeyToTheOwner(ViewerOfTheKeysTeam);
            var keysBefore = KeysOnRecord();

            using var answer = await CreateAKey(authenticatedClient, ViaTheKey(restrictedKey), noScope);

            await ThenTheBroaderScopeIsRefused(answer);
            Assert.That(KeysOnRecord(), Is.EqualTo(keysBefore));
        }

        [Test]
        public async Task S18_RestrictedKey_AsksForAScopeWithinItsRights_GetsTheKey()
        {
            var restrictedKey = await IssueAKeyToTheOwner(ViewerOfTheKeysTeam);

            using var answer = await CreateAKey(authenticatedClient, ViaTheKey(restrictedKey), [ViewerOfTheKeysTeam]);

            Assert.That(answer.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        }

        [TestCaseSource(nameof(BroaderThanTheKey))]
        public async Task S18_RestrictedKey_AsksForABroaderScope_IsRefusedAndNothingIsCreated(ApiKeyScopeDto broaderScope)
        {
            var restrictedKey = await IssueAKeyToTheOwner(ViewerOfTheKeysTeam);
            var keysBefore = KeysOnRecord();

            using var answer = await CreateAKey(authenticatedClient, ViaTheKey(restrictedKey), [broaderScope]);

            await ThenTheBroaderScopeIsRefused(answer);
            Assert.That(KeysOnRecord(), Is.EqualTo(keysBefore));
        }

        [TestCaseSource(nameof(WaysToNameNoScope))]
        public async Task S18_UnrestrictedKey_AsksForNoScope_GetsTheKey(ApiKeyScopeDto[]? noScope)
        {
            var unrestrictedKey = await IssueAKeyToTheOwner();

            using var answer = await CreateAKey(authenticatedClient, ViaTheKey(unrestrictedKey), noScope);

            Assert.That(answer.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        }

        [TestCaseSource(nameof(WaysToNameNoScope))]
        public async Task S18_SignedInUser_AsksForNoScope_GetsTheKey(ApiKeyScopeDto[]? noScope)
        {
            using var answer = await CreateAKey(authenticatedClient, SignedInAsTheOwner, noScope);

            Assert.That(answer.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        }

        [Test]
        public async Task S18_AuthenticationIsOff_ThePresentedKeyMeansNothingAndNoScopeGetsTheKey()
        {
            var restrictedKey = await IssueAKeyToTheOwner(ViewerOfTheKeysTeam);

            using var answer = await CreateAKey(authOffClient, ViaTheKey(restrictedKey), scope: null);

            Assert.That(answer.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        }

        private static void SignedInAsTheOwner(HttpRequestMessage request)
        {
            request.Headers.Add(TestAuthHandler.SubjectHeader, TheOwner);
        }

        private static Action<HttpRequestMessage> ViaTheKey(string plainTextKey)
        {
            return request => request.Headers.Add(ApiKeyAuthenticationHandler.ApiKeyHeaderName, plainTextKey);
        }

        private async Task<string> IssueAKeyToTheOwner(params ApiKeyScopeDto[] scope)
        {
            using var answer = await CreateAKey(authenticatedClient, SignedInAsTheOwner, scope);
            var body = await answer.Content.ReadAsStringAsync();

            Assert.That(answer.StatusCode, Is.EqualTo(HttpStatusCode.Created), body);

            using var document = JsonDocument.Parse(body);

            return document.RootElement.GetProperty("plainTextKey").GetString()!;
        }

        private static async Task<HttpResponseMessage> CreateAKey(
            HttpClient client,
            Action<HttpRequestMessage> asTheCaller,
            IReadOnlyList<ApiKeyScopeDto>? scope)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, ApiKeysRoute)
            {
                Content = JsonContent.Create(new CreateApiKeyRequest
                {
                    Name = "a-new-key",
                    Description = "asked for by the test",
                    Scope = scope,
                }),
            };
            asTheCaller(request);

            return await client.SendAsync(request);
        }

        private static async Task ThenTheBroaderScopeIsRefused(HttpResponseMessage answer)
        {
            var body = await answer.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
                Assert.That(body, Does.Contain(BroaderScopeAnswer));
            }
        }

        private int KeysOnRecord()
        {
            using var scope = authenticatedHost.Services.CreateScope();

            return scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().ApiKeys.Count();
        }

        private static WebApplicationFactory<Program> WithRealKeysAndTheRealRbacService(TestWebApplicationFactory<Program> root)
        {
            return root.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Authentication:Enabled"] = "true",
                        ["Authorization:Enabled"] = "true",
                    });
                });

                builder.ConfigureServices(services =>
                {
                    services.AddAuthentication(defaultOptions =>
                    {
                        defaultOptions.DefaultScheme = TwoWaysInScheme;
                        defaultOptions.DefaultAuthenticateScheme = TwoWaysInScheme;
                        defaultOptions.DefaultChallengeScheme = TwoWaysInScheme;
                        defaultOptions.DefaultForbidScheme = TwoWaysInScheme;
                    })
                    .AddPolicyScheme(TwoWaysInScheme, "Two ways in", options =>
                    {
                        options.ForwardDefaultSelector = context =>
                            context.Request.Headers.ContainsKey(ApiKeyAuthenticationHandler.ApiKeyHeaderName)
                                ? SmartAuthSchemeSelector.ApiKeyScheme
                                : TestAuthHandler.SchemeName;
                        options.ForwardChallenge = TestAuthHandler.SchemeName;
                        options.ForwardForbid = TestAuthHandler.SchemeName;
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { })
                    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(SmartAuthSchemeSelector.ApiKeyScheme, _ => { });

                    var licenseService = new Mock<ILicenseService>();
                    licenseService.Setup(s => s.CanUsePremiumFeatures()).Returns(true);
                    services.RemoveAll<ILicenseService>();
                    services.AddScoped(_ => licenseService.Object);
                });
            });
        }
    }
}
