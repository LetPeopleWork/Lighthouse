using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Implementation.Auth;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Auth
{
    [TestFixture]
    public class AuthModeResolverTest
    {
        private Mock<IOptions<AuthenticationConfiguration>> configMock;
        private Mock<IAuthConfigurationValidator> validatorMock;
        private Mock<ILicenseService> licenseServiceMock;
        private Mock<IPlatformService> platformServiceMock;
        private Mock<ILogger<AuthModeResolver>> loggerMock;

        [SetUp]
        public void SetUp()
        {
            configMock = new Mock<IOptions<AuthenticationConfiguration>>();
            validatorMock = new Mock<IAuthConfigurationValidator>();
            licenseServiceMock = new Mock<ILicenseService>();
            platformServiceMock = new Mock<IPlatformService>();
            loggerMock = new Mock<ILogger<AuthModeResolver>>();
        }

        [Test]
        public void Resolve_AuthDisabled_ReturnsDisabledMode()
        {
            SetupConfig(enabled: false);
            SetupValidation(isValid: true);

            var subject = CreateSubject();

            var result = subject.Resolve();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Mode, Is.EqualTo(AuthMode.Disabled));
                Assert.That(result.MisconfigurationMessage, Is.Null);
            }

        }

        [Test]
        public void Resolve_AuthEnabled_ValidConfig_PremiumValid_ReturnsEnabledMode()
        {
            SetupConfig(enabled: true);
            SetupValidation(isValid: true);
            licenseServiceMock.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            var subject = CreateSubject();

            var result = subject.Resolve();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Mode, Is.EqualTo(AuthMode.Enabled));
                Assert.That(result.MisconfigurationMessage, Is.Null);
            }

        }

        [Test]
        public void Resolve_AuthEnabled_ValidConfig_PremiumInvalid_ReturnsBlockedMode()
        {
            SetupConfig(enabled: true);
            SetupValidation(isValid: true);
            licenseServiceMock.Setup(l => l.CanUsePremiumFeatures()).Returns(false);

            var subject = CreateSubject();

            var result = subject.Resolve();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Mode, Is.EqualTo(AuthMode.Blocked));
                Assert.That(result.MisconfigurationMessage, Is.Null);
            }
        }

        [Test]
        public void Resolve_AuthEnabled_InvalidConfig_ReturnsMisconfiguredMode()
        {
            SetupConfig(enabled: true);
            SetupValidation(isValid: false, errorReason: "Authority is required");

            var subject = CreateSubject();

            var result = subject.Resolve();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Mode, Is.EqualTo(AuthMode.Misconfigured));
                Assert.That(result.MisconfigurationMessage, Is.EqualTo("Authority is required"));
            }
        }

        [Test]
        public void Resolve_Standalone_AuthEnabled_ReturnsDisabledMode()
        {
            SetupConfig(enabled: true);
            SetupValidation(isValid: true);
            platformServiceMock.Setup(p => p.IsStandalone).Returns(true);
            licenseServiceMock.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            var subject = CreateSubject();

            var result = subject.Resolve();

            Assert.That(result.Mode, Is.EqualTo(AuthMode.Disabled));
        }

        [Test]
        public void Resolve_Standalone_AuthDisabled_ReturnsDisabledMode()
        {
            SetupConfig(enabled: false);
            SetupValidation(isValid: true);
            platformServiceMock.Setup(p => p.IsStandalone).Returns(true);

            var subject = CreateSubject();

            var result = subject.Resolve();

            Assert.That(result.Mode, Is.EqualTo(AuthMode.Disabled));
        }

        [Test]
        public void Resolve_AuthEnabled_InvalidConfig_DoesNotCheckLicense()
        {
            SetupConfig(enabled: true);
            SetupValidation(isValid: false, errorReason: "ClientId is required");

            var subject = CreateSubject();

            subject.Resolve();

            licenseServiceMock.Verify(l => l.CanUsePremiumFeatures(), Times.Never);
        }

        [Test]
        public void Resolve_AuthDisabled_DoesNotCheckLicense()
        {
            SetupConfig(enabled: false);
            SetupValidation(isValid: true);

            var subject = CreateSubject();

            subject.Resolve();

            licenseServiceMock.Verify(l => l.CanUsePremiumFeatures(), Times.Never);
        }

        [Test]
        public void Resolve_AuthDisabled_DoesNotValidateConfig()
        {
            SetupConfig(enabled: false);

            var subject = CreateSubject();

            subject.Resolve();

            validatorMock.Verify(v => v.Validate(It.IsAny<AuthenticationConfiguration>()), Times.Never);
        }

        private AuthModeResolver CreateSubject()
        {
            return new AuthModeResolver(
                configMock.Object,
                validatorMock.Object,
                licenseServiceMock.Object,
                platformServiceMock.Object,
                loggerMock.Object);
        }

        private void SetupConfig(bool enabled)
        {
            var config = new AuthenticationConfiguration
            {
                Enabled = enabled,
                Authority = "https://idp.example.com",
                ClientId = "lighthouse-client",
                ClientSecret = "secret",
            };
            configMock.Setup(c => c.Value).Returns(config);
        }

        private void SetupValidation(bool isValid, string? errorReason = null)
        {
            var result = isValid
                ? AuthConfigurationValidationResult.Valid()
                : AuthConfigurationValidationResult.Invalid(errorReason!);

            validatorMock.Setup(v => v.Validate(It.IsAny<AuthenticationConfiguration>())).Returns(result);
        }
    }
}
