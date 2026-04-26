using Lighthouse.Backend.API;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace Lighthouse.Backend.Tests.API
{
    public class CliAuthControllerTest
    {
        private Mock<ICliAuthSessionService> cliAuthSessionServiceMock;
        private Mock<IAuthModeResolver> authModeResolverMock;
        private Mock<IServiceProvider> serviceProviderMock;
        private Mock<IAuthenticationService> authenticationServiceMock;

        [SetUp]
        public void Setup()
        {
            cliAuthSessionServiceMock = new Mock<ICliAuthSessionService>();
            authModeResolverMock = new Mock<IAuthModeResolver>();
            serviceProviderMock = new Mock<IServiceProvider>();
            authenticationServiceMock = new Mock<IAuthenticationService>();

            serviceProviderMock
                .Setup(x => x.GetService(typeof(IAuthenticationService)))
                .Returns(authenticationServiceMock.Object);
        }

        // --- StartSession ---

        [Test]
        public void StartSession_AuthNotEnabled_ReturnsNotFound()
        {
            SetupAuthMode(AuthMode.Disabled);
            var subject = CreateSubject();

            var result = subject.StartSession();

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public void StartSession_AuthEnabled_ReturnsOkWithSessionInfo()
        {
            SetupAuthMode(AuthMode.Enabled);
            var expiresAt = DateTime.UtcNow.AddMinutes(10);
            cliAuthSessionServiceMock.Setup(x => x.StartSession())
                .Returns(("session-abc", expiresAt));

            var subject = CreateSubjectWithHttpContext("https", "localhost");

            var result = subject.StartSession() as OkObjectResult;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result!.StatusCode, Is.EqualTo(200));

                var info = result.Value as CliAuthSessionInfo;
                Assert.That(info, Is.Not.Null);
                Assert.That(info!.SessionId, Is.EqualTo("session-abc"));
                Assert.That(info.ExpiresAt, Is.EqualTo(expiresAt));
                Assert.That(info.VerificationUrl, Does.Contain("session-abc"));
            }
        }

        [Test]
        public void StartSession_AuthEnabled_VerificationUrlContainsSchemeAndHost()
        {
            SetupAuthMode(AuthMode.Enabled);
            cliAuthSessionServiceMock.Setup(x => x.StartSession())
                .Returns(("my-session", DateTime.UtcNow.AddMinutes(10)));

            var subject = CreateSubjectWithHttpContext("https", "lighthouse.example.com");

            var result = subject.StartSession() as OkObjectResult;
            var info = result?.Value as CliAuthSessionInfo;

            Assert.That(info?.VerificationUrl, Does.StartWith("https://lighthouse.example.com"));
        }

        // --- PollSession ---

        [Test]
        public void PollSession_DelegatesToService_ReturnsPollResponse()
        {
            var expected = new CliAuthSessionPollResponse { Status = "pending" };
            cliAuthSessionServiceMock.Setup(x => x.PollSession("sess-1"))
                .Returns(expected);

            var subject = CreateSubject();
            var result = subject.PollSession("sess-1") as OkObjectResult;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result!.Value, Is.SameAs(expected));
            }
        }

        [Test]
        public void PollSession_ApprovedSession_ReturnsApprovedStatus()
        {
            var response = new CliAuthSessionPollResponse
            {
                Status = "approved",
                Token = "my-token",
                UserName = "alice",
            };
            cliAuthSessionServiceMock.Setup(x => x.PollSession("sess-approved"))
                .Returns(response);

            var subject = CreateSubject();
            var result = (subject.PollSession("sess-approved") as OkObjectResult)?.Value
                as CliAuthSessionPollResponse;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result!.Status, Is.EqualTo("approved"));
                Assert.That(result.Token, Is.EqualTo("my-token"));
                Assert.That(result.UserName, Is.EqualTo("alice"));
            }
        }

        // --- RevokeToken ---

        [Test]
        public void RevokeToken_EmptyToken_ReturnsBadRequest()
        {
            var subject = CreateSubject();

            var result = subject.RevokeToken(new CliRevokeRequest { Token = "" });

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public void RevokeToken_WhitespaceToken_ReturnsBadRequest()
        {
            var subject = CreateSubject();

            var result = subject.RevokeToken(new CliRevokeRequest { Token = "   " });

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public void RevokeToken_ValidToken_CallsServiceAndReturnsOk()
        {
            var subject = CreateSubject();

            var result = subject.RevokeToken(new CliRevokeRequest { Token = "some-token" });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<OkResult>());
                cliAuthSessionServiceMock.Verify(x => x.RevokeToken("some-token"), Times.Once);
            }
        }

        // --- VerifyCliSession ---

        [Test]
        public async Task VerifyCliSession_AuthNotEnabled_ReturnsNotFound()
        {
            SetupAuthMode(AuthMode.Disabled);
            var subject = CreateSubject();

            var result = await subject.VerifyCliSession("sess-1");

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task VerifyCliSession_ExpiredSession_ReturnsExpiredHtml()
        {
            SetupAuthMode(AuthMode.Enabled);
            cliAuthSessionServiceMock.Setup(x => x.PollSession("expired-sess"))
                .Returns(new CliAuthSessionPollResponse { Status = "expired" });

            var subject = CreateSubjectWithHttpContext("https", "localhost");

            var result = await subject.VerifyCliSession("expired-sess") as ContentResult;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result!.Content, Does.Contain("expired"));
            }
        }

        [Test]
        public async Task VerifyCliSession_UserNotAuthenticated_ReturnsChallengeResult()
        {
            SetupAuthMode(AuthMode.Enabled);
            cliAuthSessionServiceMock.Setup(x => x.PollSession("valid-sess"))
                .Returns(new CliAuthSessionPollResponse { Status = "pending" });

            authenticationServiceMock
                .Setup(x => x.AuthenticateAsync(It.IsAny<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme))
                .ReturnsAsync(AuthenticateResult.Fail("not logged in"));

            var subject = CreateSubjectWithHttpContext("https", "localhost");

            var result = await subject.VerifyCliSession("valid-sess");

            Assert.That(result, Is.InstanceOf<ChallengeResult>());
        }

        [Test]
        public async Task VerifyCliSession_UserAuthenticated_ReturnsVerifyPageHtml()
        {
            SetupAuthMode(AuthMode.Enabled);
            cliAuthSessionServiceMock.Setup(x => x.PollSession("valid-sess"))
                .Returns(new CliAuthSessionPollResponse { Status = "pending" });

            var principal = BuildPrincipal("alice");
            authenticationServiceMock
                .Setup(x => x.AuthenticateAsync(It.IsAny<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme))
                .ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(principal, CookieAuthenticationDefaults.AuthenticationScheme)));

            var subject = CreateSubjectWithHttpContext("https", "localhost");

            var result = await subject.VerifyCliSession("valid-sess") as ContentResult;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result!.Content, Does.Contain("alice"));
                Assert.That(result.Content, Does.Contain("valid-sess"));
            }
        }

        // --- ApproveCliSession ---

        [Test]
        public async Task ApproveCliSession_AuthNotEnabled_ReturnsNotFound()
        {
            SetupAuthMode(AuthMode.Disabled);
            var subject = CreateSubject();

            var result = await subject.ApproveCliSession("sess-1");

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task ApproveCliSession_UserNotAuthenticated_ReturnsUnauthorized()
        {
            SetupAuthMode(AuthMode.Enabled);
            authenticationServiceMock
                .Setup(x => x.AuthenticateAsync(It.IsAny<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme))
                .ReturnsAsync(AuthenticateResult.Fail("not logged in"));

            var subject = CreateSubjectWithHttpContext("https", "localhost");

            var result = await subject.ApproveCliSession("sess-1");

            Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
        }

        [Test]
        public async Task ApproveCliSession_SessionAlreadyExpired_ReturnsExpiredHtml()
        {
            SetupAuthMode(AuthMode.Enabled);
            cliAuthSessionServiceMock.Setup(x => x.TryApproveSession("expired-sess", It.IsAny<string>()))
                .Returns(false);

            var principal = BuildPrincipal("alice");
            authenticationServiceMock
                .Setup(x => x.AuthenticateAsync(It.IsAny<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme))
                .ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(principal, CookieAuthenticationDefaults.AuthenticationScheme)));

            var subject = CreateSubjectWithHttpContext("https", "localhost");

            var result = await subject.ApproveCliSession("expired-sess") as ContentResult;

            Assert.That(result!.Content, Does.Contain("expired"));
        }

        [Test]
        public async Task ApproveCliSession_ValidSessionAndUser_ReturnsApprovedHtml()
        {
            SetupAuthMode(AuthMode.Enabled);
            cliAuthSessionServiceMock.Setup(x => x.TryApproveSession("valid-sess", "alice"))
                .Returns(true);

            var principal = BuildPrincipal("alice");
            authenticationServiceMock
                .Setup(x => x.AuthenticateAsync(It.IsAny<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme))
                .ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(principal, CookieAuthenticationDefaults.AuthenticationScheme)));

            var subject = CreateSubjectWithHttpContext("https", "localhost");

            var result = await subject.ApproveCliSession("valid-sess") as ContentResult;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result!.Content, Does.Contain("alice"));
                Assert.That(result.Content, Does.Contain("Authorization successful"));
            }
        }

        [Test]
        public async Task ApproveCliSession_ValidSession_CallsApproveOnService()
        {
            SetupAuthMode(AuthMode.Enabled);
            cliAuthSessionServiceMock.Setup(x => x.TryApproveSession("valid-sess", "alice"))
                .Returns(true);

            var principal = BuildPrincipal("alice");
            authenticationServiceMock
                .Setup(x => x.AuthenticateAsync(It.IsAny<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme))
                .ReturnsAsync(AuthenticateResult.Success(new AuthenticationTicket(principal, CookieAuthenticationDefaults.AuthenticationScheme)));

            var subject = CreateSubjectWithHttpContext("https", "localhost");

            await subject.ApproveCliSession("valid-sess");

            cliAuthSessionServiceMock.Verify(x => x.TryApproveSession("valid-sess", "alice"), Times.Once);
        }

        // --- Helpers ---

        private void SetupAuthMode(AuthMode mode)
        {
            authModeResolverMock.Setup(x => x.Resolve())
                .Returns(new RuntimeAuthStatus { Mode = mode });
        }

        private static ClaimsPrincipal BuildPrincipal(string name)
        {
            var claims = new[]
            {
                new Claim("name", name),
                new Claim(ClaimTypes.Name, name),
            };
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "cookie"));
        }

        private CliAuthController CreateSubject()
        {
            var controller = new CliAuthController(
                cliAuthSessionServiceMock.Object,
                authModeResolverMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { RequestServices = serviceProviderMock.Object }
                }
            };

            return controller;
        }

        private CliAuthController CreateSubjectWithHttpContext(string scheme, string host)
        {
            var controller = new CliAuthController(
                cliAuthSessionServiceMock.Object,
                authModeResolverMock.Object);

            var httpContext = new DefaultHttpContext { RequestServices = serviceProviderMock.Object };
            httpContext.Request.Scheme = scheme;
            httpContext.Request.Host = new HostString(host);

            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            return controller;
        }
    }
}