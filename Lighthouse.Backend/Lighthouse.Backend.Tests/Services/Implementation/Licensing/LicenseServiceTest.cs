using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Licensing;
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
        public void ImportLicense_EmptyLicense_ReturnsFalse()
        {
            var licenseService = CreateSubject();

            var result = licenseService.ImportLicense(string.Empty);
            
            Assert.That(result, Is.False);
        }

        [Test]
        public void ImportLicense_IsNotValidJson_ReturnsFalse()
        {
            var licenseService = CreateSubject();
            var licenseContent = "This is not a valid JSON string";

            var result = licenseService.ImportLicense(licenseContent);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ImportLicense_ValidLicense_ReturnsTrue()
        {
            var licenseService = CreateSubject();

            var licenseContent = File.ReadAllText("Services/Implementation/Licensing/valid_license.json");

            var result = licenseService.ImportLicense(licenseContent);

            Assert.That(result, Is.True);
        }

        [Test]
        public void ImportLicense_InvalidLicense_ReturnsFalse()
        {
            var licenseService = CreateSubject();
            var licenseContent = File.ReadAllText("Services/Implementation/Licensing/invalid_license.json");
            
            var result = licenseService.ImportLicense(licenseContent);
            
            Assert.That(result, Is.False);
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

        private LicenseService CreateSubject()
        {
            return new LicenseService(Mock.Of<ILogger<LicenseService>>(), licenseRepoMock.Object);
        }
    }
}
