using Lighthouse.Backend.API;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace Lighthouse.Backend.Tests.API
{
    [TestFixture]
    public class AuthControllerTest
    {
        private Mock<IAuthModeResolver> authModeResolverMock;

        [SetUp]
        public void SetUp()
        {
            authModeResolverMock = new Mock<IAuthModeResolver>();
        }

        [Test]
        public void GetRuntimeAuthStatus_AuthDisabled_ReturnsDisabledStatus()
        {
            var expectedStatus = new RuntimeAuthStatus { Mode = AuthMode.Disabled };
            authModeResolverMock.Setup(r => r.Resolve()).Returns(expectedStatus);
            var subject = CreateSubject();

            var actual = (ObjectResult)subject.GetRuntimeAuthStatus();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(actual.StatusCode, Is.EqualTo(200));
                var status = actual.Value as RuntimeAuthStatus;
                Assert.That(status, Is.Not.Null);
                Assert.That(status!.Mode, Is.EqualTo(AuthMode.Disabled));
                Assert.That(status.MisconfigurationMessage, Is.Null);
            }

        }

        [Test]
        public void GetRuntimeAuthStatus_AuthEnabled_ReturnsEnabledStatus()
        {
            var expectedStatus = new RuntimeAuthStatus { Mode = AuthMode.Enabled };
            authModeResolverMock.Setup(r => r.Resolve()).Returns(expectedStatus);
            var subject = CreateSubject();

            var actual = (ObjectResult)subject.GetRuntimeAuthStatus();

            Assert.That(actual.StatusCode, Is.EqualTo(200));
            var status = actual.Value as RuntimeAuthStatus;
            Assert.That(status!.Mode, Is.EqualTo(AuthMode.Enabled));
        }

        [Test]
        public void GetRuntimeAuthStatus_AuthMisconfigured_ReturnsMisconfiguredWithMessage()
        {
            var expectedStatus = new RuntimeAuthStatus
            {
                Mode = AuthMode.Misconfigured,
                MisconfigurationMessage = "Authority is required",
            };
            authModeResolverMock.Setup(r => r.Resolve()).Returns(expectedStatus);
            var subject = CreateSubject();

            var actual = (ObjectResult)subject.GetRuntimeAuthStatus();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(actual.StatusCode, Is.EqualTo(200));
                var status = actual.Value as RuntimeAuthStatus;
                Assert.That(status!.Mode, Is.EqualTo(AuthMode.Misconfigured));
                Assert.That(status.MisconfigurationMessage, Is.EqualTo("Authority is required"));
            }
        }

        [Test]
        public void GetRuntimeAuthStatus_AuthBlocked_ReturnsBlockedStatus()
        {
            var expectedStatus = new RuntimeAuthStatus { Mode = AuthMode.Blocked };
            authModeResolverMock.Setup(r => r.Resolve()).Returns(expectedStatus);
            var subject = CreateSubject();

            var actual = (ObjectResult)subject.GetRuntimeAuthStatus();

            Assert.That(actual.StatusCode, Is.EqualTo(200));
            var status = actual.Value as RuntimeAuthStatus;
            Assert.That(status!.Mode, Is.EqualTo(AuthMode.Blocked));
        }

        [Test]
        public void GetRuntimeAuthStatus_DoesNotLeakConfigDetails()
        {
            var expectedStatus = new RuntimeAuthStatus { Mode = AuthMode.Misconfigured, MisconfigurationMessage = "Authority is required" };
            authModeResolverMock.Setup(r => r.Resolve()).Returns(expectedStatus);
            var subject = CreateSubject();

            var actual = (ObjectResult)subject.GetRuntimeAuthStatus();

            var status = actual.Value as RuntimeAuthStatus;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(status!.MisconfigurationMessage, Does.Not.Contain("https://"));
                Assert.That(status.MisconfigurationMessage, Does.Not.Contain("client"));
            }
        }

        private AuthController CreateSubject()
        {
            return new AuthController(authModeResolverMock.Object);
        }

        private AuthController CreateSubjectWithUser(ClaimsPrincipal user)
        {
            var controller = new AuthController(authModeResolverMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user },
                }
            };
            return controller;
        }

        [Test]
        public void Login_AuthEnabled_ReturnsChallengeResult()
        {
            authModeResolverMock.Setup(r => r.Resolve()).Returns(new RuntimeAuthStatus { Mode = AuthMode.Enabled });
            var subject = CreateSubject();

            var result = subject.Login();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<ChallengeResult>());
                var challenge = (ChallengeResult)result;
                Assert.That(challenge.AuthenticationSchemes, Does.Contain("OpenIdConnect"));
                Assert.That(challenge.Properties!.RedirectUri, Is.EqualTo("/"));
            }
        }

        [Test]
        public void Login_AuthBlocked_ReturnsChallengeResult()
        {
            authModeResolverMock.Setup(r => r.Resolve()).Returns(new RuntimeAuthStatus { Mode = AuthMode.Blocked });
            var subject = CreateSubject();

            var result = subject.Login();

            Assert.That(result, Is.InstanceOf<ChallengeResult>());
        }

        [Test]
        public void Login_AuthDisabled_ReturnsNotFound()
        {
            authModeResolverMock.Setup(r => r.Resolve()).Returns(new RuntimeAuthStatus { Mode = AuthMode.Disabled });
            var subject = CreateSubject();

            var result = subject.Login();

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public void Login_AuthMisconfigured_ReturnsNotFound()
        {
            authModeResolverMock.Setup(r => r.Resolve()).Returns(new RuntimeAuthStatus { Mode = AuthMode.Misconfigured, MisconfigurationMessage = "Authority is required" });
            var subject = CreateSubject();

            var result = subject.Login();

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public void Logout_AuthEnabled_ReturnsSignOutResult()
        {
            authModeResolverMock.Setup(r => r.Resolve()).Returns(new RuntimeAuthStatus { Mode = AuthMode.Enabled });
            var subject = CreateSubject();

            var result = subject.Logout();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<SignOutResult>());
                var signOut = (SignOutResult)result;
                Assert.That(signOut.AuthenticationSchemes, Does.Contain("Cookies"));
                Assert.That(signOut.AuthenticationSchemes, Does.Contain("OpenIdConnect"));
                Assert.That(signOut.Properties!.RedirectUri, Is.EqualTo("/"));
            }
        }

        [Test]
        public void Logout_AuthDisabled_ReturnsNotFound()
        {
            authModeResolverMock.Setup(r => r.Resolve()).Returns(new RuntimeAuthStatus { Mode = AuthMode.Disabled });
            var subject = CreateSubject();

            var result = subject.Logout();

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public void GetSession_Authenticated_ReturnsAuthenticatedStatus()
        {
            var claims = new[]
            {
                new Claim("name", "Test User"),
                new Claim(ClaimTypes.Email, "test@example.com"),
            };
            var identity = new ClaimsIdentity(claims, "TestAuthentication");
            var user = new ClaimsPrincipal(identity);
            var subject = CreateSubjectWithUser(user);

            var result = (ObjectResult)subject.GetSession();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.StatusCode, Is.EqualTo(200));
                var session = result.Value as AuthSessionStatus;
                Assert.That(session, Is.Not.Null);
                Assert.That(session!.IsAuthenticated, Is.True);
                Assert.That(session.DisplayName, Is.EqualTo("Test User"));
                Assert.That(session.Email, Is.EqualTo("test@example.com"));
            }
        }

        [Test]
        public void GetSession_NotAuthenticated_ReturnsUnauthenticatedStatus()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity());
            var subject = CreateSubjectWithUser(user);

            var result = (ObjectResult)subject.GetSession();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.StatusCode, Is.EqualTo(200));
                var session = result.Value as AuthSessionStatus;
                Assert.That(session, Is.Not.Null);
                Assert.That(session!.IsAuthenticated, Is.False);
                Assert.That(session.DisplayName, Is.Null);
                Assert.That(session.Email, Is.Null);
            }
        }

        [Test]
        public void GetSession_AuthenticatedWithEmailClaim_ReturnsEmail()
        {
            var claims = new[]
            {
                new Claim("name", "Another User"),
                new Claim("email", "user@example.com"),
            };
            var identity = new ClaimsIdentity(claims, "TestAuthentication");
            var user = new ClaimsPrincipal(identity);
            var subject = CreateSubjectWithUser(user);

            var result = (ObjectResult)subject.GetSession();

            var session = (result.Value as AuthSessionStatus)!;
            Assert.That(session.Email, Is.EqualTo("user@example.com"));
        }
    }
}
