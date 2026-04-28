using Lighthouse.Backend.API;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace Lighthouse.Backend.Tests.API
{
    public class ApiKeyControllerTest
    {
        private Mock<IApiKeyService> apiKeyServiceMock;

        [SetUp]
        public void Setup()
        {
            apiKeyServiceMock = new Mock<IApiKeyService>();
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
                CreatedByUser = "alice",
                CreatedAt = DateTime.UtcNow,
                PlainTextKey = "lh_plaintext_key"
            };

            apiKeyServiceMock.Setup(x => x.CreateApiKeyAsync("my-key", "desc", "alice"))
                .ReturnsAsync(creationResult);

            var subject = CreateSubjectWithUser("alice");
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
            var subject = CreateSubjectWithUser("alice");
            var request = new CreateApiKeyRequest { Name = "", Description = "desc" };

            var result = await subject.CreateApiKey(request);

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task CreateApiKey_CallsServiceWithCurrentUser()
        {
            var creationResult = new ApiKeyCreationResult { Name = "k", PlainTextKey = "x" };
            apiKeyServiceMock.Setup(x => x.CreateApiKeyAsync(It.IsAny<string>(), It.IsAny<string>(), "bob"))
                .ReturnsAsync(creationResult);

            var subject = CreateSubjectWithUser("bob");
            await subject.CreateApiKey(new CreateApiKeyRequest { Name = "k", Description = "" });

            apiKeyServiceMock.Verify(x => x.CreateApiKeyAsync("k", "", "bob"), Times.Once);
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
            apiKeyServiceMock.Setup(x => x.GetAllApiKeys()).Returns(keys);

            var subject = CreateSubjectWithUser("alice");
            var result = subject.GetApiKeys() as OkObjectResult;

            Assert.That(result?.Value, Is.SameAs(keys));
        }

        // --- Delete ---

        [Test]
        public void DeleteApiKey_ExistingId_ReturnsNoContent()
        {
            apiKeyServiceMock.Setup(x => x.DeleteApiKey(5)).Returns(true);

            var subject = CreateSubjectWithUser("alice");
            var result = subject.DeleteApiKey(5);

            Assert.That(result, Is.InstanceOf<NoContentResult>());
        }

        [Test]
        public void DeleteApiKey_NonExistentId_ReturnsNotFound()
        {
            apiKeyServiceMock.Setup(x => x.DeleteApiKey(99)).Returns(false);

            var subject = CreateSubjectWithUser("alice");
            var result = subject.DeleteApiKey(99);

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        // --- Helpers ---

        private ApiKeyController CreateSubjectWithUser(string userName)
        {
            var claims = new[] { new Claim(ClaimTypes.Name, userName) };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);

            var controller = new ApiKeyController(apiKeyServiceMock.Object)
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
