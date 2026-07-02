using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Licensing;

namespace Lighthouse.Backend.Tests.Services.Implementation.Licensing
{
    [TestFixture]
    public class LicenseVerifierTest
    {
        // Accented characters in this fixture are built from explicit code points
        // ((char)0x00E9 = precomposed 'e-acute', (char)0x0301 = combining acute) so the
        // source file stays pure ASCII and is immune to editor/Unicode-normalization drift.
        private const char EAcute = (char)0x00E9;      // precomposed (NFC)
        private const char CombiningAcute = (char)0x0301; // combining accent (NFD when after 'e')

        [Test]
        public void VerifyLicense_ValidLicenseWithoutValidFrom_ReturnsTrue()
        {
            var verifier = new LicenseVerifier();

            // Extract license from file
            using var doc = System.Text.Json.JsonDocument.Parse(TestLicenseData.ValidExpiredLicense);
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

        [Test]
        public void CanonicalizeJson_NameWithAccentedCharacter_MatchesRawUtf8SignerBytes()
        {
            // Regression test for Bug 5382: licenses with accented names failed to import.
            // The Python signer emits the name as raw UTF-8, but C#'s default JSON encoder
            // emitted an uppercase e-acute escape. The differing byte sequences produced
            // different SHA-256 hashes, so RSA verification rejected the license. The canonical
            // output must be byte-identical to the signer's raw-UTF-8 form.
            var name = "Jan" + EAcute + "e McConnell"; // precomposed NFC
            var license = new LicenseInformation
            {
                LicenseNumber = "65",
                Name = name,
                Email = "janee.mcconnell@epsilon.com",
                Organization = "Epsilon (part of Publicis Groupe)",
                ExpiryDate = DateTime.SpecifyKind(new DateTime(2026, 7, 31), DateTimeKind.Utc),
                ValidFrom = DateTime.SpecifyKind(new DateTime(2026, 6, 28), DateTimeKind.Utc),
                Signature = string.Empty,
            };

            var canonical = LicenseVerifier.CanonicalizeJson(license);

            var expected =
                "{\"email\":\"janee.mcconnell@epsilon.com\",\"expiry\":\"2026-07-31\"," +
                "\"license_number\":\"65\",\"name\":\"" + name + "\"," +
                "\"organization\":\"Epsilon (part of Publicis Groupe)\",\"valid_from\":\"2026-06-28\"}";
            Assert.That(canonical, Is.EqualTo(expected));
        }

        [Test]
        public void CanonicalizeJson_OrganizationWithAmpersand_EmitsRawAmpersandNotEscaped()
        {
            // Latent second facet of Bug 5382: C#'s default encoder also escapes HTML-sensitive
            // ASCII (& < > + '), which the Python signer leaves literal. Names/orgs such as
            // "Johnson & Johnson" would fail verification for the same reason accents did.
            var license = new LicenseInformation
            {
                LicenseNumber = "70",
                Name = "Test User",
                Email = "test@example.com",
                Organization = "Johnson & Johnson",
                ExpiryDate = DateTime.SpecifyKind(new DateTime(2026, 12, 31), DateTimeKind.Utc),
                ValidFrom = null,
                Signature = string.Empty,
            };

            var canonical = LicenseVerifier.CanonicalizeJson(license);

            Assert.That(canonical, Does.Contain("Johnson & Johnson"));
            Assert.That(canonical, Does.Not.Contain("\\u0026"));
        }

        [Test]
        public void CanonicalizeJson_DecomposedAccent_NormalizedToComposedForm()
        {
            // Hardening for Bug 5382: a name supplied in NFD (decomposed) form - 'e' followed by
            // a combining acute accent - must canonicalize to the same NFC (precomposed) bytes
            // the signer produces, so equivalent Unicode representations verify identically.
            var decomposedName = "Jane" + CombiningAcute + "e McConnell"; // NFD
            var expectedComposed = "Jan" + EAcute + "e McConnell";        // NFC
            var license = new LicenseInformation
            {
                LicenseNumber = "71",
                Name = decomposedName,
                Email = "test@example.com",
                Organization = "Test Org",
                ExpiryDate = DateTime.SpecifyKind(new DateTime(2026, 12, 31), DateTimeKind.Utc),
                ValidFrom = null,
                Signature = string.Empty,
            };

            var canonical = LicenseVerifier.CanonicalizeJson(license);

            Assert.That(canonical, Does.Contain(expectedComposed));
        }
    }
}
