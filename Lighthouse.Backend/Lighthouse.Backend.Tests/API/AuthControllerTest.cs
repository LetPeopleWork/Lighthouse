using Lighthouse.Backend.API;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Microsoft.AspNetCore.Mvc;
using Moq;

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

            Assert.That(actual.StatusCode, Is.EqualTo(200));
            var status = actual.Value as RuntimeAuthStatus;
            Assert.That(status, Is.Not.Null);
            Assert.That(status!.Mode, Is.EqualTo(AuthMode.Disabled));
            Assert.That(status.MisconfigurationMessage, Is.Null);
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

            Assert.That(actual.StatusCode, Is.EqualTo(200));
            var status = actual.Value as RuntimeAuthStatus;
            Assert.That(status!.Mode, Is.EqualTo(AuthMode.Misconfigured));
            Assert.That(status.MisconfigurationMessage, Is.EqualTo("Authority is required"));
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
            Assert.That(status!.MisconfigurationMessage, Does.Not.Contain("https://"));
            Assert.That(status.MisconfigurationMessage, Does.Not.Contain("client"));
        }

        private AuthController CreateSubject()
        {
            return new AuthController(authModeResolverMock.Object);
        }
    }
}
