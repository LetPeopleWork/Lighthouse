using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.API.Integration
{
    /// <summary>
    /// Walking-skeleton HTTP smoke for <see cref="Lighthouse.Backend.API.ApiKeyController"/>:
    /// asserts the full POST/GET/DELETE pipeline serialises and returns the expected
    /// status codes over real HTTP under the test environment's disabled-auth handler.
    /// Per-user scoping and 401/403 paths are pinned at unit/controller level in
    /// <c>ApiKeyServiceTest.cs</c> and <c>ApiKeyControllerTest.cs</c>; per-user HTTP
    /// integration is deferred pending a controllable test auth scheme.
    /// </summary>
    public class ApiKeyControllerHttpSmokeTests() : IntegrationTestBase(new TestWebApplicationFactory<Program>())
    {
        [Test]
        public async Task ApiKeyCrudRoundTrip_OverHttp_Succeeds()
        {
            var keyName = $"smoke-key-{Guid.NewGuid():N}";
            var createRequest = new CreateApiKeyRequest { Name = keyName, Description = "smoke" };

            var createResponse = await Client.PostAsJsonAsync("/api/latest/apikeys", createRequest);
            var createBody = await createResponse.Content.ReadAsStringAsync();
            Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created), $"Create body: {createBody}");

            var creationResult = await createResponse.Content.ReadFromJsonAsync<ApiKeyCreationResult>();
            
            using (Assert.EnterMultipleScope())
            {
                Assert.That(creationResult, Is.Not.Null);
                Assert.That(creationResult!.PlainTextKey, Is.Not.Empty);
                Assert.That(creationResult.Id, Is.GreaterThan(0));
            }


            var listAfterCreate = await Client.GetAsync("/api/latest/apikeys");
            Assert.That(listAfterCreate.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var keysAfterCreate = await listAfterCreate.Content.ReadFromJsonAsync<IEnumerable<ApiKeyInfo>>();
            Assert.That(keysAfterCreate, Is.Not.Null);
            var createdRow = keysAfterCreate!.SingleOrDefault(k => k.Name == keyName);
            
            using (Assert.EnterMultipleScope())
            {
                Assert.That(createdRow, Is.Not.Null, "Posted key should be visible in subsequent GET.");
                Assert.That(createdRow!.Id, Is.EqualTo(creationResult.Id));
            }


            var deleteResponse = await Client.DeleteAsync($"/api/latest/apikeys/{creationResult.Id}");
            Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

            var listAfterDelete = await Client.GetAsync("/api/latest/apikeys");
            Assert.That(listAfterDelete.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var keysAfterDelete = await listAfterDelete.Content.ReadFromJsonAsync<IEnumerable<ApiKeyInfo>>();
            Assert.That(keysAfterDelete, Is.Not.Null);
            Assert.That(keysAfterDelete!.Any(k => k.Name == keyName), Is.False, "Deleted key should not appear in GET.");
        }

        [Test]
        public async Task GetApiKeys_OverHttp_ReturnsScopesArrayAndOmitsCreatedByUser()
        {
            const string ownerSubject = "lighthouse|auth-disabled";
            const int portfolioScopeId = 7;
            using (var seedScope = ServiceProvider.CreateScope())
            {
                var db = seedScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                var seededKey = new ApiKey
                {
                    Name = $"scopes-key-{Guid.NewGuid():N}",
                    Description = "scopes-fixture",
                    CreatedByUser = "Authentication Disabled",
                    OwnerSubject = ownerSubject,
                    CreatedAt = DateTime.UtcNow,
                    KeyHash = "irrelevant-hash",
                    Salt = "irrelevant-salt",
                };
                db.ApiKeys.Add(seededKey);
                await db.SaveChangesAsync();

                db.ApiKeyPermissions.Add(new ApiKeyPermission
                {
                    ApiKeyId = seededKey.Id,
                    Role = UserRole.PortfolioAdmin,
                    ScopeType = PermissionScopeType.Portfolio,
                    ScopeId = portfolioScopeId,
                    GrantedAt = DateTime.UtcNow,
                });
                await db.SaveChangesAsync();
            }

            var response = await Client.GetAsync("/api/latest/apikeys");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            await using var bodyStream = await response.Content.ReadAsStreamAsync();
            using var document = await JsonDocument.ParseAsync(bodyStream);
            var root = document.RootElement;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(root.ValueKind, Is.EqualTo(JsonValueKind.Array));
                Assert.That(root.GetArrayLength(), Is.EqualTo(1));
            }

            var element = root[0];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(element.TryGetProperty("createdByUser", out _), Is.False,
                    "createdByUser should be removed from the response DTO.");
                Assert.That(element.TryGetProperty("scopes", out var scopesProperty), Is.True,
                    "scopes property should be present on the response DTO.");
                Assert.That(scopesProperty.ValueKind, Is.EqualTo(JsonValueKind.Array));
                Assert.That(scopesProperty.GetArrayLength(), Is.EqualTo(1));

                var scopeElement = scopesProperty[0];
                Assert.That(scopeElement.GetProperty("role").GetString(), Is.EqualTo(nameof(UserRole.PortfolioAdmin)));
                Assert.That(scopeElement.GetProperty("scopeType").GetString(), Is.EqualTo(nameof(PermissionScopeType.Portfolio)));
                Assert.That(scopeElement.GetProperty("scopeId").GetInt32(), Is.EqualTo(portfolioScopeId));
            }
        }

        [Test]
        public async Task CreateApiKey_WhitespaceName_OverHttp_Returns400()
        {
            var request = new CreateApiKeyRequest { Name = "   ", Description = "any" };

            var response = await Client.PostAsJsonAsync("/api/latest/apikeys", request);

            var body = await response.Content.ReadAsStringAsync();
            
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Body: {body}");
                Assert.That(body, Does.Contain("name").IgnoreCase, $"Expected body to mention 'name'; got: {body}");
            }

        }
    }
}
