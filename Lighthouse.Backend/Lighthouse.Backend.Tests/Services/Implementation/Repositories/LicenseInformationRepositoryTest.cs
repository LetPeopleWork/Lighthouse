using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Repositories
{
    internal class LicenseInformationRepositoryTest : IntegrationTestBase
    {
        public LicenseInformationRepositoryTest() : base(new TestWebApplicationFactory<Program>())
        {
        }

        [Test]
        public async Task Add_NoLicense_AddsNewLicense()
        {
            var subject = CreateSubject();

            var licenseInfo = new LicenseInformation
            {
                Name = "Test",
                Email = "test@mail.com",
                LicenseNumber = "1234567890",
                Organization = "LetPeopleWork GmbH",
                ExpiryDate = DateTime.Now.AddYears(1),
                Signature = "wlkjsdalkjfasdlkfasd"
            };

            subject.Add(licenseInfo);
            await subject.Save();

            var savedLicense = subject.GetAll().Single();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(savedLicense.Name, Is.EqualTo(licenseInfo.Name));
                Assert.That(savedLicense.Email, Is.EqualTo(licenseInfo.Email));
                Assert.That(savedLicense.LicenseNumber, Is.EqualTo(licenseInfo.LicenseNumber));
                Assert.That(savedLicense.Organization, Is.EqualTo(licenseInfo.Organization));
                Assert.That(savedLicense.ExpiryDate, Is.EqualTo(licenseInfo.ExpiryDate));
                Assert.That(savedLicense.Signature, Is.EqualTo(licenseInfo.Signature));
            }
        }

        [Test]
        public async Task Add_ExistingLicense_OverwritesLicense()
        {
            var subject = CreateSubject();

            var licenseInfo = new LicenseInformation
            {
                Name = "Test",
                Email = "test@mail.com",
                LicenseNumber = "1234567890",
                Organization = "LetPeopleWork GmbH",
                ExpiryDate = DateTime.Now.AddYears(1),
                Signature = "VflBochumOle"
            };

            // Save initial License
            subject.Add(licenseInfo);
            await subject.Save();

            // Update License
            licenseInfo.Email = "newemail@test.com";
            licenseInfo.Organization = "LetPeopleWork AG";
            licenseInfo.ExpiryDate = DateTime.Now.AddYears(2);
            licenseInfo.ValidFrom = DateTime.Now.AddYears(20);
            licenseInfo.LicenseNumber = "1886";
            licenseInfo.Signature = "HoppGC!";
            subject.Add(licenseInfo);
            await subject.Save();

            // We expect a single license that is now updated
            var savedLicense = subject.GetAll().Single();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(savedLicense.Name, Is.EqualTo(licenseInfo.Name));
                Assert.That(savedLicense.Email, Is.EqualTo(licenseInfo.Email));
                Assert.That(savedLicense.LicenseNumber, Is.EqualTo(licenseInfo.LicenseNumber));
                Assert.That(savedLicense.Organization, Is.EqualTo(licenseInfo.Organization));
                Assert.That(savedLicense.ExpiryDate, Is.EqualTo(licenseInfo.ExpiryDate));
                Assert.That(savedLicense.ValidFrom, Is.EqualTo(licenseInfo.ValidFrom));
                Assert.That(savedLicense.Signature, Is.EqualTo(licenseInfo.Signature));
            }
        }

        private LicenseInformationRepository CreateSubject()
        {
            return new LicenseInformationRepository(DatabaseContext, Mock.Of<ILogger<LicenseInformationRepository>>());
        }
    }
}
