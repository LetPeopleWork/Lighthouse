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
    public class CliTokenAuthenticationHandlerTest
    {
        private Mock<ICliAuthSessionService> cliAuthSessionServiceMock;

        private Mock<IOptionsMonitor<AuthenticationSchemeOptions>> optionsMock;

        private Mock<ILoggerFactory> loggerFactoryMock;
        
        private DefaultHttpContext httpContext;

        private const string SchemeName = "CliToken";

        [SetUp]
        public void Setup()
        {
            cliAuthSessionServiceMock = new Mock<ICliAuthSessionService>();
            optionsMock = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
            loggerFactoryMock = new Mock<ILoggerFactory>();

            optionsMock.Setup(x => x.Get(It.IsAny<string>()))
                .Returns(new AuthenticationSchemeOptions());

            loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>()))
                .Returns(Mock.Of<ILogger>());

            httpContext = new DefaultHttpContext();
        }

        [Test]
        public async Task HandleAuthenticateAsync_NoAuthorizationHeader_ReturnsNoResult()
        {
            var handler = await CreateAndInitializeHandler();

            var result = await handler.AuthenticateAsync();

            Assert.That(result.None, Is.True);
        }

        [Test]
        public async Task HandleAuthenticateAsync_NonBearerAuthorizationHeader_ReturnsNoResult()
        {
            httpContext.Request.Headers.Authorization = "Basic dXNlcjpwYXNz";
            var handler = await CreateAndInitializeHandler();

            var result = await handler.AuthenticateAsync();

            Assert.That(result.None, Is.True);
        }

        [Test]
        public async Task HandleAuthenticateAsync_BearerPrefixOnly_ReturnsFail()
        {
            httpContext.Request.Headers.Authorization = "Bearer ";
            var handler = await CreateAndInitializeHandler();

            var result = await handler.AuthenticateAsync();

            Assert.That(result.Failure, Is.Not.Null);
        }

        [Test]
        public async Task HandleAuthenticateAsync_InvalidToken_ReturnsFail()
        {
            httpContext.Request.Headers.Authorization = "Bearer invalid-token";
            cliAuthSessionServiceMock
                .Setup(x => x.ValidateToken("invalid-token", out It.Ref<string?>.IsAny))
                .Returns(false);

            var handler = await CreateAndInitializeHandler();

            var result = await handler.AuthenticateAsync();

            Assert.That(result.Failure, Is.Not.Null);
        }

        [Test]
        public async Task HandleAuthenticateAsync_ValidToken_ReturnsSuccess()
        {
            var token = "valid-bearer-token";
            var userName = "testuser";
            httpContext.Request.Headers.Authorization = $"Bearer {token}";

            SetupValidToken(token, userName);

            var handler = await CreateAndInitializeHandler();

            var result = await handler.AuthenticateAsync();

            Assert.That(result.Succeeded, Is.True);
        }

        [Test]
        public async Task HandleAuthenticateAsync_ValidToken_PrincipalHasNameClaim()
        {
            var token = "valid-bearer-token";
            var userName = "testuser";
            httpContext.Request.Headers.Authorization = $"Bearer {token}";

            SetupValidToken(token, userName);

            var handler = await CreateAndInitializeHandler();

            var result = await handler.AuthenticateAsync();

            Assert.That(result.Principal?.Identity?.Name, Is.EqualTo(userName));
        }

        [Test]
        public async Task HandleAuthenticateAsync_ValidToken_PrincipalHasCliTokenAuthMethodClaim()
        {
            var token = "valid-bearer-token";
            httpContext.Request.Headers.Authorization = $"Bearer {token}";

            SetupValidToken(token, "testuser");

            var handler = await CreateAndInitializeHandler();

            var result = await handler.AuthenticateAsync();

            var authMethodClaim = result.Principal?.FindFirst("auth_method")?.Value;
            Assert.That(authMethodClaim, Is.EqualTo("cli-token"));
        }

        [Test]
        public async Task HandleAuthenticateAsync_ValidToken_NullUserName_FallsBackToCliUser()
        {
            var token = "valid-bearer-token";
            httpContext.Request.Headers.Authorization = $"Bearer {token}";

            string? nullUser = null;
            cliAuthSessionServiceMock
                .Setup(x => x.ValidateToken(token, out nullUser))
                .Returns(true);

            var handler = await CreateAndInitializeHandler();

            var result = await handler.AuthenticateAsync();

            Assert.That(result.Principal?.Identity?.Name, Is.EqualTo("cli-user"));
        }

        [Test]
        public async Task HandleChallengeAsync_Sets401StatusCode()
        {
            var handler = await CreateAndInitializeHandler();

            await handler.ChallengeAsync(new AuthenticationProperties());

            Assert.That(httpContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        [Test]
        public async Task HandleAuthenticateAsync_ValidToken_CallsValidateTokenOnService()
        {
            var token = "some-token";
            httpContext.Request.Headers.Authorization = $"Bearer {token}";
            SetupValidToken(token, "user");

            var handler = await CreateAndInitializeHandler();
            await handler.AuthenticateAsync();

            cliAuthSessionServiceMock.Verify(
                x => x.ValidateToken(token, out It.Ref<string?>.IsAny),
                Times.Once);
        }

        private void SetupValidToken(string token, string userName)
        {
            cliAuthSessionServiceMock
                .Setup(x => x.ValidateToken(token, out userName))
                .Returns(true);
        }

        private async Task<CliTokenAuthenticationHandler> CreateAndInitializeHandler()
        {
            var handler = new CliTokenAuthenticationHandler(
                optionsMock.Object,
                loggerFactoryMock.Object,
                UrlEncoder.Default,
                cliAuthSessionServiceMock.Object);

            var scheme = new AuthenticationScheme(SchemeName, null, typeof(CliTokenAuthenticationHandler));
            await handler.InitializeAsync(scheme, httpContext);

            return handler;
        }
    }
}