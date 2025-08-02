using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Lighthouse.Backend.Services.Implementation.Licensing
{
    public class LicenseService
    {
        private readonly ILogger<LicenseService> logger;
        private readonly IRepository<LicenseInformation> licenseRepository;
        private static readonly Lazy<string> publicKey = new(LoadEmbeddedPublicKey);

        public LicenseService(ILogger<LicenseService> logger, IRepository<LicenseInformation> licenseRepository)
        {
            this.logger = logger;
            this.licenseRepository = licenseRepository;
        }

        public bool ImportLicense(string licenseContent)
        {
            try
            {
                var licenseInformation = ExtractLicenseInformation(licenseContent);

                var verifyLicense = VerifyLicense(licenseInformation);

                if (verifyLicense)
                {
                    licenseRepository.Add(licenseInformation);
                    licenseRepository.Save();
                }

                return verifyLicense;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while importing the license.");
                return false;
            }
        }

        private bool VerifyLicense(LicenseInformation license)
        {
            if (string.IsNullOrEmpty(license.Signature))
            {
                return false;
            }

            string canonicalLicense = CanonicalizeJson(license);

            byte[] licenseBytes = Encoding.UTF8.GetBytes(canonicalLicense);
            byte[] signatureBytes = Convert.FromBase64String(license.Signature);

            using RSA rsa = RSA.Create();
            rsa.ImportFromPem(publicKey.Value);

            bool valid = rsa.VerifyData(
                licenseBytes,
                signatureBytes,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1
            );

            return valid;
        }

        private LicenseInformation ExtractLicenseInformation(string license)
        {
            using JsonDocument licenseDoc = JsonDocument.Parse(license);

            var licenseElement = licenseDoc.RootElement.GetProperty("license");
            var signatureBase64 = licenseDoc.RootElement.GetProperty("signature").GetString();

            return new LicenseInformation
            {
                Name = licenseElement.GetProperty("name").GetString() ?? string.Empty,
                Email = licenseElement.GetProperty("email").GetString() ?? string.Empty,
                Organization = licenseElement.GetProperty("organization").GetString() ?? string.Empty,
                ExpiryDate = licenseElement.GetProperty("expiry").GetDateTime(),
                Signature = signatureBase64,
            };
        }

        private static string CanonicalizeJson(LicenseInformation license)
        {
            var dict = new SortedDictionary<string, object>
            {
                ["email"] = license.Email,
                ["expiry"] = license.ExpiryDate.ToString("yyyy-MM-dd"),
                ["name"] = license.Name,
                ["organization"] = license.Organization
            };

            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false });

            writer.WriteStartObject();
            foreach (var kvp in dict)
            {
                writer.WritePropertyName(kvp.Key);

                switch (kvp.Value)
                {
                    case string stringValue:
                        writer.WriteStringValue(stringValue);
                        break;
                    case DateTime dateValue:
                        writer.WriteStringValue(dateValue.ToString("yyyy-MM-dd"));
                        break;
                    default:
                        writer.WriteStringValue(kvp.Value?.ToString() ?? "");
                        break;
                }
            }
            writer.WriteEndObject();
            writer.Flush();

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private static string LoadEmbeddedPublicKey()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames()
                .First(x => x.EndsWith("public_key.pem"));

            using var stream = assembly.GetManifestResourceStream(resourceName)!;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
