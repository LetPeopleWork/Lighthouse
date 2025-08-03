using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Licensing;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Licensing
{
    [TestFixture]
    public class LicenseServiceTest
    {
        private Mock<IRepository<LicenseInformation>> licenseRepoMock;

        [SetUp]
        public void SetUp()
        {
            licenseRepoMock = new Mock<IRepository<LicenseInformation>>();
        }

        [Test]
        public void ImportLicense_EmptyLicense_ReturnsNull()
        {
            var licenseService = CreateSubject();

            var result = licenseService.ImportLicense(string.Empty);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void ImportLicense_IsNotValidJson_ReturnsNull()
        {
            var licenseService = CreateSubject();
            var licenseContent = "This is not a valid JSON string";

            var result = licenseService.ImportLicense(licenseContent);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void ImportLicense_ValidLicense_ReturnsLicenseInfo()
        {
            var licenseService = CreateSubject();

            var licenseContent = File.ReadAllText("Services/Implementation/Licensing/valid_license.json");

            var result = licenseService.ImportLicense(licenseContent);

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void ImportLicense_InvalidLicense_ReturnsNull()
        {
            var licenseService = CreateSubject();
            var licenseContent = File.ReadAllText("Services/Implementation/Licensing/invalid_license.json");

            var result = licenseService.ImportLicense(licenseContent);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void ImportLicense_ValidLicense_StoresInDatabase()
        {
            var licenseService = CreateSubject();
            var licenseContent = File.ReadAllText("Services/Implementation/Licensing/valid_license.json");

            licenseService.ImportLicense(licenseContent);

            licenseRepoMock.Verify(repo => repo.Add(It.IsAny<LicenseInformation>()), Times.Once);
            licenseRepoMock.Verify(repo => repo.Save(), Times.Once);
        }

        [Test]
        public void GetLicenseData_NoLicense_ReturnsNullAndFalse()
        {
            var licenseService = CreateSubject();
            licenseRepoMock.Setup(repo => repo.GetAll()).Returns(new List<LicenseInformation>());

            var (licenseInfo, isValid) = licenseService.GetLicenseData();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(licenseInfo, Is.Null);
                Assert.That(isValid, Is.False);
            }
        }

        [Test]
        public void GetLicenseData_ValidLicense_ReturnsLicenseInformationAndTrue()
        {
            var licenseService = CreateSubject();
            var licenseContent = File.ReadAllText("Services/Implementation/Licensing/valid_license.json");

            var licenseInfo = licenseService.ImportLicense(licenseContent) ?? throw new ArgumentNullException("LicenseInfo cannot be null");
            licenseRepoMock.Setup(repo => repo.GetAll()).Returns(new List<LicenseInformation> { licenseInfo });

            var (result, isValid) = licenseService.GetLicenseData();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(isValid, Is.True);
            }
        }

        [Test]
        public void GetLicenseData_InvalidLicense_ReturnsLicenseInformationAndFalse()
        {
            var licenseService = CreateSubject();

            licenseRepoMock.Setup(repo => repo.GetAll()).Returns(new List<LicenseInformation> {
                new LicenseInformation
                {
                    Name = "Invalid License",
                    Organization = "Test Org",
                    Email = "invalid@mail.com",
                    ExpiryDate = DateTime.UtcNow.AddDays(10),
                    Signature = "invalid_signature"
                }
            });

            var (result, isValid) = licenseService.GetLicenseData();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(isValid, Is.False);
            }
        }

        [Test]
        public void CanUsePremiumFeature_InvalidLicense_ReturnsFalse()
        {
            var licenseService = CreateSubject();

            var licenseContent = File.ReadAllText("Services/Implementation/Licensing/invalid_license.json");
            licenseService.ImportLicense(licenseContent);

            var canUsePremiumFeatures = licenseService.CanUsePremiumFeatures();
            Assert.That(canUsePremiumFeatures, Is.False);
        }

        [Test]
        public void CanUsePremiumFeature_ValidLicense_Expired_ReturnsFalse()
        {
            var licenseService = CreateSubject();

            var licenseContent = File.ReadAllText("Services/Implementation/Licensing/valid_license.json");
            licenseService.ImportLicense(licenseContent);

            var canUsePremiumFeatures = licenseService.CanUsePremiumFeatures();
            Assert.That(canUsePremiumFeatures, Is.False);
        }

        [Test]
        public void CanUsePremiumFeature_ValidLicense_NotExpired_ReturnsTrue()
        {
            var licenseInfo = new LicenseInformation
            {
                Name = "Benjamin",
                Organization = "Let People Work",
                Email = "benjamin@letpeople.work",
                ExpiryDate = DateTime.UtcNow.AddYears(1),
            };

            var licenseVerifierMock = new Mock<ILicenseVerifier>();
            licenseVerifierMock
                .Setup(verifier => verifier.VerifyLicense(licenseInfo))
                .Returns(true);

            licenseRepoMock.Setup(repo => repo.GetAll()).Returns(new List<LicenseInformation> { licenseInfo });

            var licenseService = CreateSubject(licenseVerifierMock.Object);

            var canUsePremiumFeatures = licenseService.CanUsePremiumFeatures();

            Assert.That(canUsePremiumFeatures, Is.True);
        }

        [Test]
        public void CanUsePremiumFeature_ValidLicense_ExpiresToday_ReturnsTrue()
        {
            var licenseInfo = new LicenseInformation
            {
                Name = "Benjamin",
                Organization = "Let People Work",
                Email = "benjamin@letpeople.work",
                ExpiryDate = DateTime.Today,
            };

            var licenseVerifierMock = new Mock<ILicenseVerifier>();
            licenseVerifierMock
                .Setup(verifier => verifier.VerifyLicense(licenseInfo))
                .Returns(true);

            licenseRepoMock.Setup(repo => repo.GetAll()).Returns(new List<LicenseInformation> { licenseInfo });

            var licenseService = CreateSubject(licenseVerifierMock.Object);

            var canUsePremiumFeatures = licenseService.CanUsePremiumFeatures();

            Assert.That(canUsePremiumFeatures, Is.True);
        }

        private LicenseService CreateSubject(ILicenseVerifier? licenseVerifierOverride = null)
        {
            var licenseVerifier = licenseVerifierOverride ?? new LicenseVerifier();

            return new LicenseService(Mock.Of<ILogger<LicenseService>>(), licenseRepoMock.Object, licenseVerifier);
        }
    }
}
