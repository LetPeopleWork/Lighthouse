using Lighthouse.Backend.Services.Implementation.Auth;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Encodings.Web;

namespace Lighthouse.Backend.Tests.Services.Implementation.Auth
{
    public class ApiKeyAuthenticationHandlerTest
    {
        private Mock<IApiKeyService> apiKeyServiceMock;
        private Mock<IOptionsMonitor<AuthenticationSchemeOptions>> optionsMock;
        private Mock<ILoggerFactory> loggerFactoryMock;
        private DefaultHttpContext httpContext;

        private const string SchemeName = "ApiKey";

        [SetUp]
        public void Setup()
        {
            apiKeyServiceMock = new Mock<IApiKeyService>();
            optionsMock = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
            loggerFactoryMock = new Mock<ILoggerFactory>();

            optionsMock.Setup(x => x.Get(It.IsAny<string>()))
                .Returns(new AuthenticationSchemeOptions());

            loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>()))
                .Returns(Mock.Of<ILogger>());

            httpContext = new DefaultHttpContext();
        }

        [Test]
        public async Task HandleAuthenticateAsync_NoApiKeyHeader_ReturnsNoResult()
        {
            var handler = await CreateAndInitializeHandler();

            var result = await handler.AuthenticateAsync();

            Assert.That(result.None, Is.True);
        }

        [Test]
        public async Task HandleAuthenticateAsync_EmptyApiKeyHeader_ReturnsFail()
        {
            httpContext.Request.Headers["X-Api-Key"] = "";
            var handler = await CreateAndInitializeHandler();

            var result = await handler.AuthenticateAsync();

            Assert.That(result.Failure, Is.Not.Null);
        }

        [Test]
        public async Task HandleAuthenticateAsync_InvalidApiKey_ReturnsFail()
        {
            httpContext.Request.Headers["X-Api-Key"] = "invalid-key";
            apiKeyServiceMock.Setup(x => x.ValidateApiKeyAsync("invalid-key"))
                .ReturnsAsync(false);

            var handler = await CreateAndInitializeHandler();

            var result = await handler.AuthenticateAsync();

            Assert.That(result.Failure, Is.Not.Null);
        }

        [Test]
        public async Task HandleAuthenticateAsync_ValidApiKey_ReturnsSuccess()
        {
            httpContext.Request.Headers["X-Api-Key"] = "valid-key";
            apiKeyServiceMock.Setup(x => x.ValidateApiKeyAsync("valid-key"))
                .ReturnsAsync(true);

            var handler = await CreateAndInitializeHandler();

            var result = await handler.AuthenticateAsync();

            Assert.That(result.Succeeded, Is.True);
        }

        [Test]
        public async Task HandleAuthenticateAsync_ValidApiKey_PrincipalHasApiKeyAuthMethodClaim()
        {
            httpContext.Request.Headers["X-Api-Key"] = "valid-key";
            apiKeyServiceMock.Setup(x => x.ValidateApiKeyAsync("valid-key"))
                .ReturnsAsync(true);

            var handler = await CreateAndInitializeHandler();

            var result = await handler.AuthenticateAsync();

            var authMethodClaim = result.Principal?.FindFirst("auth_method")?.Value;
            Assert.That(authMethodClaim, Is.EqualTo("api-key"));
        }

        [Test]
        public async Task HandleAuthenticateAsync_ValidApiKey_PrincipalHasApiKeyUserName()
        {
            httpContext.Request.Headers["X-Api-Key"] = "valid-key";
            apiKeyServiceMock.Setup(x => x.ValidateApiKeyAsync("valid-key"))
                .ReturnsAsync(true);

            var handler = await CreateAndInitializeHandler();

            var result = await handler.AuthenticateAsync();

            Assert.That(result.Principal?.Identity?.Name, Is.EqualTo("api-key-user"));
        }

        [Test]
        public async Task HandleChallengeAsync_Sets401StatusCode()
        {
            var handler = await CreateAndInitializeHandler();

            await handler.ChallengeAsync(new AuthenticationProperties());

            Assert.That(httpContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        [Test]
        public async Task HandleAuthenticateAsync_ValidKey_CallsValidateOnService()
        {
            httpContext.Request.Headers["X-Api-Key"] = "some-key";
            apiKeyServiceMock.Setup(x => x.ValidateApiKeyAsync("some-key"))
                .ReturnsAsync(true);

            var handler = await CreateAndInitializeHandler();
            await handler.AuthenticateAsync();

            apiKeyServiceMock.Verify(x => x.ValidateApiKeyAsync("some-key"), Times.Once);
        }

        private async Task<ApiKeyAuthenticationHandler> CreateAndInitializeHandler()
        {
            var handler = new ApiKeyAuthenticationHandler(
                optionsMock.Object,
                loggerFactoryMock.Object,
                UrlEncoder.Default,
                apiKeyServiceMock.Object);

            var scheme = new AuthenticationScheme(SchemeName, null, typeof(ApiKeyAuthenticationHandler));
            await handler.InitializeAsync(scheme, httpContext);

            return handler;
        }
    }
}
