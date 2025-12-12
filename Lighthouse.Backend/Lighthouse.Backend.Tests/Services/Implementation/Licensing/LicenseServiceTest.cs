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
        public async Task ImportLicense_EmptyLicense_ReturnsNull()
        {
            var licenseService = CreateSubject();

            var result = await licenseService.ImportLicense(string.Empty);

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task ImportLicense_IsNotValidJson_ReturnsNull()
        {
            var licenseService = CreateSubject();
            var licenseContent = "This is not a valid JSON string";

            var result = await licenseService.ImportLicense(licenseContent);

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task ImportLicense_ValidLicense_ReturnsLicenseInfo()
        {
            var licenseService = CreateSubject();

            var result = await licenseService.ImportLicense(TestLicenseData.ValidExpiredLicense);

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task ImportLicense_InvalidLicense_ReturnsNull()
        {
            var licenseService = CreateSubject();
            
            var result = await licenseService.ImportLicense(TestLicenseData.InvalidLicense);

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task ImportLicense_ValidLicense_StoresInDatabase()
        {
            var licenseService = CreateSubject();
            
            await licenseService.ImportLicense(TestLicenseData.ValidExpiredLicense);

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
        public async Task GetLicenseData_ValidLicense_ReturnsLicenseInformationAndTrue()
        {
            var licenseService = CreateSubject();
            
            var licenseInfo = await licenseService.ImportLicense(TestLicenseData.ValidExpiredLicense) ?? throw new ArgumentNullException("LicenseInfo cannot be null");
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
        public async Task CanUsePremiumFeature_InvalidLicense_ReturnsFalse()
        {
            var licenseService = CreateSubject();

            await licenseService.ImportLicense(TestLicenseData.InvalidLicense);

            var canUsePremiumFeatures = licenseService.CanUsePremiumFeatures();
            Assert.That(canUsePremiumFeatures, Is.False);
        }

        [Test]
        public async Task CanUsePremiumFeature_ValidLicense_Expired_ReturnsFalse()
        {
            var licenseService = CreateSubject();

            await licenseService.ImportLicense(TestLicenseData.ValidExpiredLicense);

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

        [Test]
        public void CanUsePremiumFeature_ValidLicense_ValidFromInFuture_ReturnsFalse()
        {
            var licenseInfo = new LicenseInformation
            {
                Name = "Benjamin",
                Organization = "Let People Work",
                Email = "benjamin@letpeople.work",
                ExpiryDate = DateTime.UtcNow.AddYears(1),
                ValidFrom = DateTime.UtcNow.AddDays(10),
            };

            var licenseVerifierMock = new Mock<ILicenseVerifier>();
            licenseVerifierMock
                .Setup(verifier => verifier.VerifyLicense(licenseInfo))
                .Returns(true);

            licenseRepoMock.Setup(repo => repo.GetAll()).Returns(new List<LicenseInformation> { licenseInfo });

            var licenseService = CreateSubject(licenseVerifierMock.Object);

            var canUsePremiumFeatures = licenseService.CanUsePremiumFeatures();

            Assert.That(canUsePremiumFeatures, Is.False);
        }

        [Test]
        public void CanUsePremiumFeature_ValidLicense_ValidFromInPast_ReturnsTrue()
        {
            var licenseInfo = new LicenseInformation
            {
                Name = "Benjamin",
                Organization = "Let People Work",
                Email = "benjamin@letpeople.work",
                ExpiryDate = DateTime.UtcNow.AddYears(1),
                ValidFrom = DateTime.UtcNow.AddDays(-10),
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
        public void CanUsePremiumFeature_ValidLicense_ValidFromToday_ReturnsTrue()
        {
            var licenseInfo = new LicenseInformation
            {
                Name = "Benjamin",
                Organization = "Let People Work",
                Email = "benjamin@letpeople.work",
                ExpiryDate = DateTime.UtcNow.AddYears(1),
                ValidFrom = DateTime.Today,
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
        public void CanUsePremiumFeature_ValidLicense_ValidFromNull_ReturnsTrue()
        {
            var licenseInfo = new LicenseInformation
            {
                Name = "Benjamin",
                Organization = "Let People Work",
                Email = "benjamin@letpeople.work",
                ExpiryDate = DateTime.UtcNow.AddYears(1),
                ValidFrom = null,
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
        public async Task ImportLicense_LicenseWithoutValidFrom_SetsValidFromToNull()
        {
            var licenseService = CreateSubject();
            
            var result = await licenseService.ImportLicense(TestLicenseData.ValidExpiredLicense);

            // The valid_expired_license.json doesn't have valid_from, so it should be null
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.ValidFrom, Is.Null);
        }

        [Test]
        public void ClearLicense_NoLicense_DoesNotThrow()
        {
            var licenseService = CreateSubject();
            licenseRepoMock.Setup(repo => repo.GetAll()).Returns(new List<LicenseInformation>());

            Assert.DoesNotThrowAsync(async () => await licenseService.ClearLicense());

            licenseRepoMock.Verify(repo => repo.Remove(It.IsAny<int>()), Times.Never);
            licenseRepoMock.Verify(repo => repo.Save(), Times.Never);
        }

        [Test]
        public async Task ClearLicense_WithLicense_RemovesLicenseFromRepository()
        {
            var licenseService = CreateSubject();
            var licenseInfo = new LicenseInformation
            {
                Id = 1,
                Name = "Test User",
                Organization = "Test Org",
                Email = "test@example.com",
                ExpiryDate = DateTime.UtcNow.AddDays(30),
                Signature = "test_signature"
            };

            licenseRepoMock.Setup(repo => repo.GetAll()).Returns(new List<LicenseInformation> { licenseInfo });

            await licenseService.ClearLicense();

            licenseRepoMock.Verify(repo => repo.Remove(licenseInfo.Id), Times.Once);
            licenseRepoMock.Verify(repo => repo.Save(), Times.Once);
        }

        [Test]
        public async Task ClearLicense_WithLicense_SubsequentGetLicenseDataReturnsNull()
        {
            var licenseService = CreateSubject();
            var licenseInfo = await licenseService.ImportLicense(TestLicenseData.ValidExpiredLicense);

            // Setup: Initially has license
            licenseRepoMock.Setup(repo => repo.GetAll()).Returns(new List<LicenseInformation> { licenseInfo! });

            var (initialLicense, initialIsValid) = licenseService.GetLicenseData();
            Assert.That(initialLicense, Is.Not.Null);

            // Clear license
            await licenseService.ClearLicense();

            // Setup: After clearing, no license
            licenseRepoMock.Setup(repo => repo.GetAll()).Returns(new List<LicenseInformation>());

            var (clearedLicense, clearedIsValid) = licenseService.GetLicenseData();
            
            using (Assert.EnterMultipleScope())
            {
                Assert.That(clearedLicense, Is.Null);
                Assert.That(clearedIsValid, Is.False);
            }
        }

        private LicenseService CreateSubject(ILicenseVerifier? licenseVerifierOverride = null)
        {
            var licenseVerifier = licenseVerifierOverride ?? new LicenseVerifier();

            return new LicenseService(Mock.Of<ILogger<LicenseService>>(), licenseRepoMock.Object, licenseVerifier);
        }
    }
}
