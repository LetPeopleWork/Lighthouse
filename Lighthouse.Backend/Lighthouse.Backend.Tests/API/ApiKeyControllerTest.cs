using Lighthouse.Backend.API;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace Lighthouse.Backend.Tests.API
{
    public class ApiKeyControllerTest
    {
        private const string BroaderScopeRefusal = "Cannot issue API key with broader scope than your own permissions.";

        private const string InvalidScopeEntryMessage = "API key scope entry is invalid.";

        private Mock<IApiKeyService> apiKeyServiceMock;
        private Mock<IRbacAdministrationService> rbacAdministrationServiceMock;

        [SetUp]
        public void Setup()
        {
            apiKeyServiceMock = new Mock<IApiKeyService>();
            rbacAdministrationServiceMock = new Mock<IRbacAdministrationService>();
            rbacAdministrationServiceMock
                .Setup(s => s.CanSatisfyRequirementAsync(
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<RbacGuardRequirement>(),
                    It.IsAny<int?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
        }

        // --- Create ---

        [Test]
        public async Task CreateApiKey_ValidRequest_ReturnsCreatedWithResult()
        {
            var creationResult = new ApiKeyCreationResult
            {
                Id = 1,
                Name = "my-key",
                Description = "desc",
                CreatedAt = DateTime.UtcNow,
                PlainTextKey = "lh_plaintext_key"
            };

            apiKeyServiceMock.Setup(x => x.CreateApiKeyAsync("my-key", "desc", "alice", "alice-subject", It.IsAny<IReadOnlyList<ApiKeyScopeDto>?>()))
                .ReturnsAsync(creationResult);

            var subject = CreateSubjectWithUser("alice", "alice-subject");
            var request = new CreateApiKeyRequest { Name = "my-key", Description = "desc" };

            var result = await subject.CreateApiKey(request) as ObjectResult;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result!.StatusCode, Is.EqualTo(StatusCodes.Status201Created));
                Assert.That(result.Value, Is.SameAs(creationResult));
            }
        }

        [Test]
        public async Task CreateApiKey_EmptyName_ReturnsBadRequest()
        {
            var subject = CreateSubjectWithUser("alice", "alice-subject");
            var request = new CreateApiKeyRequest { Name = "", Description = "desc" };

            var result = await subject.CreateApiKey(request);

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task CreateApiKey_CallsServiceWithCurrentUser()
        {
            var creationResult = new ApiKeyCreationResult { Name = "k", PlainTextKey = "x" };
            apiKeyServiceMock.Setup(x => x.CreateApiKeyAsync(It.IsAny<string>(), It.IsAny<string>(), "bob", "bob-subject", It.IsAny<IReadOnlyList<ApiKeyScopeDto>?>()))
                .ReturnsAsync(creationResult);

            var subject = CreateSubjectWithUser("bob", "bob-subject");
            await subject.CreateApiKey(new CreateApiKeyRequest { Name = "k", Description = "" });

            apiKeyServiceMock.Verify(x => x.CreateApiKeyAsync("k", "", "bob", "bob-subject", null), Times.Once);
        }

        [Test]
        public async Task CreateApiKey_MissingStableSubject_ReturnsForbid()
        {
            var subject = CreateSubjectWithUser("bob", null);

            var result = await subject.CreateApiKey(new CreateApiKeyRequest { Name = "k", Description = "" });

            Assert.That(result, Is.InstanceOf<ForbidResult>());
        }

        [TestCaseSource(nameof(RequestsNamingNoScope))]
        public async Task CreateApiKey_RestrictedKeyNamesNoScope_RefusesAndCreatesNothing(IReadOnlyList<ApiKeyScopeDto>? scope)
        {
            TheCallerIsOnARestrictedKey();
            var subject = CreateSubjectWithUser("alice", "alice-subject");

            var result = await subject.CreateApiKey(new CreateApiKeyRequest { Name = "k", Scope = scope });

            AssertRefusedForBroaderScope(result);
            AssertNoKeyWasCreated();
        }

        [TestCaseSource(nameof(RequestsNamingNoScope))]
        public async Task CreateApiKey_UnrestrictedCallerNamesNoScope_CreatesTheKey(IReadOnlyList<ApiKeyScopeDto>? scope)
        {
            var issuedKey = TheServiceIssuesAKey();
            var subject = CreateSubjectWithUser("alice", "alice-subject");

            var result = await subject.CreateApiKey(new CreateApiKeyRequest { Name = "k", Scope = scope });

            AssertCreated(result, issuedKey);
            apiKeyServiceMock.Verify(x => x.CreateApiKeyAsync("k", "", "alice", "alice-subject", scope), Times.Once);
        }

        [Test]
        public async Task CreateApiKey_RestrictedKeyNamesAScopeItHolds_CreatesTheKey()
        {
            TheCallerIsOnARestrictedKey();
            var issuedKey = TheServiceIssuesAKey();
            var scope = new List<ApiKeyScopeDto> { AScopeEntry(UserRole.TeamAdmin, PermissionScopeType.Team, 5) };
            var subject = CreateSubjectWithUser("alice", "alice-subject");

            var result = await subject.CreateApiKey(new CreateApiKeyRequest { Name = "k", Scope = scope });

            AssertCreated(result, issuedKey);
            apiKeyServiceMock.Verify(x => x.CreateApiKeyAsync("k", "", "alice", "alice-subject", scope), Times.Once);
        }

        [TestCase(UserRole.SystemAdmin, PermissionScopeType.System, null, RbacGuardRequirement.SystemAdmin)]
        [TestCase(UserRole.TeamAdmin, PermissionScopeType.Team, 5, RbacGuardRequirement.TeamWrite)]
        [TestCase(UserRole.PortfolioAdmin, PermissionScopeType.Portfolio, 5, RbacGuardRequirement.PortfolioWrite)]
        [TestCase(UserRole.Viewer, PermissionScopeType.Team, 5, RbacGuardRequirement.TeamRead)]
        [TestCase(UserRole.Viewer, PermissionScopeType.Portfolio, 5, RbacGuardRequirement.PortfolioRead)]
        public async Task CreateApiKey_ScopeEntry_AsksWhetherTheCallerMeetsTheRequirementItMapsTo(
            UserRole role,
            PermissionScopeType scopeType,
            int? scopeId,
            RbacGuardRequirement expectedRequirement)
        {
            var issuedKey = TheServiceIssuesAKey();
            var subject = CreateSubjectWithUser("alice", "alice-subject");

            var result = await subject.CreateApiKey(new CreateApiKeyRequest { Name = "k", Scope = [AScopeEntry(role, scopeType, scopeId)] });

            AssertCreated(result, issuedKey);
            rbacAdministrationServiceMock.Verify(
                s => s.CanSatisfyRequirementAsync(subject.User, expectedRequirement, scopeId, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestCase(UserRole.SystemAdmin, PermissionScopeType.Team, 5)]
        [TestCase(UserRole.SystemAdmin, PermissionScopeType.Portfolio, 5)]
        [TestCase(UserRole.TeamAdmin, PermissionScopeType.System, 5)]
        [TestCase(UserRole.TeamAdmin, PermissionScopeType.Portfolio, 5)]
        [TestCase(UserRole.TeamAdmin, PermissionScopeType.Team, null)]
        [TestCase(UserRole.PortfolioAdmin, PermissionScopeType.System, 5)]
        [TestCase(UserRole.PortfolioAdmin, PermissionScopeType.Team, 5)]
        [TestCase(UserRole.PortfolioAdmin, PermissionScopeType.Portfolio, null)]
        [TestCase(UserRole.Viewer, PermissionScopeType.System, 5)]
        [TestCase(UserRole.Viewer, PermissionScopeType.Team, null)]
        [TestCase(UserRole.Viewer, PermissionScopeType.Portfolio, null)]
        public async Task CreateApiKey_ScopeEntryMapsToNoRequirement_AnswersBadRequestAndCreatesNothing(
            UserRole role,
            PermissionScopeType scopeType,
            int? scopeId)
        {
            var subject = CreateSubjectWithUser("alice", "alice-subject");

            var result = await subject.CreateApiKey(new CreateApiKeyRequest { Name = "k", Scope = [AScopeEntry(role, scopeType, scopeId)] });

            Assert.That((result as BadRequestObjectResult)?.Value, Is.EqualTo(InvalidScopeEntryMessage));
            rbacAdministrationServiceMock.Verify(
                s => s.CanSatisfyRequirementAsync(
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<RbacGuardRequirement>(),
                    It.IsAny<int?>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
            AssertNoKeyWasCreated();
        }

        [Test]
        public async Task CreateApiKey_CallerCannotGrantOneOfTheScopes_RefusesAndCreatesNothing()
        {
            const int teamTheCallerCannotGrant = 6;
            rbacAdministrationServiceMock
                .Setup(s => s.CanSatisfyRequirementAsync(
                    It.IsAny<ClaimsPrincipal>(),
                    RbacGuardRequirement.TeamWrite,
                    teamTheCallerCannotGrant,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            var subject = CreateSubjectWithUser("alice", "alice-subject");

            var result = await subject.CreateApiKey(new CreateApiKeyRequest
            {
                Name = "k",
                Scope =
                [
                    AScopeEntry(UserRole.TeamAdmin, PermissionScopeType.Team, 5),
                    AScopeEntry(UserRole.TeamAdmin, PermissionScopeType.Team, teamTheCallerCannotGrant),
                ],
            });

            AssertRefusedForBroaderScope(result);
            AssertNoKeyWasCreated();
        }

        // --- List ---

        [Test]
        public void GetApiKeys_ReturnsAllKeys()
        {
            var keys = new List<ApiKeyInfo>
            {
                new ApiKeyInfo { Id = 1, Name = "key1" },
                new ApiKeyInfo { Id = 2, Name = "key2" },
            };
            apiKeyServiceMock.Setup(x => x.GetApiKeysByOwnerSubject("alice-subject")).Returns(keys);

            var subject = CreateSubjectWithUser("alice", "alice-subject");
            var result = subject.GetApiKeys() as OkObjectResult;

            Assert.That(result?.Value, Is.SameAs(keys));
        }

        [Test]
        public void GetApiKeys_CallsServiceWithCurrentUser()
        {
            apiKeyServiceMock.Setup(x => x.GetApiKeysByOwnerSubject("alice-subject")).Returns([]);

            var subject = CreateSubjectWithUser("alice", "alice-subject");

            subject.GetApiKeys();

            apiKeyServiceMock.Verify(x => x.GetApiKeysByOwnerSubject("alice-subject"), Times.Once);
        }

        [Test]
        public void GetApiKeys_MissingStableSubject_ReturnsForbid()
        {
            var subject = CreateSubjectWithUser("alice", null);

            var result = subject.GetApiKeys();

            Assert.That(result, Is.InstanceOf<ForbidResult>());
        }

        // --- Delete ---

        [Test]
        public async Task DeleteApiKey_ExistingId_ReturnsNoContent()
        {
            apiKeyServiceMock.Setup(x => x.DeleteApiKey(5, "alice-subject")).ReturnsAsync(true);

            var subject = CreateSubjectWithUser("alice", "alice-subject");
            var result = await subject.DeleteApiKey(5);

            Assert.That(result, Is.InstanceOf<NoContentResult>());
        }

        [Test]
        public async Task DeleteApiKey_NonExistentId_ReturnsNotFound()
        {
            apiKeyServiceMock.Setup(x => x.DeleteApiKey(99, "alice-subject")).ReturnsAsync(false);

            var subject = CreateSubjectWithUser("alice", "alice-subject");
            var result = await subject.DeleteApiKey(99);

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task DeleteApiKey_CallsServiceWithCurrentUser()
        {
            apiKeyServiceMock.Setup(x => x.DeleteApiKey(7, "alice-subject")).ReturnsAsync(true);

            var subject = CreateSubjectWithUser("alice", "alice-subject");

            await subject.DeleteApiKey(7);

            apiKeyServiceMock.Verify(x => x.DeleteApiKey(7, "alice-subject"), Times.Once);
        }

        [Test]
        public async Task DeleteApiKey_MissingStableSubject_ReturnsForbid()
        {
            var subject = CreateSubjectWithUser("alice", null);

            var result = await subject.DeleteApiKey(1);

            Assert.That(result, Is.InstanceOf<ForbidResult>());
        }

        // --- Helpers ---

        private static IEnumerable<TestCaseData> RequestsNamingNoScope()
        {
            yield return new TestCaseData((IReadOnlyList<ApiKeyScopeDto>?)null).SetArgDisplayNames("scope left out");
            yield return new TestCaseData(new List<ApiKeyScopeDto>()).SetArgDisplayNames("empty scope");
        }

        private static ApiKeyScopeDto AScopeEntry(UserRole role, PermissionScopeType scopeType, int? scopeId)
            => new() { Role = role, ScopeType = scopeType, ScopeId = scopeId };

        private void TheCallerIsOnARestrictedKey()
        {
            rbacAdministrationServiceMock
                .Setup(s => s.IsRestrictedApiKeyAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
        }

        private ApiKeyCreationResult TheServiceIssuesAKey()
        {
            var issuedKey = new ApiKeyCreationResult { Name = "k", PlainTextKey = "x" };
            apiKeyServiceMock
                .Setup(x => x.CreateApiKeyAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<ApiKeyScopeDto>?>()))
                .ReturnsAsync(issuedKey);

            return issuedKey;
        }

        private void AssertNoKeyWasCreated()
        {
            apiKeyServiceMock.Verify(
                x => x.CreateApiKeyAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<ApiKeyScopeDto>?>()),
                Times.Never);
        }

        private static void AssertCreated(IActionResult result, ApiKeyCreationResult issuedKey)
        {
            var created = result as ObjectResult;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(created?.StatusCode, Is.EqualTo(StatusCodes.Status201Created));
                Assert.That(created?.Value, Is.SameAs(issuedKey));
            }
        }

        private static void AssertRefusedForBroaderScope(IActionResult result)
        {
            var refusal = result as ObjectResult;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refusal?.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
                Assert.That(refusal?.Value, Is.EqualTo(BroaderScopeRefusal));
            }
        }

        private ApiKeyController CreateSubjectWithUser(string userName, string? subject)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userName),
            };

            if (!string.IsNullOrWhiteSpace(subject))
            {
                claims.Add(new Claim("sub", subject));
            }

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);

            var controller = new ApiKeyController(apiKeyServiceMock.Object, rbacAdministrationServiceMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = principal
                    }
                }
            };

            return controller;
        }
    }
}
