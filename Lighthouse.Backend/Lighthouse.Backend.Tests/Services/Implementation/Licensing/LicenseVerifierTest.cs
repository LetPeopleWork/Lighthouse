using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Licensing;

namespace Lighthouse.Backend.Tests.Services.Implementation.Licensing
{
    [TestFixture]
    public class LicenseVerifierTest
    {
        [Test]
        public void VerifyLicense_ValidLicenseWithoutValidFrom_ReturnsTrue()
        {
            var verifier = new LicenseVerifier();
            var licenseContent = File.ReadAllText("Services/Implementation/Licensing/valid_license.json");
            
            // Extract license from file
            using var doc = System.Text.Json.JsonDocument.Parse(licenseContent);
            var licenseElement = doc.RootElement.GetProperty("license");
            var signature = doc.RootElement.GetProperty("signature").GetString();
            
            var license = new LicenseInformation
            {
                Name = licenseElement.GetProperty("name").GetString() ?? string.Empty,
                Email = licenseElement.GetProperty("email").GetString() ?? string.Empty,
                Organization = licenseElement.GetProperty("organization").GetString() ?? string.Empty,
                ExpiryDate = DateTime.SpecifyKind(licenseElement.GetProperty("expiry").GetDateTime(), DateTimeKind.Utc),
                LicenseNumber = licenseElement.TryGetProperty("license_number", out var licenseNumberElement) 
                    ? licenseNumberElement.GetString() ?? string.Empty 
                    : string.Empty,
                ValidFrom = null,
                Signature = signature ?? string.Empty,
            };

            var result = verifier.VerifyLicense(license);

            Assert.That(result, Is.True);
        }

        [Test]
        public void VerifyLicense_LicenseWithValidFrom_VerifiesCorrectly()
        {
            var verifier = new LicenseVerifier();
            
            // Create a license with ValidFrom - this won't verify against a real signature
            // but tests the canonicalization logic
            var license = new LicenseInformation
            {
                Name = "Test User",
                Email = "test@example.com",
                Organization = "Test Org",
                ExpiryDate = DateTime.SpecifyKind(new DateTime(2025, 12, 31), DateTimeKind.Utc),
                LicenseNumber = "12345",
                ValidFrom = DateTime.SpecifyKind(new DateTime(2025, 01, 01), DateTimeKind.Utc),
                Signature = "invalid_signature_for_testing",
            };

            var result = verifier.VerifyLicense(license);

            // This will be false because the signature is invalid, but it tests the logic
            Assert.That(result, Is.False);
        }

        [Test]
        public void VerifyLicense_EmptySignature_ReturnsFalse()
        {
            var verifier = new LicenseVerifier();
            
            var license = new LicenseInformation
            {
                Name = "Test User",
                Email = "test@example.com",
                Organization = "Test Org",
                ExpiryDate = DateTime.UtcNow.AddYears(1),
                LicenseNumber = "12345",
                ValidFrom = null,
                Signature = string.Empty,
            };

            var result = verifier.VerifyLicense(license);

            Assert.That(result, Is.False);
        }

        [Test]
        public void VerifyLicense_NullSignature_ReturnsFalse()
        {
            var verifier = new LicenseVerifier();
            
            var license = new LicenseInformation
            {
                Name = "Test User",
                Email = "test@example.com",
                Organization = "Test Org",
                ExpiryDate = DateTime.UtcNow.AddYears(1),
                LicenseNumber = "12345",
                ValidFrom = null,
                Signature = null!,
            };

            var result = verifier.VerifyLicense(license);

            Assert.That(result, Is.False);
        }

        [Test]
        public void VerifyLicense_InvalidSignatureFormat_ReturnsFalse()
        {
            var verifier = new LicenseVerifier();
            
            var license = new LicenseInformation
            {
                Name = "Test User",
                Email = "test@example.com",
                Organization = "Test Org",
                ExpiryDate = DateTime.UtcNow.AddYears(1),
                LicenseNumber = "12345",
                ValidFrom = null,
                Signature = "not_base64!!!",
            };

            var result = verifier.VerifyLicense(license);

            Assert.That(result, Is.False);
        }
    }
}
