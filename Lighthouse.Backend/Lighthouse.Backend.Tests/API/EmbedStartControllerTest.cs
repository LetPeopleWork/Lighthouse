using Lighthouse.Backend.API;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Implementation.Auth;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;

namespace Lighthouse.Backend.Tests.API
{
    /// <summary>
    /// Epic 5146 slice 01 (#5692) — ADR-137 hop 1, at the cheapest level that still exercises the
    /// real controller. The journey tests cover the wiring; these cover the edges of the nonce
    /// contract, the challenge's return address and the shape of the terminal page.
    /// </summary>
    [TestFixture]
    public class EmbedStartControllerTest
    {
        private const string ViewerSubject = "viewer-embed-start-unit";
        private const string ValidNonce = "AAAAAAAAAAAAAAAAAAAAAA";

        private static CancellationToken TestToken => TestContext.CurrentContext.CancellationToken;

        private Mock<IAuthModeResolver> authModeResolver = null!;
        private Mock<ICurrentUserProfileService> currentUserProfileService = null!;
        private Mock<IEmbedSessionTokenService> embedSessionTokenService = null!;
        private Mock<IAuthenticationService> authenticationService = null!;
        private ServiceProvider requestServices = null!;
        private DefaultHttpContext httpContext = null!;

        [SetUp]
        public void SetUp()
        {
            authModeResolver = new Mock<IAuthModeResolver>();
            authModeResolver.Setup(resolver => resolver.Resolve())
                .Returns(new RuntimeAuthStatus { Mode = AuthMode.Enabled });

            currentUserProfileService = new Mock<ICurrentUserProfileService>();
            embedSessionTokenService = new Mock<IEmbedSessionTokenService>();

            authenticationService = new Mock<IAuthenticationService>();
            authenticationService
                .Setup(service => service.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
                .ReturnsAsync(AuthenticateResult.NoResult());

            var services = new ServiceCollection();
            services.AddSingleton(authenticationService.Object);
            requestServices = services.BuildServiceProvider();

            httpContext = new DefaultHttpContext { RequestServices = requestServices };
        }

        [TearDown]
        public void TearDown()
        {
            requestServices.Dispose();
        }

        [Test]
        public async Task Start_SuppressesTheReferer()
        {
            await CreateSubject().Start(ValidNonce, TestToken);

            Assert.That(httpContext.Response.Headers["Referrer-Policy"].ToString(), Is.EqualTo("no-referrer"),
                "the nonce rides in the query string, so a Referer would carry a live handshake to whatever "
                + "the terminal page links to next");
        }

        [Test]
        [TestCase(22)]
        [TestCase(128)]
        public async Task Start_WithANonceAtTheAllowedLengthBoundary_ProceedsToTheIdentityProvider(int nonceLength)
        {
            var result = await CreateSubject().Start(new string('a', nonceLength), TestToken);

            Assert.That(result, Is.InstanceOf<ChallengeResult>(),
                "the bounds are inclusive; refusing a nonce of exactly the allowed length strands every "
                + "caller that generates one at the boundary");
        }

        [Test]
        [TestCase(21)]
        [TestCase(129)]
        public async Task Start_WithANonceOutsideTheAllowedLength_IsRefused(int nonceLength)
        {
            var result = await CreateSubject().Start(new string('a', nonceLength), TestToken);

            Assert.That(result, Is.InstanceOf<BadRequestResult>());
        }

        [Test]
        public async Task Start_WithANonceMixingLegalAndIllegalCharacters_IsRefused()
        {
            var result = await CreateSubject().Start("a" + new string('!', 30), TestToken);

            Assert.That(result, Is.InstanceOf<BadRequestResult>(),
                "every character has to be legal; one legal character in an otherwise arbitrary string "
                + "would let a caller smuggle whatever it likes into the redirect");
        }

        [Test]
        public async Task Start_ChallengingTheIdentityProvider_CarriesTheNonceBackToItself()
        {
            var result = await CreateSubject().Start(ValidNonce, TestToken);

            var challenge = result as ChallengeResult;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(challenge, Is.Not.Null);
                Assert.That(challenge!.Properties?.RedirectUri,
                    Is.EqualTo($"{EmbedStartController.StartPath}?nonce={Uri.EscapeDataString(ValidNonce)}"),
                    "without the nonce on the return address the viewer completes a login that resolves nothing");
                Assert.That(challenge.AuthenticationSchemes,
                    Does.Contain(OpenIdConnectDefaults.AuthenticationScheme));
            }
        }

        [Test]
        public async Task Start_ForAViewerWhoCanSeeSomething_EndsOnAReadablePage()
        {
            GivenAnAuthenticatedViewerWithReadableScope();

            var result = await CreateSubject().Start(ValidNonce, TestToken);

            var page = result as ContentResult;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(page, Is.Not.Null);
                Assert.That(page!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
                Assert.That(page.ContentType, Is.EqualTo("text/html; charset=utf-8"),
                    "the orphaned tab renders this; served as anything else it is markup a person reads verbatim");
                Assert.That(page.Content, Does.Contain("You are signed in to Lighthouse"),
                    "D61: the tab ends on a page a person can read, not on a blank 200");
            }
        }

        private void GivenAnAuthenticatedViewerWithReadableScope()
        {
            var identity = new ClaimsIdentity(
                [new Claim(ApiKeyPrincipalFactory.SubjectClaimType, ViewerSubject)],
                OpenIdConnectDefaults.AuthenticationScheme);

            var ticket = new AuthenticationTicket(
                new ClaimsPrincipal(identity),
                CookieAuthenticationDefaults.AuthenticationScheme);

            authenticationService
                .Setup(service => service.AuthenticateAsync(It.IsAny<HttpContext>(), SmartAuthSchemeSelector.CookieScheme))
                .ReturnsAsync(AuthenticateResult.Success(ticket));

            currentUserProfileService
                .Setup(service => service.GetOrCreateFromPrincipalAsync(
                    It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserProfile { Subject = ViewerSubject });
        }

        private EmbedStartController CreateSubject()
        {
            return new EmbedStartController(
                authModeResolver.Object,
                currentUserProfileService.Object,
                embedSessionTokenService.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
            };
        }
    }
}
